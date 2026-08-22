using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Base.Models;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.ContaCorrente.Models;
using BancoBr.API.Sicoob.Errors;
using BancoBr.Common.Instances;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.ContaCorrente
{
    /// <summary>
    /// Cliente para a API "Conta Corrente" do Sicoob, v4 (saldo/extrato).
    /// </summary>
    public class ExtratoClient : ContaCorrenteApiBase
    {
        public static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/conta-corrente/v4");

        private static readonly Uri DefaultTokenEndpoint = new Uri("https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token");

        private static readonly string[] Scopes = { "cco_consulta" };

        private const int RequestsPerSecond = 2;

        private readonly IAccessTokenProvider _tokenProvider;
        private readonly string _clientId;
        private readonly string _baseUrl;
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        internal ExtratoClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null)
            : this(clientId, certificateSource, BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint ?? DefaultTokenEndpoint))
        {
        }

        internal ExtratoClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            : this(BuildHttpClient(certificateSource), tokenProvider, clientId, BaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake,
        /// sem certificado real nem chamadas HTTP de fato.
        /// </summary>
        public ExtratoClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl)
            : base(httpClient)
        {
            if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));

            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            var baseUrlText = baseUrl.ToString();
            _baseUrl = baseUrlText.EndsWith("/") ? baseUrlText : baseUrlText + "/";
        }

        private static HttpClient BuildHttpClient(CertificateSource certificateSource)
        {
            var certHandler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
            };
            certHandler.ClientCertificates.Add(certificateSource.GetCertificate());

            var rateLimiter = new RateLimitingHandler(RequestsPerSecond)
            {
                InnerHandler = certHandler,
            };

            return new HttpClient(rateLimiter);
        }

        private static OAuthTokenProvider BuildTokenProvider(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint)
        {
            var certHandler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
            };
            certHandler.ClientCertificates.Add(certificateSource.GetCertificate());

            var tokenHttpClient = new HttpClient(certHandler);
            var tokenOptions = new OAuthTokenProviderOptions
            {
                TokenEndpoint = tokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scopes = Scopes,
            };

            return new OAuthTokenProvider(tokenHttpClient, tokenOptions);
        }

        #region ::. Operações .::

        public override async Task<Extrato> ConsultarExtratoAsync(long numeroContaCorrente, int mes, int ano, int? diaInicial, int? diaFinal, bool agruparCnab, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}extrato/{mes}/{ano}?numeroContaCorrente={numeroContaCorrente}&agruparCNAB={(agruparCnab ? "true" : "false")}";

            if (diaInicial.HasValue)
                url += $"&diaInicial={diaInicial.Value}";

            if (diaFinal.HasValue)
                url += $"&diaFinal={diaFinal.Value}";

            using (var response = await SendWithAuthAsync(() => BuildRequest(url), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    return new Extrato { Transacoes = Array.Empty<ExtratoTransacao>() };

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var wire = JsonConvert.DeserializeObject<ExtratoResponse>(body, SerializerSettings);

                return MapExtrato(wire);
            }
        }

        #endregion

        #region ::. Mapeamento wire -> agnóstico .::

        private static Extrato MapExtrato(ExtratoResponse wire)
        {
            if (wire == null)
                return new Extrato { Transacoes = Array.Empty<ExtratoTransacao>() };

            return new Extrato
            {
                SaldoAtual = ParseDecimal(wire.SaldoAtual),
                SaldoBloqueado = ParseDecimal(wire.SaldoBloqueado),
                SaldoLimite = ParseDecimal(wire.SaldoLimite),
                SaldoAnterior = ParseDecimal(wire.SaldoAnterior),
                SaldoBloqueioJudicial = ParseDecimal(wire.SaldoBloqueioJudicial),
                SaldoBloqueioJudicialAnterior = ParseDecimal(wire.SaldoBloqueioJudicialAnterior),
                Transacoes = (wire.Transacoes ?? Array.Empty<TransacaoResponse>()).Select(MapTransacao).ToList(),
            };
        }

        private static ExtratoTransacao MapTransacao(TransacaoResponse dto) => new ExtratoTransacao
        {
            TransactionId = dto.TransactionId,
            Tipo = dto.Tipo,
            Valor = ParseDecimal(dto.Valor),
            Data = ParseData(dto.Data),
            DataLote = string.IsNullOrWhiteSpace(dto.DataLote) ? (DateTime?)null : ParseData(dto.DataLote),
            Descricao = dto.Descricao,
            NumeroDocumento = dto.NumeroDocumento,
            CpfCnpj = dto.CpfCnpj,
            DescricaoInformacaoComplementar = dto.DescInfComplementar,
        };

        private static decimal ParseDecimal(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return 0;

            return decimal.Parse(valor, NumberStyles.Number, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// ATENÇÃO: o Sicoob devolve as datas do extrato como texto e o formato exato
        /// ("dd/MM/yyyy" vs. ISO 8601) não está confirmado em documentação disponível neste
        /// repositório — DEVE SER VALIDADO contra respostas reais do sandbox/produção. Tenta
        /// "dd/MM/yyyy" (padrão mais comum nas APIs do Sicoob) e cai para um parse genérico
        /// caso contrário.
        /// </summary>
        private static DateTime ParseData(string data)
        {
            if (DateTime.TryParseExact(data, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exata))
                return exata;

            return DateTime.Parse(data, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        #endregion

        #region ::. Plumbing HTTP .::

        private HttpRequestMessage BuildRequest(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("client_id", _clientId);
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
                return await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            SicoobErrorResponse errorResponse;
            try
            {
                errorResponse = JsonConvert.DeserializeObject<SicoobErrorResponse>(body, SerializerSettings);
            }
            catch (JsonException)
            {
                errorResponse = null;
            }

            throw new SicoobApiException((int)response.StatusCode, errorResponse?.Mensagens ?? new System.Collections.Generic.List<SicoobMensagem>());
        }

        #endregion
    }
}
