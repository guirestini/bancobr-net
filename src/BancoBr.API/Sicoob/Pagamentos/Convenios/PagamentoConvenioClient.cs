using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.Errors;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using BancoBr.API.Sicoob.Pagamentos.Convenios.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Convenios
{
    /// <summary>
    /// Cliente para a API "Convênios Pagamentos" do Sicoob, v2 — bloco de Arrecadação por
    /// código de barras (pagamento de convênios/tributos via código de barras).
    /// </summary>
    public class PagamentoConvenioClient : PagamentoConvenioApiBase
    {
        /// <summary>
        /// Base URL, scopes e rate limit são intrínsecos a esta API específica do Sicoob
        /// (Convênios Pagamentos v2) — não fazem sentido como configuração vinda do
        /// consumidor (ERP), que não deveria precisar conhecer detalhes da API do banco.
        /// </summary>
        public static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/convenios-pagamentos/v2");

        private static readonly Uri DefaultTokenEndpoint = new Uri("https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token");

        private static readonly string[] Scopes = { "convenios_consulta", "convenios_escrita" };

        private const int RequestsPerSecond = 2;

        private readonly IAccessTokenProvider _tokenProvider;
        private readonly string _clientId;
        private readonly string _baseUrl;
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        internal PagamentoConvenioClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null)
            : this(clientId, certificateSource, BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint ?? DefaultTokenEndpoint))
        {
        }

        /// <summary>
        /// Usa o pipeline HTTP padrão (certificado mTLS + rate limiting) montado a partir de
        /// <paramref name="certificateSource"/>, mas com um provedor de token à escolha do
        /// chamador — por exemplo, <see cref="StaticAccessTokenProvider"/> quando o ambiente de
        /// sandbox fornece um Access Token (Bearer) pronto em vez de expor um endpoint OAuth2.
        /// </summary>
        internal PagamentoConvenioClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            : this(BuildHttpClient(certificateSource), tokenProvider, clientId, BaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake,
        /// sem certificado real nem chamadas HTTP de fato.
        /// </summary>
        public PagamentoConvenioClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl)
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

        public override async Task<ConvenioConsultaResponse> ConsultarCodigoBarrasAsync(string codigoBarras, DateTime dataPagamento, bool? recebimentoViaCaixa = null, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/codigo-barras/{codigoBarras}?dataPagamento={dataPagamento:yyyy-MM-dd}";
            if (recebimentoViaCaixa.HasValue)
            {
                url += $"&recebimentoViaCaixa={recebimentoViaCaixa.Value.ToString().ToLowerInvariant()}";
            }

            return await SendAsync<ConvenioConsultaResponse>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);
        }

        public override async Task<PagamentoConvenioResultado> PagarConvenioAsync(string codigoBarras, ArrecadacaoPagamentoRequest request, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/codigo-barras/{codigoBarras}/pagamentos";
            var json = JsonConvert.SerializeObject(request, SerializerSettings);

            using (var response = await SendWithAuthAsync(() => BuildRequest(HttpMethod.Post, url, json), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    return PagamentoConvenioResultado.PendenteDeAssinatura();
                }

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var envelope = JsonConvert.DeserializeObject<ResultadoEnvelope<ArrecadacaoResultado>>(body, SerializerSettings);
                return PagamentoConvenioResultado.Efetivado(envelope.Resultado);
            }
        }

        public override async Task<IReadOnlyList<ArrecadacaoConsultaItem>> ConsultarPagamentosAsync(string codigoBarras, long instituicao, DateTime dataMovimento, long? transacao = null, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/codigo-barras/{codigoBarras}/pagamentos?instituicao={instituicao}&dataMovimento={dataMovimento:yyyy-MM-dd}";
            if (transacao.HasValue)
            {
                url += $"&transacao={transacao.Value}";
            }

            return await SendAsync<List<ArrecadacaoConsultaItem>>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);
        }

        public override async Task<ComprovanteArrecadacao> ConsultarComprovantePorNsuAsync(long nsu, long instituicao, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/pagamentos/{nsu}/comprovante?instituicao={instituicao}";
            return await SendAsync<ComprovanteArrecadacao>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);
        }

        public override async Task<IReadOnlyList<ConciliacaoItem>> ConsultarConciliacoesAsync(long instituicao, DateTime dataMovimento, int? unidade = null, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/conciliacoes?dataMovimento={dataMovimento:yyyy-MM-dd}&instituicao={instituicao}";
            if (unidade.HasValue)
            {
                url += $"&unidade={unidade.Value}";
            }

            return await SendAsync<List<ConciliacaoItem>>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);
        }

        public override async Task<IReadOnlyList<ConvenioHabilitado>> ConsultarConveniosHabilitadosAsync(long transacao, long instituicao, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/convenios-habilitados?transacao={transacao}&instituicao={instituicao}";
            return await SendAsync<List<ConvenioHabilitado>>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<T> SendAsync<T>(HttpMethod method, string url, string body, CancellationToken cancellationToken)
        {
            using (var response = await SendWithAuthAsync(() => BuildRequest(method, url, body), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default;
                }

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var envelope = JsonConvert.DeserializeObject<ResultadoEnvelope<T>>(responseBody, SerializerSettings);
                return envelope.Resultado;
            }
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string url, string body)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("client_id", _clientId);

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
                return await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

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

            throw new SicoobApiException((int)response.StatusCode, errorResponse?.Mensagens ?? new List<SicoobMensagem>());
        }
    }
}
