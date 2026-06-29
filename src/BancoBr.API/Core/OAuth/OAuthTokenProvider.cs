using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.Core.OAuth
{
    /// <summary>
    /// Obtém e mantém em cache um token OAuth2 via client_credentials (RFC 6749).
    /// O HttpClient informado deve já estar configurado com o certificado mTLS quando exigido
    /// pela API (o próprio endpoint de token do Sicoob exige o certificado).
    /// </summary>
    public class OAuthTokenProvider : IAccessTokenProvider
    {
        private readonly HttpClient _httpClient;
        private readonly OAuthTokenProviderOptions _options;
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

        private string _cachedAccessToken;
        private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

        public OAuthTokenProvider(HttpClient httpClient, OAuthTokenProviderOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public virtual async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (_cachedAccessToken != null && DateTimeOffset.UtcNow < _expiresAt)
            {
                return _cachedAccessToken;
            }

            await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_cachedAccessToken != null && DateTimeOffset.UtcNow < _expiresAt)
                {
                    return _cachedAccessToken;
                }

                await RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
                return _cachedAccessToken;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public void InvalidateToken()
        {
            _cachedAccessToken = null;
            _expiresAt = DateTimeOffset.MinValue;
        }

        private async Task RefreshTokenAsync(CancellationToken cancellationToken)
        {
            var formValues = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
            };

            if (!string.IsNullOrEmpty(_options.ClientSecret))
            {
                formValues.Add(new KeyValuePair<string, string>("client_secret", _options.ClientSecret));
            }

            if (_options.Scopes != null && _options.Scopes.Count > 0)
            {
                formValues.Add(new KeyValuePair<string, string>("scope", string.Join(" ", _options.Scopes)));
            }

            using (var content = new FormUrlEncodedContent(formValues))
            using (var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint) { Content = content })
            using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var root = JObject.Parse(body);
                _cachedAccessToken = (string)root["access_token"];

                var expiresInSeconds = root["expires_in"]?.Value<int>() ?? 0;

                // Renova um pouco antes do vencimento real para evitar usar um token expirado em trânsito.
                var safetyMargin = TimeSpan.FromSeconds(Math.Min(30, expiresInSeconds / 4.0));
                _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds) - safetyMargin;
            }
        }
    }
}
