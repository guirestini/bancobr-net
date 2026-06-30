using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.Models;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.Errors;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos
{
    /// <summary>
    /// Cliente para a API "Pagamentos de Boletos" (Cobrança Bancária) do Sicoob, v3.
    /// </summary>
    public class PagamentoBoletoClient : PagamentoBoletoApiBase
    {
        /// <summary>
        /// Base URL, scopes e rate limit são intrínsecos a esta API específica do Sicoob
        /// (Pagamentos de Boletos v3) — não fazem sentido como configuração vinda do
        /// consumidor (ERP), que não deveria precisar conhecer detalhes da API do banco.
        /// </summary>
        public static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/pagamentos/v3");

        /// <summary>
        /// Endpoint OAuth2 (client_credentials) do Sicoob, igual para qualquer API/produto e
        /// qualquer cooperado. Só precisa ser sobrescrito em cenários fora do padrão
        /// (ex.: um ambiente de testes com realm próprio) via o parâmetro tokenEndpoint.
        /// </summary>
        private static readonly Uri DefaultTokenEndpoint = new Uri("https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token");

        private static readonly string[] Scopes = { "pagamentos_consulta", "pagamentos_inclusao", "pagamentos_alteracao" };

        private const int RequestsPerSecond = 2;

        private readonly IAccessTokenProvider _tokenProvider;
        private readonly string _clientId;
        private readonly string _baseUrl;
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        internal PagamentoBoletoClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null)
            : this(clientId, certificateSource, BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint ?? DefaultTokenEndpoint))
        {
        }

        /// <summary>
        /// Usa o pipeline HTTP padrão (certificado mTLS + rate limiting) montado a partir de
        /// <paramref name="certificateSource"/>, mas com um provedor de token à escolha do
        /// chamador — por exemplo, <see cref="StaticAccessTokenProvider"/> quando o ambiente de
        /// sandbox fornece um Access Token (Bearer) pronto em vez de expor um endpoint OAuth2.
        /// </summary>
        internal PagamentoBoletoClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            : this(BuildHttpClient(certificateSource), tokenProvider, clientId, BaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake,
        /// sem certificado real nem chamadas HTTP de fato.
        /// </summary>
        public PagamentoBoletoClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl)
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

        public override async Task<BancoBr.API.Base.Models.BoletoConsultaResponse> ConsultarBoletoAsync(string codigoBarras, long numeroConta, DateTime? dataPagamento = null, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos/{codigoBarras}?numeroConta={numeroConta}";
            if (dataPagamento.HasValue)
            {
                url += $"&dataPagamento={dataPagamento.Value:yyyy-MM-dd}";
            }

            var resposta = await SendAsync<BoletoConsultaResponse>(HttpMethod.Get, url, body: null, idempotencyKey: null, cancellationToken)
                .ConfigureAwait(false);

            return MapToAgnostic(resposta);
        }

        public override async Task<BancoBr.API.Base.Models.PagamentoBoletoResultado> PagarBoletoComConsultaAsync(string codigoBarras, long numeroConta, int numeroAgencia, Guid idLancamento, string numeroCpfCnpjPortador, string nomePortador, bool aceitaValorDivergente = false, string descricaoObservacao = null, DateTime? dataPagamento = null, int personType = 0, CancellationToken cancellationToken = default)
        {
            var consulta = await ConsultarBoletoAsync(codigoBarras, numeroConta, dataPagamento, cancellationToken).ConfigureAwait(false);
            if (consulta == null)
            {
                return BancoBr.API.Base.Models.PagamentoBoletoResultado.NaoEncontrado();
            }

            if (consulta.BloquearPagamento)
            {
                return BancoBr.API.Base.Models.PagamentoBoletoResultado.Bloqueado(consulta.MensagemBloqueioPagamento);
            }

            var request = new BancoBr.API.Base.Models.BoletoPagamentoRequest
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
                DebtorAccount = new BancoBr.API.Base.Models.DebtorAccount
                {
                    Issuer = numeroAgencia,
                    Number = numeroConta,
                    AccountType = 0,
                    PersonType = personType,
                },
            };

            var idempotencyKey = IdempotencyKey.New(numeroAgencia, numeroConta, idLancamento);
            return await PagarBoletoAsync(codigoBarras, request, idempotencyKey, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<IReadOnlyList<PagamentoBoletoLoteResultadoItem>> PagarLoteBoletosAsync(IEnumerable<PagamentoBoletoLoteItem> itens, CancellationToken cancellationToken = default)
        {
            var resultados = new List<PagamentoBoletoLoteResultadoItem>();

            foreach (var item in itens)
            {
                try
                {
                    var resultado = await PagarBoletoComConsultaAsync(
                        item.CodigoBarras,
                        item.NumeroConta,
                        item.NumeroAgencia,
                        item.IdLancamento,
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

        public override async Task<BancoBr.API.Base.Models.PagamentoBoletoResultado> PagarBoletoAsync(string codigoBarras, BancoBr.API.Base.Models.BoletoPagamentoRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos/pagamentos/{codigoBarras}";
            var json = JsonConvert.SerializeObject(MapToSicoob(request), SerializerSettings);

            using (var response = await SendWithAuthAsync(() => BuildRequest(HttpMethod.Post, url, json, idempotencyKey), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    return BancoBr.API.Base.Models.PagamentoBoletoResultado.PendenteDeAssinatura();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return BancoBr.API.Base.Models.PagamentoBoletoResultado.SemConteudo();
                }

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var envelope = JsonConvert.DeserializeObject<ResultadoEnvelope<ComprovantePagamento>>(body, SerializerSettings);
                return BancoBr.API.Base.Models.PagamentoBoletoResultado.Efetivado(MapToAgnostic(envelope.Resultado));
            }
        }

        public override async Task<BancoBr.API.Base.Models.ComprovantePagamento> ConsultarComprovantePorIdAsync(long idPagamento, long numeroConta, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos/pagamentos/{idPagamento}/comprovantes?numeroConta={numeroConta}";
            var resultado = await SendAsync<ComprovantePagamento>(HttpMethod.Get, url, body: null, idempotencyKey: null, cancellationToken)
                .ConfigureAwait(false);
            return MapToAgnostic(resultado);
        }

        public override async Task CancelarAgendamentoAsync(long idPagamento, long numeroConta, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos/pagamentos/agendamentos/{idPagamento}";
            var json = JsonConvert.SerializeObject(new CancelamentoRequest { NumeroConta = numeroConta }, SerializerSettings);

            using (var response = await SendWithAuthAsync(() => BuildRequest(HttpMethod.Delete, url, json, idempotencyKey: null), cancellationToken).ConfigureAwait(false))
            {
                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            }
        }

        public override async Task<BancoBr.API.Base.Models.ComprovantePagamento> ConsultarComprovantePorIdempotencyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos/pagamentos/{idempotencyKey}/idempotency/comprovantes";
            var resultado = await SendAsync<ComprovantePagamento>(HttpMethod.Get, url, body: null, idempotencyKey: null, cancellationToken)
                .ConfigureAwait(false);
            return MapToAgnostic(resultado);
        }

        private static BancoBr.API.Base.Models.BoletoConsultaResponse MapToAgnostic(BoletoConsultaResponse sicoob)
        {
            if (sicoob == null) return null;

            return new BancoBr.API.Base.Models.BoletoConsultaResponse
            {
                NumeroInstituicaoEmissora = sicoob.NumeroInstituicaoEmissora,
                NomeInstituicaoEmissora = sicoob.NomeInstituicaoEmissora,
                TipoPessoaBeneficiario = sicoob.TipoPessoaBeneficiario,
                NumeroCpfCnpjBeneficiario = sicoob.NumeroCpfCnpjBeneficiario,
                NomeRazaoSocialBeneficiario = sicoob.NomeRazaoSocialBeneficiario,
                NomeFantasiaBeneficiario = sicoob.NomeFantasiaBeneficiario,
                TipoPessoaPagador = sicoob.TipoPessoaPagador,
                NumeroCpfCnpjPagador = sicoob.NumeroCpfCnpjPagador,
                NomeRazaoSocialPagador = sicoob.NomeRazaoSocialPagador,
                CodigoBarras = sicoob.CodigoBarras,
                NumeroLinhaDigitavel = sicoob.NumeroLinhaDigitavel,
                DataVencimentoBoleto = sicoob.DataVencimentoBoleto,
                DataLimitePagamentoBoleto = sicoob.DataLimitePagamentoBoleto,
                ValorBoleto = sicoob.ValorBoleto,
                ValorAbatimentoDesconto = sicoob.ValorAbatimentoDesconto,
                ValorMultaMora = sicoob.ValorMultaMora,
                ValorPagamento = sicoob.ValorPagamento,
                DataPagamento = sicoob.DataPagamento,
                PermiteAlterarValor = sicoob.PermiteAlterarValor,
                ConsultaEmContingencia = sicoob.ConsultaEmContingencia,
                CodigoEspecieDocumento = sicoob.CodigoEspecieDocumento,
                CodigoSituacaoBoletoPagamento = sicoob.CodigoSituacaoBoletoPagamento,
                NossoNumero = sicoob.NossoNumero,
                NumeroDocumento = sicoob.NumeroDocumento,
                IdentificadorConsulta = sicoob.IdentificadorConsulta,
                DescricaoInstrucaoValorMinMax = sicoob.DescricaoInstrucaoValorMinMax,
                BloquearPagamento = sicoob.BloquearPagamento,
                MensagemBloqueioPagamento = sicoob.MensagemBloqueioPagamento,
            };
        }

        private static BancoBr.API.Base.Models.ComprovantePagamento MapToAgnostic(ComprovantePagamento sicoob)
        {
            if (sicoob == null) return null;

            return new BancoBr.API.Base.Models.ComprovantePagamento
            {
                NumeroAgencia = sicoob.NumeroAgencia,
                NomeAgencia = sicoob.NomeAgencia,
                NumeroConta = sicoob.NumeroConta,
                NomeProprietarioContaCorrente = sicoob.NomeProprietarioContaCorrente,
                NumeroLinhaDigitavel = sicoob.NumeroLinhaDigitavel,
                NumeroInstituicaoEmissora = sicoob.NumeroInstituicaoEmissora,
                NomeInstituicaoEmissora = sicoob.NomeInstituicaoEmissora,
                NumeroCpfCnpjBeneficiario = sicoob.NumeroCpfCnpjBeneficiario,
                NomeRazaoSocialBeneficiario = sicoob.NomeRazaoSocialBeneficiario,
                NumeroCpfCnpjPagador = sicoob.NumeroCpfCnpjPagador,
                NomeRazaoSocialPagador = sicoob.NomeRazaoSocialPagador,
                DataVencimento = sicoob.DataVencimento,
                ValorBoleto = sicoob.ValorBoleto,
                ValorAbatimentoDesconto = sicoob.ValorAbatimentoDesconto,
                ValorMultaMora = sicoob.ValorMultaMora,
                ValorPagamento = sicoob.ValorPagamento,
                DataPagamento = sicoob.DataPagamento,
                SituacaoPagamento = sicoob.SituacaoPagamento,
                DescricaoDetalheSituacao = sicoob.DescricaoDetalheSituacao,
                DataHoraCadastro = sicoob.DataHoraCadastro,
                AceitaValorDivergente = sicoob.AceitaValorDivergente,
                NossoNumero = sicoob.NossoNumero,
                NumeroDocumento = sicoob.NumeroDocumento,
                DescricaoObservacao = sicoob.DescricaoObservacao,
                DescricaoOuvidoria = sicoob.DescricaoOuvidoria,
                DescricaoTituloComprovante = sicoob.DescricaoTituloComprovante,
                IdPagamento = sicoob.IdPagamento,
                NumeroAutenticacaoPagamento = sicoob.NumeroAutenticacaoPagamento,
            };
        }

        private static BoletoPagamentoRequest MapToSicoob(BancoBr.API.Base.Models.BoletoPagamentoRequest agnostico)
        {
            return new BoletoPagamentoRequest
            {
                IdentificadorConsulta = agnostico.IdentificadorConsulta,
                ValorBoleto = agnostico.ValorBoleto,
                ValorDescontoAbatimento = agnostico.ValorDescontoAbatimento,
                ValorMultaMora = agnostico.ValorMultaMora,
                DescricaoObservacao = agnostico.DescricaoObservacao,
                AceitaValorDivergente = agnostico.AceitaValorDivergente,
                NumeroCpfCnpjPortador = agnostico.NumeroCpfCnpjPortador,
                NomePortador = agnostico.NomePortador,
                Amount = agnostico.Amount,
                Date = agnostico.Date,
                DebtorAccount = agnostico.DebtorAccount == null ? null : new DebtorAccount
                {
                    Issuer = agnostico.DebtorAccount.Issuer,
                    Number = agnostico.DebtorAccount.Number,
                    AccountType = agnostico.DebtorAccount.AccountType,
                    PersonType = agnostico.DebtorAccount.PersonType,
                },
            };
        }

        public override async Task<System.Collections.Generic.IReadOnlyList<BoletoDDA>> ConsultarBoletosDdaAsync(long numeroConta, DateTime dataInicial, DateTime dataFinal, SituacaoBoletoEnum situacao, TipoDataConsultaEnum tipoData, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos?numeroConta={numeroConta}&dataInicial={dataInicial:yyyy-MM-dd}&dataFinal={dataFinal:yyyy-MM-dd}&situacao={(int)situacao}&tipoData={(int)tipoData}";

            using (var response = await SendWithAuthAsync(() => BuildRequest(HttpMethod.Get, url, body: null, idempotencyKey: null), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return Array.Empty<BoletoDDA>();
                }

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<BoletoDDA[]>(body, SerializerSettings);
            }
        }

        private async Task<T> SendAsync<T>(HttpMethod method, string url, string body, string idempotencyKey, CancellationToken cancellationToken)
        {
            using (var response = await SendWithAuthAsync(() => BuildRequest(method, url, body, idempotencyKey), cancellationToken).ConfigureAwait(false))
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

            throw new SicoobApiException((int)response.StatusCode, errorResponse?.Mensagens ?? new System.Collections.Generic.List<SicoobMensagem>());
        }
    }
}
