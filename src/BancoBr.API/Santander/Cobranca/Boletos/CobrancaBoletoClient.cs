using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Base.Models;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Santander.Cobranca.Boletos.Models;
using BancoBr.API.Santander.Http;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.API.Santander.Cobranca.Boletos
{
    /// <summary>
    /// Cliente para a API "Hub de Cobrança" do Santander (collection_bill_management v2) —
    /// registro de boleto de cobrança com Pix (Bolepix). Consulta, baixa e webhook não são
    /// implementados.
    ///
    /// Toda chamada acontece dentro de uma workspace do Hub de Cobrança. O cliente a resolve
    /// sozinho pelo convênio do beneficiário (<see cref="Correntista.Convenio"/>): usa a
    /// workspace ativa de cobrança que contém o convênio. Ao contrário do Pix, não cria
    /// workspace — o cadastro do convênio no Hub é uma contratação, não uma flag.
    ///
    /// Assim como nos demais clientes, esta classe é o limite de mapeamento entre o contrato
    /// público (<see cref="Movimento"/>/<see cref="MovimentoItemBoletoCobranca"/>) e os DTOs de
    /// wire (<c>Boletos.Models.*</c>).
    /// </summary>
    internal class CobrancaBoletoClient : CobrancaBoletoApiBase
    {
        public static readonly Uri BaseUrl = new Uri("https://trust-open.api.santander.com.br/collection_bill_management/v2");

        /// <summary>Tipo da workspace do Hub de Cobrança (suposto; a documentação recebida cobre só o POST bank_slips).</summary>
        private const string TipoWorkspaceCobranca = "BILLING";

        private static readonly HashSet<string> Ufs = new HashSet<string>
        {
            "RR", "AM", "AP", "PA", "AC", "RO", "TO", "MA", "PI", "CE", "RN", "PB", "PE", "AL", "SE", "BA", "MT", "DF", "GO", "MS", "MG", "ES", "RJ", "SP", "PR", "SC", "RS",
        };

        private readonly SantanderHttpGateway _gateway;
        private readonly string _baseUrl;
        private readonly Dictionary<string, string> _workspaces = new Dictionary<string, string>();
        private readonly SemaphoreSlim _workspacesLock = new SemaphoreSlim(1, 1);

        internal CobrancaBoletoClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null)
            : this(clientId, certificateSource, SantanderHttpGateway.BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint))
        {
        }

        internal CobrancaBoletoClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            : this(SantanderHttpGateway.BuildHttpClient(certificateSource), tokenProvider, clientId, BaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake.
        /// </summary>
        public CobrancaBoletoClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl)
            : base(httpClient)
        {
            if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));

            _gateway = new SantanderHttpGateway(httpClient, tokenProvider, clientId);
            var baseUrlText = baseUrl.ToString();
            _baseUrl = baseUrlText.EndsWith("/") ? baseUrlText : baseUrlText + "/";
        }

        #region ::. Operações .::

        internal override async Task<Movimento> EmitirBoletoAsync(Movimento movimento, Correntista beneficiario, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            var item = movimento.MovimentoItem as MovimentoItemBoletoCobranca;
            if (item == null)
                throw new InvalidOperationException("EmitirBoletoAsync espera um MovimentoItemBoletoCobranca em Movimento.MovimentoItem.");

            var wireRequest = MontarRequisicao(movimento, item, beneficiario);
            var workspaceId = await ResolverWorkspaceAsync(wireRequest.CovenantCode, cancellationToken).ConfigureAwait(false);

            var dto = await _gateway.SendAsync<BoletoCobrancaResponse>(HttpMethod.Post, $"{_baseUrl}workspaces/{workspaceId}/bank_slips", wireRequest, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, item, dto);
        }

        /// <summary>
        /// Só há retorno de liquidação: a listagem de boletos do Hub filtra por status LIQUIDADO e
        /// pagina de 50 em 50 — todas as páginas são lidas. A workspace pode conter outros
        /// convênios, então o resultado é restrito ao do beneficiário.
        /// </summary>
        internal override async Task<IReadOnlyList<Movimento>> ConsultarRetornoAsync(Correntista beneficiario, TipoRetornoCobrancaEnum tipo, DateTime dataInicial, DateTime dataFinal, TipoDataRetornoCobrancaEnum tipoData, string nossoNumero, string seuNumero, CancellationToken cancellationToken = default)
        {
            if (tipo != TipoRetornoCobrancaEnum.Liquidacao)
                throw new NotSupportedException($"O retorno de cobrança do tipo {tipo} não é suportado pelo Santander.");

            var convenio = ExigirConvenio(beneficiario);

            if (dataFinal.Date < dataInicial.Date)
                throw new ArgumentException("A data final do período não pode ser anterior à data inicial.", nameof(dataFinal));

            if (!string.IsNullOrEmpty(nossoNumero) && !Regex.IsMatch(nossoNumero, @"^[0-9]{1,13}$"))
                throw new ArgumentException("O nosso número deve ter de 1 a 13 dígitos.", nameof(nossoNumero));

            if (!string.IsNullOrEmpty(seuNumero) && !Regex.IsMatch(seuNumero, @"^[0-9a-zA-Z ]{1,15}$"))
                throw new ArgumentException("O seu número aceita até 15 caracteres alfanuméricos.", nameof(seuNumero));

            var workspaceId = await ResolverWorkspaceAsync(convenio, cancellationToken).ConfigureAwait(false);

            var filtro = new StringBuilder("status=LIQUIDADO");
            var prefixoData = tipoData == TipoDataRetornoCobrancaEnum.Vencimento ? "dueDate" : "paymentDate";
            filtro.Append($"&{prefixoData}Initial={dataInicial:yyyy-MM-dd}&{prefixoData}Final={dataFinal:yyyy-MM-dd}");
            if (!string.IsNullOrEmpty(nossoNumero)) filtro.Append($"&bankNumber={nossoNumero}");
            if (!string.IsNullOrEmpty(seuNumero)) filtro.Append($"&clientNumber={Uri.EscapeDataString(seuNumero)}");

            const int limite = 50;
            var chave = NormalizarConvenio(convenio);
            var retorno = new List<Movimento>();
            var lidos = 0;

            while (true)
            {
                var url = $"{_baseUrl}workspaces/{workspaceId}/bank_slips?{filtro}&_limit={limite}&_offset={lidos}";
                var dto = await _gateway.SendAsync<RetornoBoletoCobrancaListResponse>(HttpMethod.Get, url, body: null, cancellationToken).ConfigureAwait(false);
                if (dto?.Content == null || dto.Content.Count == 0)
                    break;

                lidos += dto.Content.Count;
                retorno.AddRange(dto.Content
                    .Where(b => string.IsNullOrEmpty(b.CovenantCode) || NormalizarConvenio(b.CovenantCode) == chave)
                    .Select(MapRetornoLiquidacao));

                if (dto.Pageable == null || lidos >= dto.Pageable.TotalElements)
                    break;
            }

            return retorno;
        }

        #endregion

        #region ::. Workspace .::

        /// <summary>
        /// Acha a workspace ativa de cobrança que contém o convênio. O resultado fica em cache
        /// por convênio, e a trava evita listar duas vezes em chamadas concorrentes.
        /// </summary>
        private async Task<string> ResolverWorkspaceAsync(string convenio, CancellationToken cancellationToken)
        {
            var chave = NormalizarConvenio(convenio);

            await _workspacesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_workspaces.TryGetValue(chave, out var emCache))
                    return emCache;

                var existentes = await ListarWorkspacesAsync(cancellationToken).ConfigureAwait(false);
                var workspace = existentes.FirstOrDefault(w =>
                    w.Status == "ACTIVE"
                    && w.Type == TipoWorkspaceCobranca
                    && w.Covenants != null
                    && w.Covenants.Any(c => NormalizarConvenio(c.Code) == chave));

                if (workspace == null)
                    throw new InvalidOperationException($"Não há workspace ativa do Hub de Cobrança do Santander com o convênio {convenio}. Cadastre o convênio numa workspace de cobrança no Hub antes de emitir boletos.");

                _workspaces[chave] = workspace.IdResolvido;
                return workspace.IdResolvido;
            }
            finally
            {
                _workspacesLock.Release();
            }
        }

        private async Task<List<WorkspaceCobranca>> ListarWorkspacesAsync(CancellationToken cancellationToken)
        {
            const int limite = 50;
            var todas = new List<WorkspaceCobranca>();

            while (true)
            {
                var dto = await _gateway.SendAsync<WorkspaceCobrancaListResponse>(HttpMethod.Get, $"{_baseUrl}workspaces?_limit={limite}&_offset={todas.Count}", body: null, cancellationToken).ConfigureAwait(false);
                if (dto?.Content == null || dto.Content.Count == 0)
                    break;

                todas.AddRange(dto.Content);
                if (dto.Pageable == null || todas.Count >= dto.Pageable.TotalElements)
                    break;
            }

            return todas;
        }

        /// <summary>Ignora zeros à esquerda, já que o código do convênio pode vir preenchido até o tamanho máximo.</summary>
        private static string NormalizarConvenio(string convenio) => (convenio ?? string.Empty).Trim().TrimStart('0');

        #endregion

        #region ::. Mapeamento de requisição .::

        private static string ExigirConvenio(Correntista beneficiario)
        {
            if (beneficiario == null)
                throw new ArgumentNullException(nameof(beneficiario), "O Santander exige o beneficiário (com o convênio de cobrança) para localizar a workspace do Hub de Cobrança.");

            var convenio = (beneficiario.Convenio ?? string.Empty).Trim();
            if (!Regex.IsMatch(convenio, @"^[0-9]{1,9}$"))
                throw new InvalidOperationException("O convênio de cobrança (Correntista.Convenio) deve ter de 1 a 9 dígitos.");

            return convenio;
        }

        private static RequisicaoBoletoCobranca MontarRequisicao(Movimento movimento, MovimentoItemBoletoCobranca item, Correntista beneficiario)
        {
            var convenio = ExigirConvenio(beneficiario);

            if (string.IsNullOrEmpty(item.NossoNumero) || !Regex.IsMatch(item.NossoNumero, @"^[0-9]{1,13}$"))
                throw new InvalidOperationException("O nosso número (NossoNumero) deve ter de 1 a 13 dígitos.");

            if (movimento.ValorPagamento <= 0)
                throw new InvalidOperationException("O valor nominal do boleto (Movimento.ValorPagamento) deve ser maior que zero.");

            if (!string.IsNullOrEmpty(movimento.NumeroDocumento) && !Regex.IsMatch(movimento.NumeroDocumento, @"^[0-9a-zA-Z ]{1,15}$"))
                throw new InvalidOperationException("O seu número (Movimento.NumeroDocumento) aceita até 15 caracteres alfanuméricos.");

            if (!string.IsNullOrEmpty(item.CodigoParticipante) && !Regex.IsMatch(item.CodigoParticipante, @"^[0-9a-zA-Z ]{1,25}$"))
                throw new InvalidOperationException("O controle do participante (CodigoParticipante) aceita até 25 caracteres alfanuméricos.");

            var emissao = (item.DataEmissao ?? DateTime.Today).Date;
            if (item.DataVencimento == default(DateTime))
                throw new InvalidOperationException("Informe a data de vencimento do boleto (DataVencimento).");
            if (item.DataVencimento.Date < emissao)
                throw new InvalidOperationException("A data de vencimento do boleto não pode ser anterior à data de emissão.");

            if (!string.IsNullOrEmpty(item.TxId) && (item.TxId.Length < 26 || item.TxId.Length > 35))
                throw new InvalidOperationException("O TxId do Bolepix deve ter de 26 a 35 caracteres.");

            GarantirNsu(item);

            var wireRequest = new RequisicaoBoletoCobranca
            {
                Environment = item.AmbienteTeste ? "TESTE" : "PRODUCAO",
                NsuCode = item.NsuCode,
                NsuDate = item.DataNsu.Value,
                CovenantCode = convenio,
                BankNumber = item.NossoNumero,
                ClientNumber = string.IsNullOrEmpty(movimento.NumeroDocumento) ? null : movimento.NumeroDocumento,
                DueDate = item.DataVencimento,
                IssueDate = emissao,
                ParticipantCode = string.IsNullOrEmpty(item.CodigoParticipante) ? null : item.CodigoParticipante,
                NominalValue = Decimal2(movimento.ValorPagamento),
                Payer = MontarPagador(movimento.Favorecido),
                Beneficiary = MontarBeneficiarioFinal(item.BeneficiarioFinal),
                DocumentKind = MapEspecie(item.EspecieDocumento),
                Discount = MontarDesconto(item),
                FinePercentage = Decimal2(item.PercentualMulta),
                FineQuantityDays = Inteiro(item.DiasMulta),
                InterestPercentage = Decimal2(item.PercentualJuros),
                DeductionValue = Decimal2(item.ValorAbatimento),
                ProtestType = MapProtesto(item.TipoProtesto),
                ProtestQuantityDays = Inteiro(item.DiasProtesto),
                WriteOffQuantityDays = Inteiro(item.DiasBaixa),
                PaymentType = MapTipoPagamento(item.TipoPagamento),
                ParcelsQuantity = Inteiro(item.QuantidadeParciais),
                ValueType = item.TipoValorParcial.HasValue ? MapTipoValorParcial(item.TipoValorParcial.Value) : null,
                MinValueOrPercentage = Decimal2(item.ValorMinimo),
                MaxValueOrPercentage = Decimal2(item.ValorMaximo),
                IofPercentage = item.PercentualIOF.HasValue ? item.PercentualIOF.Value.ToString("F5", CultureInfo.InvariantCulture) : null,
                Sharing = MontarPartilha(item.Partilha),
                Key = MontarChave(item),
                TxId = string.IsNullOrEmpty(item.TxId) ? null : item.TxId,
                Messages = item.Mensagens != null && item.Mensagens.Count > 0 ? item.Mensagens : null,
            };

            return wireRequest;
        }

        /// <summary>
        /// O NSU identifica a chamada no banco (exclusivo por dia). Gerado uma vez e gravado no
        /// item, para que um retry da mesma emissão reuse o mesmo NSU e não duplique o boleto.
        /// </summary>
        private static void GarantirNsu(MovimentoItemBoletoCobranca item)
        {
            if (string.IsNullOrEmpty(item.NsuCode))
            {
                var sufixo = Guid.NewGuid().ToString("N").ToUpperInvariant();
                item.NsuCode = item.AmbienteTeste ? "TST" + sufixo.Substring(0, 17) : sufixo.Substring(0, 20);
            }
            else if (!Regex.IsMatch(item.NsuCode, @"^[A-Za-z0-9]{1,20}$"))
            {
                throw new InvalidOperationException("O NSU (NsuCode) aceita de 1 a 20 caracteres alfanuméricos.");
            }

            if (!item.DataNsu.HasValue)
                item.DataNsu = DateTime.Today;
        }

        private static BoletoPagador MontarPagador(Favorecido pagador)
        {
            if (pagador == null)
                throw new InvalidOperationException("Informe o pagador do boleto em Movimento.Favorecido.");

            var documento = pagador.CPF_CNPJ.RemoveMascaraCpfCnpj();
            if (string.IsNullOrWhiteSpace(documento))
                throw new InvalidOperationException("O pagador exige o CPF/CNPJ (Favorecido.CPF_CNPJ).");

            var endereco = string.IsNullOrWhiteSpace(pagador.NumeroEndereco) ? pagador.Endereco : $"{pagador.Endereco}, {pagador.NumeroEndereco}";
            var uf = (pagador.UF ?? string.Empty).Trim().ToUpperInvariant();
            if (!Ufs.Contains(uf))
                throw new InvalidOperationException("O pagador exige uma UF válida (Favorecido.UF).");

            if (pagador.CEP <= 0 || pagador.CEP > 99999999)
                throw new InvalidOperationException("O pagador exige um CEP válido (Favorecido.CEP).");

            var cep = pagador.CEP.ToString("D8");

            return new BoletoPagador
            {
                Name = ExigirTexto(pagador.Nome, "nome do pagador (Favorecido.Nome)", 40),
                DocumentType = documento.Length > 11 ? "CNPJ" : "CPF",
                DocumentNumber = documento,
                Address = ExigirTexto(endereco, "endereço do pagador (Favorecido.Endereco)", 40),
                Neighborhood = ExigirTexto(pagador.Bairro, "bairro do pagador (Favorecido.Bairro)", 30),
                City = ExigirTexto(pagador.Cidade, "cidade do pagador (Favorecido.Cidade)", 20),
                State = uf,
                ZipCode = cep.Substring(0, 5) + "-" + cep.Substring(5),
            };
        }

        private static BoletoBeneficiarioFinal MontarBeneficiarioFinal(Pessoa pessoa)
        {
            if (pessoa == null)
                return null;

            var documento = pessoa.CPF_CNPJ.RemoveMascaraCpfCnpj();

            return new BoletoBeneficiarioFinal
            {
                Name = string.IsNullOrWhiteSpace(pessoa.Nome) ? null : ExigirTexto(pessoa.Nome, "nome do beneficiário final", 40),
                DocumentType = string.IsNullOrWhiteSpace(documento) ? null : (documento.Length > 11 ? "CNPJ" : "CPF"),
                DocumentNumber = string.IsNullOrWhiteSpace(documento) ? null : documento,
            };
        }

        private static BoletoDesconto MontarDesconto(MovimentoItemBoletoCobranca item)
        {
            var desconto = new BoletoDesconto { Type = MapTipoDesconto(item.TipoDesconto) };
            if (item.TipoDesconto == TipoDescontoBoletoEnum.Isento)
                return desconto;

            var descontos = item.Descontos ?? new List<DescontoBoleto>();
            if (descontos.Count == 0 || descontos.Count > 3)
                throw new InvalidOperationException("Informe de 1 a 3 descontos (Descontos) quando o tipo de desconto não for Isento.");

            desconto.DiscountOne = MontarDescontoItem(descontos[0]);
            desconto.DiscountTwo = descontos.Count > 1 ? MontarDescontoItem(descontos[1]) : null;
            desconto.DiscountThree = descontos.Count > 2 ? MontarDescontoItem(descontos[2]) : null;

            return desconto;
        }

        private static BoletoDescontoItem MontarDescontoItem(DescontoBoleto desconto) =>
            new BoletoDescontoItem { Value = desconto.Valor, LimitDate = desconto.DataLimite };

        private static List<BoletoPartilha> MontarPartilha(List<PartilhaBoleto> partilha)
        {
            if (partilha == null || partilha.Count == 0)
                return null;

            return partilha.Select(p =>
            {
                if (string.IsNullOrEmpty(p.Codigo) || !Regex.IsMatch(p.Codigo, @"^\d{2}$"))
                    throw new InvalidOperationException("O código de partilha (PartilhaBoleto.Codigo) deve ter 2 dígitos.");

                return new BoletoPartilha { Code = p.Codigo, Value = Decimal2(p.Valor) };
            }).ToList();
        }

        private static BoletoChaveDict MontarChave(MovimentoItemBoletoCobranca item)
        {
            if (string.IsNullOrWhiteSpace(item.ChavePIX))
                return null;

            return new BoletoChaveDict { Type = MapTipoChave(item), DictKey = item.ChavePIX };
        }

        private static string ExigirTexto(string valor, string descricao, int tamanhoMaximo)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new InvalidOperationException($"Informe o {descricao}.");

            valor = valor.Trim();
            if (valor.Length > tamanhoMaximo)
                throw new InvalidOperationException($"O {descricao} aceita até {tamanhoMaximo} caracteres.");

            return valor;
        }

        private static string Decimal2(decimal valor) => valor.ToString("F2", CultureInfo.InvariantCulture);

        private static string Decimal2(decimal? valor) => valor.HasValue ? Decimal2(valor.Value) : null;

        private static string Inteiro(int? valor) => valor.HasValue ? valor.Value.ToString(CultureInfo.InvariantCulture) : null;

        private static string MapEspecie(EspecieDocumentoBoletoEnum especie)
        {
            switch (especie)
            {
                case EspecieDocumentoBoletoEnum.DuplicataServico: return "DUPLICATA_SERVICO";
                case EspecieDocumentoBoletoEnum.NotaPromissoria: return "NOTA_PROMISSORIA";
                case EspecieDocumentoBoletoEnum.NotaPromissoriaRural: return "NOTA_PROMISSORIA_RURAL";
                case EspecieDocumentoBoletoEnum.Recibo: return "RECIBO";
                case EspecieDocumentoBoletoEnum.ApoliceSeguro: return "APOLICE_SEGURO";
                case EspecieDocumentoBoletoEnum.BoletoCartaoCredito: return "BOLETO_CARTAO_CREDITO";
                case EspecieDocumentoBoletoEnum.BoletoProposta: return "BOLETO_PROPOSTA";
                case EspecieDocumentoBoletoEnum.BoletoDepositoAporte: return "BOLETO_DEPOSITO_APORTE";
                case EspecieDocumentoBoletoEnum.Cheque: return "CHEQUE";
                case EspecieDocumentoBoletoEnum.NotaPromissoriaDireta: return "NOTA_PROMISSORIA_DIRETA";
                case EspecieDocumentoBoletoEnum.Outros: return "OUTROS";
                default: return "DUPLICATA_MERCANTIL";
            }
        }

        private static string MapTipoDesconto(TipoDescontoBoletoEnum tipo)
        {
            switch (tipo)
            {
                case TipoDescontoBoletoEnum.ValorDataFixa: return "VALOR_DATA_FIXA";
                case TipoDescontoBoletoEnum.ValorDiaCorrido: return "VALOR_DIA_CORRIDO";
                case TipoDescontoBoletoEnum.ValorDiaUtil: return "VALOR_DIA_UTIL";
                default: return "ISENTO";
            }
        }

        private static string MapProtesto(TipoProtestoBoletoEnum tipo)
        {
            switch (tipo)
            {
                case TipoProtestoBoletoEnum.DiasCorridos: return "DIAS_CORRIDOS";
                case TipoProtestoBoletoEnum.DiasUteis: return "DIAS_UTEIS";
                case TipoProtestoBoletoEnum.CadastroConvenio: return "CADASTRO_CONVENIO";
                default: return "SEM_PROTESTO";
            }
        }

        private static string MapTipoPagamento(TipoPagamentoBoletoEnum tipo)
        {
            switch (tipo)
            {
                case TipoPagamentoBoletoEnum.Divergente: return "DIVERGENTE";
                case TipoPagamentoBoletoEnum.Parcial: return "PARCIAL";
                default: return "REGISTRO";
            }
        }

        private static string MapTipoValorParcial(TipoValorPagamentoParcialEnum tipo) =>
            tipo == TipoValorPagamentoParcialEnum.Valor ? "VALOR" : "PERCENTUAL";

        private static string MapTipoChave(MovimentoItemBoletoCobranca item)
        {
            switch (item.TipoChavePIX)
            {
                case FormaIniciacaoEnum.PIX_Telefone:
                    return "CELULAR";

                case FormaIniciacaoEnum.PIX_Email:
                    return "EMAIL";

                case FormaIniciacaoEnum.PIX_Aleatorio:
                    return "EVP";

                case FormaIniciacaoEnum.PIX_CPF_CNPJ:
                    return new string(item.ChavePIX.Where(char.IsDigit).ToArray()).Length > 11 ? "CNPJ" : "CPF";

                default:
                    throw new InvalidOperationException("O tipo da chave Pix (TipoChavePIX) não foi informado no movimento.");
            }
        }

        #endregion

        #region ::. Mapeamento de resposta .::

        private static Movimento MapRetornoLiquidacao(RetornoBoletoCobranca dto)
        {
            var pagamento = dto.Payment;
            var dataPagamento = ParseData(pagamento?.Date);

            return new Movimento
            {
                Favorecido = MapPagador(dto.Payer),
                NumeroDocumento = dto.ClientNumber,
                NumeroDocumentoNoBanco = dto.BankNumber,
                ValorPagamento = ParseValor(pagamento?.PaidValue) ?? 0m,
                DataPagamento = dataPagamento ?? default(DateTime),
                SituacaoBancoBr = MapStatusParaSituacao(dto.Status),
                HorarioBancoBr = dataPagamento,
                MovimentoItem = new MovimentoItemRetornoCobranca
                {
                    TipoRetorno = TipoRetornoCobrancaEnum.Liquidacao,
                    Convenio = dto.CovenantCode,
                    NossoNumero = dto.BankNumber,
                    NsuCode = dto.NsuCode,
                    DataNsu = ParseData(dto.NsuDate),
                    StatusBanco = dto.Status,
                    ComplementoStatusBanco = dto.StatusComplement,
                    ValorNominal = ParseValor(dto.NominalValue),
                    DataVencimento = ParseData(dto.DueDate),
                    DataEmissao = ParseData(dto.IssueDate),
                    DataEntrada = ParseData(dto.EntryDate),
                    CodigoBarras = dto.Barcode,
                    LinhaDigitavel = dto.DigitableLine,
                    BeneficiarioFinal = MapBeneficiarioFinal(dto.Beneficiary),
                    ValorJuros = ParseValor(pagamento?.InterestValue),
                    ValorMulta = ParseValor(pagamento?.FineValue),
                    ValorAbatimento = ParseValor(pagamento?.DeductionValue),
                    ValorDesconto = ParseValor(pagamento?.RebateValue),
                    ValorIOF = ParseValor(pagamento?.IofValue),
                    TipoPagamento = pagamento?.Type,
                    BancoPagamento = pagamento?.BankCode,
                    CanalPagamento = pagamento?.Channel,
                    EspeciePagamento = pagamento?.Kind,
                    TxId = pagamento?.TxId,
                },
            };
        }

        private static Favorecido MapPagador(BoletoPagador pagador)
        {
            if (pagador == null)
                return null;

            int.TryParse(pagador.ZipCode.JustNumbers(), out var cep);

            return new Favorecido
            {
                Nome = pagador.Name,
                TipoPessoa = MapTipoPessoa(pagador.DocumentType),
                CPF_CNPJ = pagador.DocumentNumber,
                Endereco = pagador.Address,
                Bairro = pagador.Neighborhood,
                Cidade = pagador.City,
                UF = pagador.State,
                CEP = cep,
            };
        }

        private static Pessoa MapBeneficiarioFinal(BoletoBeneficiarioFinal beneficiario)
        {
            if (beneficiario == null || (string.IsNullOrWhiteSpace(beneficiario.Name) && string.IsNullOrWhiteSpace(beneficiario.DocumentNumber)))
                return null;

            return new Favorecido
            {
                Nome = beneficiario.Name,
                TipoPessoa = MapTipoPessoa(beneficiario.DocumentType),
                CPF_CNPJ = beneficiario.DocumentNumber,
            };
        }

        private static TipoInscricaoCPFCNPJEnum MapTipoPessoa(string documentType) =>
            string.Equals(documentType, "CNPJ", StringComparison.OrdinalIgnoreCase) ? TipoInscricaoCPFCNPJEnum.CNPJ : TipoInscricaoCPFCNPJEnum.CPF;

        /// <summary>LIQUIDADO é pago; BAIXADO foi cancelado. Qualquer outro valor não é reportado como efetivado.</summary>
        private static BancoBrSituacaoEnum MapStatusParaSituacao(string status)
        {
            switch (status?.Trim().ToUpperInvariant())
            {
                case "LIQUIDADO":
                    return BancoBrSituacaoEnum.Efetivado;

                case "BAIXADO":
                    return BancoBrSituacaoEnum.Cancelado;

                default:
                    return BancoBrSituacaoEnum.NaoIntegrado;
            }
        }

        private static DateTime? ParseData(string texto) =>
            DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data) ? data.Date : (DateTime?)null;

        private static decimal? ParseValor(string texto) =>
            decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) ? valor : (decimal?)null;

        /// <summary>
        /// O 201 significa boleto registrado: o resultado final do pagamento só sai depois, por
        /// consulta/webhook (não implementados aqui).
        /// </summary>
        private static Movimento AplicarResposta(Movimento movimento, MovimentoItemBoletoCobranca item, BoletoCobrancaResponse dto)
        {
            movimento.NumeroDocumentoNoBanco = !string.IsNullOrWhiteSpace(dto?.BankNumber) ? dto.BankNumber : item.NossoNumero;
            movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Registrado;
            movimento.DetalheRejeicaoBancoBr = null;

            if (dto == null)
                return movimento;

            movimento.HorarioBancoBr = dto.EntryDate;
            item.DataEntrada = dto.EntryDate;
            item.CodigoBarras = dto.Barcode;
            item.LinhaDigitavel = dto.DigitableLine;
            item.QrCodePix = dto.QrCodePix;
            item.UrlQrCodePix = dto.QrCodeUrl;

            return movimento;
        }

        #endregion
    }
}
