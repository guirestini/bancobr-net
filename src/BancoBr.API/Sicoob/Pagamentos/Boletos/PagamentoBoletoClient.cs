using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Pagamentos;
using BancoBr.API.Sicoob.Errors;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using BancoBr.Common.Enums;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos
{
    /// <summary>
    /// Cliente para a API "Pagamentos de Boletos" (Cobrança Bancária) do Sicoob, v3.
    /// </summary>
    public class PagamentoBoletoClient : BancoApiClientBase, IPagamentoBoletoApi
    {
        /// <summary>
        /// Base URL, scopes e rate limit são intrínsecos a esta API específica do Sicoob
        /// (Pagamentos de Boletos v3) — não fazem sentido como configuração vinda do
        /// consumidor (ERP), que não deveria precisar conhecer detalhes da API do banco.
        /// </summary>
        public static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/pagamentos/v3");

        private static readonly string[] Scopes = { "pagamentos_consulta", "pagamentos_inclusao", "pagamentos_alteracao" };

        private const int RequestsPerSecond = 2;

        private readonly IAccessTokenProvider _tokenProvider;
        private readonly string _clientId;
        private readonly string _baseUrl;
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions();

        public PagamentoBoletoClient(SicoobApiOptions options)
            : this(options, BuildTokenProvider(options))
        {
        }

        /// <summary>
        /// Usa o pipeline HTTP padrão (certificado mTLS + rate limiting) montado a partir de
        /// <paramref name="options"/>, mas com um provedor de token à escolha do chamador —
        /// por exemplo, <see cref="StaticAccessTokenProvider"/> quando o ambiente de sandbox
        /// fornece um Access Token (Bearer) pronto em vez de expor um endpoint OAuth2.
        /// </summary>
        public PagamentoBoletoClient(SicoobApiOptions options, IAccessTokenProvider tokenProvider)
            : this(BuildHttpClient(options), tokenProvider, options.ClientId, BaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake,
        /// sem certificado real nem chamadas HTTP de fato.
        /// </summary>
        public PagamentoBoletoClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl)
            : base((int)BancoEnum.Sicoob, "Sicoob", httpClient)
        {
            if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));

            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            var baseUrlText = baseUrl.ToString();
            _baseUrl = baseUrlText.EndsWith("/") ? baseUrlText : baseUrlText + "/";
        }

        private static HttpClient BuildHttpClient(SicoobApiOptions options)
        {
            var certHandler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
            };
            certHandler.ClientCertificates.Add(options.CertificateSource.GetCertificate());

            var rateLimiter = new RateLimitingHandler(RequestsPerSecond)
            {
                InnerHandler = certHandler,
            };

            return new HttpClient(rateLimiter);
        }

        private static OAuthTokenProvider BuildTokenProvider(SicoobApiOptions options)
        {
            var certHandler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
            };
            certHandler.ClientCertificates.Add(options.CertificateSource.GetCertificate());

            var tokenHttpClient = new HttpClient(certHandler);
            var tokenOptions = new OAuthTokenProviderOptions
            {
                TokenEndpoint = options.TokenEndpoint,
                ClientId = options.ClientId,
                ClientSecret = options.ClientSecret,
                Scopes = Scopes,
            };

            return new OAuthTokenProvider(tokenHttpClient, tokenOptions);
        }

        public async Task<BoletoConsultaResponse> ConsultarBoletoAsync(string codigoBarras, long numeroConta, DateTime? dataPagamento = null, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos/{codigoBarras}?numeroConta={numeroConta}";
            if (dataPagamento.HasValue)
            {
                url += $"&dataPagamento={dataPagamento.Value:yyyy-MM-dd}";
            }

            return await SendAsync<BoletoConsultaResponse>(HttpMethod.Get, url, body: null, idempotencyKey: null, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<PagamentoBoletoResultado> PagarBoletoComConsultaAsync(string codigoBarras, long numeroConta, int numeroCooperativa, string numeroCpfCnpjPortador, string nomePortador, bool aceitaValorDivergente = false, string descricaoObservacao = null, DateTime? dataPagamento = null, int personType = 0, CancellationToken cancellationToken = default)
        {
            var consulta = await ConsultarBoletoAsync(codigoBarras, numeroConta, dataPagamento, cancellationToken).ConfigureAwait(false);
            if (consulta == null)
            {
                return PagamentoBoletoResultado.NaoEncontrado();
            }

            if (consulta.BloquearPagamento)
            {
                return PagamentoBoletoResultado.Bloqueado(consulta.MensagemBloqueioPagamento);
            }

            var request = new BoletoPagamentoRequest
            {
                IdentificadorConsulta = consulta.IdentificadorConsulta,
                ValorBoleto = consulta.ValorBoleto,
                ValorDescontoAbatimento = consulta.ValorAbatimentoDesconto,
                ValorMultaMora = consulta.ValorMultaMora,
                DescricaoObservacao = descricaoObservacao,
                AceitaValorDivergente = aceitaValorDivergente,
                NumeroCpfCnpjPortador = numeroCpfCnpjPortador,
                NomePortador = nomePortador,
                Amount = consulta.ValorPagamento,
                Date = dataPagamento ?? consulta.DataPagamento,
                DebtorAccount = new DebtorAccount
                {
                    Issuer = numeroCooperativa,
                    Number = numeroConta,
                    AccountType = 0,
                    PersonType = personType,
                },
            };

            var idempotencyKey = IdempotencyKey.New(numeroCooperativa, numeroConta);
            return await PagarBoletoAsync(codigoBarras, request, idempotencyKey, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<PagamentoBoletoLoteResultadoItem>> PagarLoteBoletosAsync(IEnumerable<PagamentoBoletoLoteItem> itens, CancellationToken cancellationToken = default)
        {
            var resultados = new List<PagamentoBoletoLoteResultadoItem>();

            foreach (var item in itens)
            {
                try
                {
                    var resultado = await PagarBoletoComConsultaAsync(
                        item.CodigoBarras,
                        item.NumeroConta,
                        item.NumeroCooperativa,
                        item.NumeroCpfCnpjPortador,
                        item.NomePortador,
                        item.AceitaValorDivergente,
                        item.DescricaoObservacao,
                        item.DataPagamento,
                        item.PersonType,
                        cancellationToken).ConfigureAwait(false);

                    resultados.Add(PagamentoBoletoLoteResultadoItem.ComSucesso(item, resultado));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    resultados.Add(PagamentoBoletoLoteResultadoItem.ComFalha(item, ex));
                }
            }

            return resultados;
        }

        public async Task<PagamentoBoletoResultado> PagarBoletoAsync(string codigoBarras, BoletoPagamentoRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos/pagamentos/{codigoBarras}";
            var json = JsonSerializer.Serialize(request, SerializerOptions);

            using (var httpRequest = BuildRequest(HttpMethod.Post, url, json, idempotencyKey))
            using (var response = await SendWithAuthAsync(httpRequest, cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    return PagamentoBoletoResultado.PendenteDeAssinatura();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return PagamentoBoletoResultado.SemConteudo();
                }

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var envelope = JsonSerializer.Deserialize<ResultadoEnvelope<ComprovantePagamento>>(body, SerializerOptions);
                return PagamentoBoletoResultado.Efetivado(envelope.Resultado);
            }
        }

        public async Task<ComprovantePagamento> ConsultarComprovantePorIdAsync(long idPagamento, long numeroConta, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos/pagamentos/{idPagamento}/comprovantes?numeroConta={numeroConta}";
            return await SendAsync<ComprovantePagamento>(HttpMethod.Get, url, body: null, idempotencyKey: null, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task CancelarAgendamentoAsync(long idPagamento, long numeroConta, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos/pagamentos/agendamentos/{idPagamento}";
            var json = JsonSerializer.Serialize(new CancelamentoRequest { NumeroConta = numeroConta }, SerializerOptions);

            using (var httpRequest = BuildRequest(HttpMethod.Delete, url, json, idempotencyKey: null))
            using (var response = await SendWithAuthAsync(httpRequest, cancellationToken).ConfigureAwait(false))
            {
                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            }
        }

        public async Task<ComprovantePagamento> ConsultarComprovantePorIdempotencyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos/pagamentos/{idempotencyKey}/idempotency/comprovantes";
            return await SendAsync<ComprovantePagamento>(HttpMethod.Get, url, body: null, idempotencyKey: null, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<System.Collections.Generic.IReadOnlyList<BoletoDDA>> ConsultarBoletosDdaAsync(long numeroConta, DateTime dataInicial, DateTime dataFinal, SituacaoBoletoEnum situacao, TipoDataConsultaEnum tipoData, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos?numeroConta={numeroConta}&dataInicial={dataInicial:yyyy-MM-dd}&dataFinal={dataFinal:yyyy-MM-dd}&situacao={(int)situacao}&tipoData={(int)tipoData}";

            using (var httpRequest = BuildRequest(HttpMethod.Get, url, body: null, idempotencyKey: null))
            using (var response = await SendWithAuthAsync(httpRequest, cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return Array.Empty<BoletoDDA>();
                }

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<BoletoDDA[]>(body, SerializerOptions);
            }
        }

        private async Task<T> SendAsync<T>(HttpMethod method, string url, string body, string idempotencyKey, CancellationToken cancellationToken)
        {
            using (var httpRequest = BuildRequest(method, url, body, idempotencyKey))
            using (var response = await SendWithAuthAsync(httpRequest, cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default;
                }

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var envelope = JsonSerializer.Deserialize<ResultadoEnvelope<T>>(responseBody, SerializerOptions);
                return envelope.Resultado;
            }
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string url, string body, string idempotencyKey)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("client_id", _clientId);

            if (idempotencyKey != null)
            {
                request.Headers.Add("x-idempotency-key", idempotencyKey);
            }

            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            return request;
        }

        private async Task<HttpResponseMessage> SendWithAuthAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
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
                errorResponse = JsonSerializer.Deserialize<SicoobErrorResponse>(body, SerializerOptions);
            }
            catch (JsonException)
            {
                errorResponse = null;
            }

            throw new SicoobApiException((int)response.StatusCode, errorResponse?.Mensagens ?? new System.Collections.Generic.List<SicoobMensagem>());
        }
    }
}
