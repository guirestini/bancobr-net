using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.Errors;
using BancoBr.API.Sicoob.Pagamentos.Pix.Models;
using BancoBr.Common.Core;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix
{
    /// <summary>
    /// Cliente para a API "Pix Pagamentos" do Sicoob, v2 — iniciação de pagamento por
    /// chave DICT e confirmação. Webhook não é implementado.
    /// </summary>
    public class PagamentoPixClient : PagamentoPixApiBase
    {
        /// <summary>
        /// Base URL, scopes e rate limit são intrínsecos a esta API específica do Sicoob
        /// (Pix Pagamentos v2) — não fazem sentido como configuração vinda do consumidor
        /// (ERP), que não deveria precisar conhecer detalhes da API do banco.
        /// </summary>
        public static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/pix-pagamentos/v2");

        private static readonly Uri DefaultTokenEndpoint = new Uri("https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token");

        private static readonly string[] Scopes = { "pixpagamentos_escrita", "pixpagamentos_consulta" };

        /// <summary>
        /// ISPB do Sicoob (Banco Cooperativo do Brasil), usado como "origem.ispb" no
        /// pagamento via QR Code. O Sicoob exige exatamente 8 caracteres
        /// (padrão ^[0-9A-Z]{8}$), por isso com zero à esquerda.
        /// </summary>
        private const string SicoobIspb = "54037916";

        /// <summary>
        /// Rate limit documentado para a API Pix Pagamentos: 1 requisição por segundo
        /// (diferente das APIs de Boletos/Convênios, que permitem 2/s).
        /// </summary>
        private const int RequestsPerSecond = 1;

        private readonly IAccessTokenProvider _tokenProvider;
        private readonly string _clientId;
        private readonly string _baseUrl;
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        internal PagamentoPixClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null)
            : this(clientId, certificateSource, BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint ?? DefaultTokenEndpoint))
        {
        }

        /// <summary>
        /// Usa o pipeline HTTP padrão (certificado mTLS + rate limiting) montado a partir de
        /// <paramref name="certificateSource"/>, mas com um provedor de token à escolha do
        /// chamador — por exemplo, <see cref="StaticAccessTokenProvider"/> quando o ambiente de
        /// sandbox fornece um Access Token (Bearer) pronto em vez de expor um endpoint OAuth2.
        /// </summary>
        internal PagamentoPixClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            : this(BuildHttpClient(certificateSource), tokenProvider, clientId, BaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake,
        /// sem certificado real nem chamadas HTTP de fato.
        /// </summary>
        public PagamentoPixClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl)
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

        public override async Task<BancoBr.API.Base.Models.RetornoPagamento> ConsultarPagamentoAsync(string endToEndId, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}pagamentos/{endToEndId}";
            var dto = await SendAsync<RetornoPagamento>(HttpMethod.Get, url, body: null, cancellationToken).ConfigureAwait(false);
            return MapRetornoPagamento(dto);
        }

        public override async Task<BancoBr.API.Base.Models.PixIniciacaoResponse> IniciarPagamentoAsync(string chave, DateTime? dataAgendamento, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}pagamentos";
            var json = JsonConvert.SerializeObject(new RequisicaoIniciacaoPix { Chave = chave, DataAgendamento = NormalizarDataAgendamento(dataAgendamento) }, SerializerSettings);

            var dto = await SendAsync<PixIniciacaoResponse>(HttpMethod.Post, url, json, cancellationToken).ConfigureAwait(false);
            return dto == null ? null : new BancoBr.API.Base.Models.PixIniciacaoResponse
            {
                EndToEndId = dto.EndToEndId,
                Chave = dto.Chave,
                Tipo = dto.Tipo,
                Proprietario = dto.Proprietario == null ? null : new BancoBr.API.Base.Models.PixProprietario
                {
                    Identificador = dto.Proprietario.Identificador,
                    Nome = dto.Proprietario.Nome,
                    Tipo = dto.Proprietario.Tipo,
                },
            };
        }

        public override async Task<BancoBr.API.Base.Models.RetornoPagamento> ConfirmarPagamentoAsync(BancoBr.API.Base.Models.RequisicaoEfetivacaoPix request, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}pagamentos/confirmacao";
            var wireRequest = new RequisicaoEfetivacaoPix
            {
                EndToEndId = request.EndToEndId,
                Valor = request.Valor.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ','),
                Descricao = request.Descricao,
                DataAgendamento = NormalizarDataAgendamento(request.DataAgendamento),
            };
            var json = JsonConvert.SerializeObject(wireRequest, SerializerSettings);

            var dto = await SendAsync<RetornoPagamento>(HttpMethod.Post, url, json, cancellationToken).ConfigureAwait(false);
            return MapRetornoPagamento(dto);
        }

        /// <summary>
        /// O Sicoob rejeita "dataAgendamento" quando a data não é estritamente posterior a
        /// hoje ("A data de agendamento deve ser maior do que o dia corrente."). Para
        /// pagamento no mesmo dia, o campo deve ser omitido do payload (liquidação imediata),
        /// em vez de enviado com a data de hoje.
        /// </summary>
        private static DateTime? NormalizarDataAgendamento(DateTime? dataAgendamento) =>
            dataAgendamento.HasValue && dataAgendamento.Value.Date <= DateTime.Today
                ? (DateTime?)null
                : dataAgendamento;

        public override async Task<BancoBr.API.Base.Models.PagamentoQrCodeResponse> PagarViaQrCodeAsync(BancoBr.API.Base.Models.RequisicaoPagamentoQrCode request, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}pagamentos/qrcode";
            var wireRequest = new RequisicaoPagamentoQrCode
            {
                QrCode = request.QrCode,
                Repeticao = request.Repeticao,
                DataVencimento = request.DataVencimento,
                DataAgendamento = request.DataAgendamento,
                Valor = request.Valor.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                PagarComJurosMulta = request.PagarComJurosMulta,
                Origem = request.Origem == null ? null : new DadosOrigemQrCode
                {
                    Ispb = SicoobIspb,
                    CpfCnpj = request.Origem.CpfCnpj.RemoveMascaraCpfCnpj(),
                    Nome = request.Origem.Nome,
                    Conta = request.Origem.Conta,
                    Agencia = request.Origem.Agencia,
                    Tipo = request.Origem.Tipo,
                },
                Destino = request.Destino == null ? null : new DadosDestinatarioQrCode
                {
                    CpfCnpj = request.Destino.CpfCnpj.RemoveMascaraCpfCnpj(),
                },
            };
            var json = JsonConvert.SerializeObject(wireRequest, SerializerSettings);

            var dto = await SendAsync<PagamentoQrCodeResponse>(HttpMethod.Post, url, json, cancellationToken).ConfigureAwait(false);
            return dto == null ? null : new BancoBr.API.Base.Models.PagamentoQrCodeResponse
            {
                EndToEndId = dto.EndToEndId,
                Estado = dto.Estado,
                Valor = dto.Valor,
                DetalheRejeicao = dto.DetalheRejeicao,
                Descricao = dto.Descricao,
                Horario = dto.Horario,
                Origem = MapDadosContaUsuario(dto.Origem),
                Destino = MapDadosContaUsuario(dto.Destino),
                DataAgendamento = dto.DataAgendamento,
                TxId = dto.TxId,
                ValorOriginal = dto.ValorOriginal,
                Abatimento = dto.Abatimento,
                Desconto = dto.Desconto,
                Multa = dto.Multa,
                Juros = dto.Juros,
                TipoQrcode = dto.TipoQrcode,
                BancoBrSituacao = MapEstadoParaSituacao(dto.Estado),
            };
        }

        private static BancoBr.API.Base.Models.RetornoPagamento MapRetornoPagamento(RetornoPagamento dto) => dto == null ? null : new BancoBr.API.Base.Models.RetornoPagamento
        {
            EndToEndId = dto.EndToEndId,
            Estado = dto.Estado,
            Valor = dto.Valor,
            DetalheRejeicao = dto.DetalheRejeicao,
            Descricao = dto.Descricao,
            Horario = dto.Horario,
            Origem = MapDadosContaUsuario(dto.Origem),
            Destino = MapDadosContaUsuario(dto.Destino),
            DataAgendamento = dto.DataAgendamento,
            BancoBrSituacao = MapEstadoParaSituacao(dto.Estado),
        };

        /// <summary>
        /// ATENÇÃO: mapeamento best-effort do campo textual "Estado" retornado pela API Pix
        /// Pagamentos do Sicoob para o enum agnóstico <see cref="BancoBr.API.Base.Models.BancoBrSituacaoEnum"/>.
        /// Os valores exatos possíveis de "Estado" NÃO estão confirmados neste código nem em
        /// documentação disponível no momento da implementação — esta lista foi construída a
        /// partir do vocabulário plausível de status de pagamento Pix. DEVE SER
        /// VALIDADO/AJUSTADO contra respostas reais do sandbox/produção do Sicoob antes de
        /// confiar neste mapeamento. Qualquer valor não reconhecido cai em NaoIntegrado, para
        /// nunca reportar falsamente Efetivado/Cancelado.
        /// </summary>
        private static BancoBr.API.Base.Models.BancoBrSituacaoEnum MapEstadoParaSituacao(string estado)
        {
            switch (estado?.Trim().ToUpperInvariant())
            {
                case "REALIZADO":
                case "LIQUIDADO":
                case "EFETIVADO":
                case "FINALIZADO":
                    return BancoBr.API.Base.Models.BancoBrSituacaoEnum.Efetivado;

                case "AGENDADO":
                case "EM_PROCESSAMENTO":
                case "PROCESSANDO":
                    return BancoBr.API.Base.Models.BancoBrSituacaoEnum.Agendado;

                case "NAO_REALIZADO":
                case "CANCELADO":
                case "DEVOLVIDO":
                    return BancoBr.API.Base.Models.BancoBrSituacaoEnum.Cancelado;

                case "REJEITADO":
                    return BancoBr.API.Base.Models.BancoBrSituacaoEnum.Rejeitado;

                default:
                    return BancoBr.API.Base.Models.BancoBrSituacaoEnum.NaoIntegrado;
            }
        }

        private static BancoBr.API.Base.Models.DadosContaUsuario MapDadosContaUsuario(DadosContaUsuario dto) => dto == null ? null : new BancoBr.API.Base.Models.DadosContaUsuario
        {
            Ispb = dto.Ispb,
            CpfCnpj = dto.CpfCnpj,
            Nome = dto.Nome,
            Conta = dto.Conta,
            Agencia = dto.Agencia,
            Tipo = dto.Tipo,
            ChaveDict = dto.ChaveDict,
            BoolFavorecido = dto.BoolFavorecido,
        };

        /// <summary>
        /// Ao contrário das APIs de Boletos/Convênios, a Pix Pagamentos não envelopa as
        /// respostas em { "resultado": ... } — o corpo já é o objeto desejado.
        /// </summary>
        private async Task<T> SendAsync<T>(HttpMethod method, string url, string body, CancellationToken cancellationToken)
        {
            using (var response = await SendWithAuthAsync(() => BuildRequest(method, url, body), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default;
                }

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                return JsonConvert.DeserializeObject<T>(responseBody, SerializerSettings);
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

        /// <summary>
        /// A Pix Pagamentos deveria retornar erros em application/problem+json
        /// ({ type, title, status, detail, violacoes[] }), diferente do
        /// { mensagens: [{ codigo, mensagem }] } usado por Boletos/Convênios — mas na
        /// prática o Sicoob também já respondeu nesse segundo formato para este endpoint.
        /// Como o Json.NET desserializa objetos JSON silenciosamente ignorando propriedades
        /// que não existem no tipo alvo (sem lançar exceção), tentar só o formato
        /// problem+json quando o corpo é, na verdade, { mensagens: [...] } resulta em um
        /// <see cref="PixErrorResponse"/> com todos os campos nulos — e a mensagem de erro
        /// real do banco era descartada silenciosamente. Por isso tentamos os dois formatos
        /// e, se nenhum produzir informação útil, caímos para o corpo bruto da resposta,
        /// para nunca esconder a causa real de uma falha do Sicoob.
        /// </summary>
        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var mensagens = new System.Collections.Generic.List<SicoobMensagem>();

            PixErrorResponse problemResponse;
            try
            {
                problemResponse = JsonConvert.DeserializeObject<PixErrorResponse>(body, SerializerSettings);
            }
            catch (JsonException)
            {
                problemResponse = null;
            }

            if (problemResponse != null && (!string.IsNullOrEmpty(problemResponse.Title) || !string.IsNullOrEmpty(problemResponse.Detail)))
            {
                mensagens.Add(new SicoobMensagem { Codigo = problemResponse.Title, Mensagem = problemResponse.Detail });
            }

            if (problemResponse?.Violacoes != null)
            {
                foreach (var violacao in problemResponse.Violacoes)
                {
                    mensagens.Add(new SicoobMensagem { Codigo = violacao.Propriedade, Mensagem = violacao.Razao });
                }
            }

            if (mensagens.Count == 0)
            {
                SicoobErrorResponse envelopeResponse;
                try
                {
                    envelopeResponse = JsonConvert.DeserializeObject<SicoobErrorResponse>(body, SerializerSettings);
                }
                catch (JsonException)
                {
                    envelopeResponse = null;
                }

                if (envelopeResponse?.Mensagens != null)
                {
                    mensagens.AddRange(envelopeResponse.Mensagens);
                }
            }

            if (mensagens.Count == 0 && !string.IsNullOrWhiteSpace(body))
            {
                mensagens.Add(new SicoobMensagem { Codigo = ((int)response.StatusCode).ToString(), Mensagem = body });
            }

            throw new SicoobApiException((int)response.StatusCode, mensagens);
        }
    }
}
