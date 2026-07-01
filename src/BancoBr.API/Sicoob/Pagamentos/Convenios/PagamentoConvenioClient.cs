using System;
using System.Collections.Generic;
using System.Linq;
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

        public override async Task<BancoBr.API.Base.Models.ConvenioConsultaResponse> ConsultarCodigoBarrasAsync(string codigoBarras, DateTime dataPagamento, bool? recebimentoViaCaixa = null, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/codigo-barras/{codigoBarras}?dataPagamento={dataPagamento:yyyy-MM-dd}";
            if (recebimentoViaCaixa.HasValue)
            {
                url += $"&recebimentoViaCaixa={recebimentoViaCaixa.Value.ToString().ToLowerInvariant()}";
            }

            var dto = await SendAsync<ConvenioConsultaResponse>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);
            return MapConvenioConsultaResponse(dto);
        }

        public override async Task<BancoBr.API.Base.Models.PagamentoConvenioResultado> PagarConvenioAsync(string codigoBarras, BancoBr.API.Base.Models.ArrecadacaoPagamentoRequest request, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/codigo-barras/{codigoBarras}/pagamentos";
            var wireRequest = MapArrecadacaoPagamentoRequest(request);
            var json = JsonConvert.SerializeObject(wireRequest, SerializerSettings);

            using (var response = await SendWithAuthAsync(() => BuildRequest(HttpMethod.Post, url, json), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    return BancoBr.API.Base.Models.PagamentoConvenioResultado.PendenteDeAssinatura();
                }

                try
                {
                    await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
                }
                catch (SicoobApiException ex) when (ex.Mensagens.Any(m => m.Codigo == SicoobErrorCodes.IdempotencyJaUtilizado))
                {
                    return await RecuperarPagamentoJaEfetivadoAsync(codigoBarras, request, cancellationToken).ConfigureAwait(false);
                }

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var envelope = JsonConvert.DeserializeObject<ResultadoEnvelope<ArrecadacaoResultado>>(body, SerializerSettings);
                return BancoBr.API.Base.Models.PagamentoConvenioResultado.Efetivado(MapArrecadacaoResultado(envelope.Resultado));
            }
        }

        /// <summary>
        /// Recupera o comprovante de um pagamento de convênio já efetivado em tentativa anterior
        /// (erro de negócio "idempotency/transação já utilizada"), consultando os pagamentos já
        /// registrados para o código de barras na mesma data de movimento e filtrando pela mesma
        /// transação enviada no pedido original.
        /// </summary>
        private async Task<BancoBr.API.Base.Models.PagamentoConvenioResultado> RecuperarPagamentoJaEfetivadoAsync(string codigoBarras, BancoBr.API.Base.Models.ArrecadacaoPagamentoRequest request, CancellationToken cancellationToken)
        {
            var pagamentos = await ConsultarPagamentosAsync(
                codigoBarras,
                request.Identificacao.Instituicao,
                request.Pagamento.DataPagamento,
                request.Transacao,
                cancellationToken).ConfigureAwait(false);

            var item = pagamentos?.FirstOrDefault(p => p.Transacao == request.Transacao);
            if (item == null)
            {
                throw new SicoobApiException((int)System.Net.HttpStatusCode.Conflict, new List<SicoobMensagem>
                {
                    new SicoobMensagem
                    {
                        Codigo = SicoobErrorCodes.IdempotencyJaUtilizado,
                        Mensagem = "Pagamento de convênio já efetivado, mas não foi possível localizar o comprovante para recuperação.",
                    },
                });
            }

            return BancoBr.API.Base.Models.PagamentoConvenioResultado.Efetivado(new BancoBr.API.Base.Models.ArrecadacaoResultado
            {
                Comprovante = null,
                Arrecadacao = new BancoBr.API.Base.Models.Arrecadacao
                {
                    ValorPago = item.ValorPago,
                    Nsu = item.Nsu,
                    DataPagamento = item.DataPagamento,
                    ValorDocumento = item.ValorDocumento,
                    ValorDesconto = item.ValorDesconto,
                    ValorJuros = item.ValorJuros,
                    ValorMulta = item.ValorMulta,
                    Autenticacao = item.Autenticacao,
                    RecebimentoViaCaixa = item.RecebimentoViaCaixa,
                },
            });
        }

        public override async Task<IReadOnlyList<BancoBr.API.Base.Models.ArrecadacaoConsultaItem>> ConsultarPagamentosAsync(string codigoBarras, long instituicao, DateTime dataMovimento, long? transacao = null, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/codigo-barras/{codigoBarras}/pagamentos?instituicao={instituicao}&dataMovimento={dataMovimento:yyyy-MM-dd}";
            if (transacao.HasValue)
            {
                url += $"&transacao={transacao.Value}";
            }

            var itens = await SendAsync<List<ArrecadacaoConsultaItem>>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);
            return itens?.Select(MapArrecadacaoConsultaItem).ToList();
        }

        public override async Task<BancoBr.API.Base.Models.ComprovanteArrecadacao> ConsultarComprovantePorNsuAsync(long nsu, long instituicao, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/pagamentos/{nsu}/comprovante?instituicao={instituicao}";
            var dto = await SendAsync<ComprovanteArrecadacao>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);
            return dto == null ? null : new BancoBr.API.Base.Models.ComprovanteArrecadacao
            {
                Comprovante = dto.Comprovante,
                Pagamento = MapArrecadacao(dto.Pagamento),
            };
        }

        public override async Task<IReadOnlyList<BancoBr.API.Base.Models.ConciliacaoItem>> ConsultarConciliacoesAsync(long instituicao, DateTime dataMovimento, int? unidade = null, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/conciliacoes?dataMovimento={dataMovimento:yyyy-MM-dd}&instituicao={instituicao}";
            if (unidade.HasValue)
            {
                url += $"&unidade={unidade.Value}";
            }

            var itens = await SendAsync<List<ConciliacaoItem>>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);
            return itens?.Select(dto => new BancoBr.API.Base.Models.ConciliacaoItem
            {
                Situacao = dto.Situacao,
                Convenio = dto.Convenio,
                SiglaConvenio = dto.SiglaConvenio,
                ValorTotal = dto.ValorTotal,
                Quantidade = dto.Quantidade,
            }).ToList();
        }

        public override async Task<IReadOnlyList<BancoBr.API.Base.Models.ConvenioHabilitado>> ConsultarConveniosHabilitadosAsync(long transacao, long instituicao, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/convenios-habilitados?transacao={transacao}&instituicao={instituicao}";
            var itens = await SendAsync<List<ConvenioHabilitado>>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);
            return itens?.Select(dto => new BancoBr.API.Base.Models.ConvenioHabilitado
            {
                Identificador = dto.Identificador,
                Sigla = dto.Sigla,
                CodigoFebraban = dto.CodigoFebraban,
                Segmento = dto.Segmento,
            }).ToList();
        }

        private static BancoBr.API.Base.Models.ConvenioConsultaResponse MapConvenioConsultaResponse(ConvenioConsultaResponse dto) => dto == null ? null : new BancoBr.API.Base.Models.ConvenioConsultaResponse
        {
            Convenio = dto.Convenio,
            SiglaConvenio = dto.SiglaConvenio,
            ValorDocumento = dto.ValorDocumento,
            ValorDesconto = dto.ValorDesconto,
            ValorMulta = dto.ValorMulta,
            ValorJuros = dto.ValorJuros,
            ValorOutrosEncargos = dto.ValorOutrosEncargos,
            ValorTotal = dto.ValorTotal,
            CodigoConvenioFebraban = dto.CodigoConvenioFebraban,
            Nsu = dto.Nsu,
            Transacao = dto.Transacao,
        };

        private static ArrecadacaoPagamentoRequest MapArrecadacaoPagamentoRequest(BancoBr.API.Base.Models.ArrecadacaoPagamentoRequest request) => new ArrecadacaoPagamentoRequest
        {
            Identificacao = new Identificacao
            {
                Instituicao = request.Identificacao.Instituicao,
                Unidade = request.Identificacao.Unidade,
            },
            Pagamento = new PagamentoConvenio
            {
                ValorPago = request.Pagamento.ValorPago,
                Nsu = request.Pagamento.Nsu,
                DataPagamento = request.Pagamento.DataPagamento,
                ValorDocumento = request.Pagamento.ValorDocumento,
                ValorDesconto = request.Pagamento.ValorDesconto,
                ValorJuros = request.Pagamento.ValorJuros,
                ValorMulta = request.Pagamento.ValorMulta,
                RecebimentoViaCaixa = request.Pagamento.RecebimentoViaCaixa,
            },
            Transacao = request.Transacao,
        };

        private static BancoBr.API.Base.Models.Arrecadacao MapArrecadacao(Arrecadacao dto) => dto == null ? null : new BancoBr.API.Base.Models.Arrecadacao
        {
            ValorPago = dto.ValorPago,
            Nsu = dto.Nsu,
            DataPagamento = dto.DataPagamento,
            ValorDocumento = dto.ValorDocumento,
            ValorDesconto = dto.ValorDesconto,
            ValorJuros = dto.ValorJuros,
            ValorMulta = dto.ValorMulta,
            Autenticacao = dto.Autenticacao,
            RecebimentoViaCaixa = dto.RecebimentoViaCaixa,
        };

        private static BancoBr.API.Base.Models.ArrecadacaoResultado MapArrecadacaoResultado(ArrecadacaoResultado dto) => dto == null ? null : new BancoBr.API.Base.Models.ArrecadacaoResultado
        {
            Comprovante = dto.Comprovante,
            Arrecadacao = MapArrecadacao(dto.Arrecadacao),
        };

        /// <summary>
        /// ATENÇÃO: mapeamento best-effort do campo "situacao.descricao" (e, em caráter secundário,
        /// "situacao.codigo") retornado pela API de Arrecadação/Convênios do Sicoob para o enum
        /// agnóstico <see cref="BancoBr.API.Base.Models.BancoBrSituacaoEnum"/>. O único valor
        /// confirmado em testes/documentação disponível neste repositório é descricao "Recebido"
        /// com codigo 0. Os demais valores abaixo foram inferidos a partir do vocabulário plausível
        /// de status de arrecadação e NÃO estão confirmados — DEVEM SER VALIDADOS/AJUSTADOS contra
        /// respostas reais do sandbox/produção do Sicoob antes de confiar neste mapeamento. Qualquer
        /// valor não reconhecido cai em NaoIntegrado, para nunca reportar falsamente
        /// Efetivado/Cancelado.
        /// </summary>
        private static BancoBr.API.Base.Models.BancoBrSituacaoEnum MapSituacaoArrecadacaoParaSituacao(SituacaoArrecadacao situacao)
        {
            if (situacao == null) return BancoBr.API.Base.Models.BancoBrSituacaoEnum.NaoIntegrado;

            switch (situacao.Descricao?.Trim().ToUpperInvariant())
            {
                case "RECEBIDO":
                case "PAGO":
                case "EFETIVADO":
                    return BancoBr.API.Base.Models.BancoBrSituacaoEnum.Efetivado;

                case "AGENDADO":
                case "PENDENTE":
                case "EM_PROCESSAMENTO":
                    return BancoBr.API.Base.Models.BancoBrSituacaoEnum.Agendado;

                case "CANCELADO":
                case "ESTORNADO":
                    return BancoBr.API.Base.Models.BancoBrSituacaoEnum.Cancelado;

                case "REJEITADO":
                    return BancoBr.API.Base.Models.BancoBrSituacaoEnum.Rejeitado;

                default:
                    return BancoBr.API.Base.Models.BancoBrSituacaoEnum.NaoIntegrado;
            }
        }

        private static BancoBr.API.Base.Models.ArrecadacaoConsultaItem MapArrecadacaoConsultaItem(ArrecadacaoConsultaItem dto) => new BancoBr.API.Base.Models.ArrecadacaoConsultaItem
        {
            ValorPago = dto.ValorPago,
            Nsu = dto.Nsu,
            DataPagamento = dto.DataPagamento,
            ValorDocumento = dto.ValorDocumento,
            ValorDesconto = dto.ValorDesconto,
            ValorJuros = dto.ValorJuros,
            ValorMulta = dto.ValorMulta,
            IdentificadorFgts = dto.IdentificadorFgts,
            AnoExercicio = dto.AnoExercicio,
            RecebimentoViaCaixa = dto.RecebimentoViaCaixa,
            Autenticacao = dto.Autenticacao,
            Situacao = dto.Situacao == null ? null : new BancoBr.API.Base.Models.SituacaoArrecadacao
            {
                Codigo = dto.Situacao.Codigo,
                Descricao = dto.Situacao.Descricao,
            },
            BancoBrSituacao = MapSituacaoArrecadacaoParaSituacao(dto.Situacao),
            Convenio = dto.Convenio,
            SiglaConvenio = dto.SiglaConvenio,
            Transacao = dto.Transacao,
        };

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
