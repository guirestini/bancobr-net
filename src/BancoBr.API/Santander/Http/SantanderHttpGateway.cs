using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Santander.Errors;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Http
{
    /// <summary>
    /// Plumbing HTTP comum às APIs do Santander: mTLS, OAuth2 com renovação automática do token
    /// no 401, header <c>X-Application-Key</c> e normalização do corpo de erro em
    /// <see cref="BancoApiException"/>. Usado por composição (os clientes já herdam de uma
    /// <c>*ApiBase</c>, então não dá para herdar daqui).
    /// </summary>
    internal class SantanderHttpGateway
    {
        public static readonly Uri DefaultTokenEndpoint = new Uri("https://trust-open.api.santander.com.br/auth/oauth/v2/token");

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        private readonly HttpClient _httpClient;
        private readonly IAccessTokenProvider _tokenProvider;
        private readonly string _clientId;

        public SantanderHttpGateway(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        }

        public static HttpClient BuildHttpClient(CertificateSource certificateSource)
        {
            var certHandler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
            };
            certHandler.ClientCertificates.Add(certificateSource.GetCertificate());

            return new HttpClient(certHandler);
        }

        public static OAuthTokenProvider BuildTokenProvider(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint)
        {
            var tokenOptions = new OAuthTokenProviderOptions
            {
                TokenEndpoint = tokenEndpoint ?? DefaultTokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
            };

            return new OAuthTokenProvider(BuildHttpClient(certificateSource), tokenOptions);
        }

        public async Task<T> SendAsync<T>(HttpMethod method, string url, object body, CancellationToken cancellationToken)
        {
            var json = body == null ? null : JsonConvert.SerializeObject(body, SerializerSettings);

            using (var response = await SendWithAuthAsync(() => BuildRequest(method, url, json), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default;
                }

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                EnsureSuccessOrThrow(response, responseBody);

                return JsonConvert.DeserializeObject<T>(responseBody, SerializerSettings);
            }
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string url, string body)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Application-Key", _clientId);

            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            return request;
        }

        private async Task<HttpResponseMessage> SendWithAuthAsync(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
        {
            var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            var response = await SendOnceAsync(requestFactory, token, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                _tokenProvider.InvalidateToken();
                token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
                response = await SendOnceAsync(requestFactory, token, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }

        private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> requestFactory, string token, CancellationToken cancellationToken)
        {
            using (var request = requestFactory())
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Os erros seguem o corpo <c>_errorCode/_message/_details/_errors[]</c>. Se o corpo não
        /// vier nesse formato, cai para o texto bruto da resposta, para nunca esconder a causa
        /// real de uma falha.
        /// </summary>
        private static void EnsureSuccessOrThrow(HttpResponseMessage response, string body)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var mensagens = new List<MensagemErro>();

            SantanderErrorResponse erro;
            try
            {
                erro = JsonConvert.DeserializeObject<SantanderErrorResponse>(body, SerializerSettings);
            }
            catch (JsonException)
            {
                erro = null;
            }

            if (erro != null && (!string.IsNullOrEmpty(erro.Message) || !string.IsNullOrEmpty(erro.Details)))
            {
                mensagens.Add(new MensagemErro { Codigo = erro.ErrorCode, Mensagem = string.IsNullOrEmpty(erro.Details) ? erro.Message : erro.Details });
            }

            if (erro?.Errors != null)
            {
                foreach (var item in erro.Errors)
                {
                    mensagens.Add(new MensagemErro { Codigo = item.Code, Mensagem = string.IsNullOrEmpty(item.Field) ? item.Message : $"{item.Field}: {item.Message}" });
                }
            }

            if (mensagens.Count == 0 && !string.IsNullOrWhiteSpace(body))
            {
                mensagens.Add(new MensagemErro { Codigo = ((int)response.StatusCode).ToString(), Mensagem = body });
            }

            throw new BancoApiException((int)response.StatusCode, mensagens);
        }
    }
}
