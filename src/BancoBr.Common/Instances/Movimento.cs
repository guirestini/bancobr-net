using BancoBr.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using BancoBr.Common.Enums;

namespace BancoBr.Common.Instances
{
    public class Movimento
    {
        public Movimento()
        {
            TipoMovimento = TipoMovimentoEnum.Inclusao;
            CodigoInstrucao = CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheLiberado;

            Moeda = "BRL";
            QuantidadeMoeda = 0;
        }

        public Favorecido Favorecido { get; set; }
        public TipoMovimentoEnum TipoMovimento { get; set; }
        public TipoLancamentoEnum TipoLancamento { get; set; }
        public CodigoInstrucaoMovimentoEnum CodigoInstrucao { get; set; }

        public string Moeda { get; set; }
        public decimal QuantidadeMoeda { get; set; }
        public DateTime DataPagamento { get; set; }
        public string NumeroDocumento { get; set; }
        public decimal ValorPagamento { get; set; }

        /// <summary>
        /// Identificador do movimento no banco. No CNAB, o número atribuído pelo banco ao
        /// pagamento; nas APIs bancárias, o EndToEndId (Pix Sicoob), o id do pagamento (Pix
        /// Santander), o IdPagamento (Boleto) ou o NSU (Convênio).
        /// </summary>
        public string NumeroDocumentoNoBanco { get; set; }

        public MovimentoItem MovimentoItem { get; set; }
        public AvisoFavorecidoEnum AvisoAoFavorecido { get; set; }

        public List<Ocorrencia> Ocorrencias { get; set; }
        public string Registro { get; set; }

        #region ::. Resultado das APIs bancárias .::

        /*************************************************************************************
         *
         * Preenchidos apenas quando o movimento trafega por BancoBr.API (pagamento via API
         * bancária). São compartilhados pelas três famílias de operação (Pix, Boleto e
         * Convênio) porque nunca são preenchidos simultaneamente por mais de uma delas.
         * O CNAB não usa nem preenche estes campos.
         *
         *************************************************************************************/

        /// <summary>
        /// Situação do pagamento no banco, normalizada. Nulo enquanto o movimento não passou
        /// por nenhuma chamada de API bancária.
        /// </summary>
        public BancoBrSituacaoEnum? SituacaoBancoBr { get; set; }

        /// <summary>
        /// Data/hora informada pelo banco para o evento do pagamento (liquidação, cadastro do
        /// comprovante, etc.).
        /// </summary>
        public DateTime? HorarioBancoBr { get; set; }

        /// <summary>
        /// Motivo textual informado pelo banco quando o pagamento não pôde ser efetivado
        /// (rejeição, bloqueio, boleto não encontrado, pendência de assinatura, ...).
        /// </summary>
        public string DetalheRejeicaoBancoBr { get; set; }

        #endregion
    }

    public class MovimentoItem
    {
    }

    public class MovimentoItemTransferenciaPIX : MovimentoItem
    {
        public FormaIniciacaoEnum TipoChavePIX { get; set; }
        public string ChavePIX { get; set; }

        #region ::. Resultado das APIs bancárias .::

        /// <summary>
        /// Nome do titular da chave, conforme resolvido pelo banco (DICT). Mantido separado de
        /// <see cref="Movimento.Favorecido"/> de propósito: o Favorecido é a suposição a priori
        /// do ERP, e a UI precisa comparar os dois antes de confirmar o pagamento.
        /// </summary>
        public string TitularConfirmadoNome { get; set; }

        /// <summary>CPF/CNPJ do titular da chave, conforme resolvido pelo banco (DICT).</summary>
        public string TitularConfirmadoDocumento { get; set; }

        /// <summary>Tipo de pessoa do titular da chave, conforme resolvido pelo banco (DICT).</summary>
        public string TitularConfirmadoTipo { get; set; }

        /// <summary>
        /// Tipo de chave que o banco confirmou na iniciação do pagamento (ecoado pela API).
        /// Textual porque cada banco usa seu próprio vocabulário — diferente de
        /// <see cref="TipoChavePIX"/>, que é a classificação Febraban usada no CNAB.
        /// </summary>
        public string TipoChaveConfirmado { get; set; }

        #endregion

        #region ::. Transferência por agência e conta (Santander) .::

        /*************************************************************************************
         *
         * Usados apenas pelo Santander, quando ChavePIX está vazia: a transferência é feita
         * para a conta do beneficiário. Nome, CPF/CNPJ e tipo de pessoa vêm de
         * Movimento.Favorecido. O banco do beneficiário é identificado por Banco (código
         * COMPE) OU Ispb — nunca os dois.
         *
         *************************************************************************************/

        /// <summary>Código COMPE do banco do beneficiário (ex.: 341). Alternativa a <see cref="Ispb"/>.</summary>
        public int Banco { get; set; }

        /// <summary>ISPB (8 dígitos) do banco do beneficiário. Alternativa a <see cref="Banco"/>.</summary>
        public string Ispb { get; set; }

        public int NumeroAgencia { get; set; }

        public long NumeroConta { get; set; }

        public string DVConta { get; set; }

        public TipoContaEnum TipoConta { get; set; }

        #endregion
    }

    public class MovimentoItemTransferenciaTED : MovimentoItem
    {
        public int Banco { get; set; }
        public int NumeroAgencia { get; set; }
        public string DVAgencia { get; set; }
        public long NumeroConta { get; set; }
        public string DVConta { get; set; }
        public FinalidadeTEDEnum CodigoFinalidadeTED { get; set; }
        public TipoContaEnum TipoConta { get; set; }

        #region ::. Resultado das APIs bancárias .::

        /// <summary>Campo descritivo de histórico, enviado no pedido e ecoado pela resposta.</summary>
        public string Historico { get; set; }

        /// <summary>Número do PA, exigido pela API do Sicoob ("0" quando não aplicável).</summary>
        public string NumeroPa { get; set; }

        /// <summary>
        /// Número de controle atribuído pela IF ao envio — é o "codigo" usado para consultar a
        /// TED depois (GET /transferencias/{codigo}). Preenchido em <see cref="Movimento.NumeroDocumentoNoBanco"/>.
        /// </summary>
        public string NumeroControleIF { get; set; }

        /// <summary>
        /// Id do agendamento atribuído pelo banco — distinto de <see cref="NumeroControleIF"/>,
        /// é o identificador usado para cancelar (DELETE /transferencias/agendamentos/{idAgendamento}).
        /// </summary>
        public long IdAgendamento { get; set; }

        /// <summary>Indica se a TED foi agendada (campo "agendamento" da resposta).</summary>
        public bool Agendado { get; set; }

        /// <summary>Situação textual da TED, conforme devolvida pelo banco (vocabulário não normalizado).</summary>
        public string Situacao { get; set; }

        public int CodigoSituacaoAgendamento { get; set; }

        public string MensagemErro { get; set; }

        /// <summary>Tipo da pessoa debitada ("F" ou "J"), conforme devolvido pelo banco.</summary>
        public string TipoPessoaDebito { get; set; }

        public string NomePessoaDebito { get; set; }

        public string NumeroCPFCNPJDebito { get; set; }

        public int NumeroBancoFavorecido { get; set; }

        #endregion
    }

    public class MovimentoItemPagamentoTituloCodigoBarra : MovimentoItem
    {
        public MovimentoItemPagamentoTituloCodigoBarra()
        {
            MoedaCodigoBarra = 9;
        }

        /// <summary>
        /// Código do banco do código de barras
        /// </summary>
        public int BancoCodigoBarra { get; set; }

        /// <summary>
        /// Moeda no código de barras
        /// </summary>
        public int MoedaCodigoBarra { get; set; }

        /// <summary>
        /// DV do código de barras
        /// </summary>
        public int DVCodigoBarra { get; set; }

        /// <summary>
        /// Fator de Vencimento do código de barras
        /// </summary>
        public int FatorVencimentoCodigoBarra { get; set; }

        /// <summary>
        /// Valor do código de barras
        /// </summary>
        public decimal ValorCodigoBarra { get; set; }

        /// <summary>
        /// Campo Livre do código de barras - 25 posições
        /// </summary>
        public string CampoLivreCodigoBarra { get; set; }

        public decimal Desconto { get; set; }
        public decimal Acrescimo { get; set; }

        #region ::. APIs bancárias .::

        /// <summary>
        /// Código de barras bruto (44 dígitos) usado pelas APIs bancárias. Distinto de
        /// <see cref="CampoLivreCodigoBarra"/> e demais campos acima, que são a decomposição
        /// exigida pelo CNAB.
        /// </summary>
        public string CodigoBarras { get; set; }

        /// <summary>
        /// Token opaco devolvido pela consulta do boleto e exigido de volta no pagamento.
        /// </summary>
        public string IdentificadorConsulta { get; set; }

        public bool AceitaValorDivergente { get; set; }

        public string DescricaoObservacao { get; set; }

        public DateTime? DataVencimento { get; set; }

        public DateTime? DataLimitePagamento { get; set; }

        public bool PermiteAlterarValor { get; set; }

        public bool ConsultaEmContingencia { get; set; }

        public int? CodigoEspecieDocumento { get; set; }

        public string DescricaoInstrucaoValorMinMax { get; set; }

        public string CodigoSituacaoBoletoPagamento { get; set; }

        public string NomeFantasiaBeneficiario { get; set; }

        public string NomeInstituicaoEmissora { get; set; }

        /// <summary>Nome do pagador conforme registrado no boleto/comprovante pelo banco.</summary>
        public string PagadorConfirmadoNome { get; set; }

        /// <summary>CPF/CNPJ do pagador conforme registrado no boleto/comprovante pelo banco.</summary>
        public string PagadorConfirmadoDocumento { get; set; }

        /// <summary>Tipo de pessoa do pagador conforme informado pelo banco (textual, varia por banco).</summary>
        public string PagadorConfirmadoTipoPessoa { get; set; }

        public string NumeroLinhaDigitavel { get; set; }

        public string NossoNumero { get; set; }

        /// <summary>
        /// Número do documento do próprio título, informado pelo beneficiário — distinto de
        /// <see cref="Movimento.NumeroDocumento"/>, que é o número do documento da empresa
        /// pagadora (NumeroDocumentoEmpresa no CNAB).
        /// </summary>
        public string NumeroDocumentoTitulo { get; set; }

        public string NomeAgencia { get; set; }

        public string NomeProprietarioContaCorrente { get; set; }

        public string DescricaoOuvidoria { get; set; }

        public string DescricaoTituloComprovante { get; set; }

        public string NumeroAutenticacaoPagamento { get; set; }

        #endregion
    }

    public class MovimentoItemPagamentoConvenioCodigoBarra : MovimentoItem
    {
        public MovimentoItemPagamentoConvenioCodigoBarra()
        {
        }

        /// <summary>
        /// Código de barras de 44 ou 48 caracteres
        /// </summary>
        public string CodigoBarra { get; set; }

        public DateTime DataVencimento { get; set; }

        #region ::. APIs bancárias .::

        public decimal ValorDocumento { get; set; }

        public decimal ValorDesconto { get; set; }

        public decimal ValorJuros { get; set; }

        public decimal ValorMulta { get; set; }

        public decimal ValorOutrosEncargos { get; set; }

        public string Convenio { get; set; }

        public string SiglaConvenio { get; set; }

        public string CodigoConvenioFebraban { get; set; }

        /// <summary>Número Sequencial Único da arrecadação.</summary>
        public long? Nsu { get; set; }

        /// <summary>
        /// Identificador da transação de arrecadação. Devolvido pela consulta do código de
        /// barras e reenviado no pagamento — é o que permite ao banco deduplicar reenvios e ao
        /// cliente recuperar um pagamento já efetivado.
        /// </summary>
        public long? Transacao { get; set; }

        /// <summary>Uso exclusivo do perfil "Parceiro Banco" em alguns bancos.</summary>
        public bool? RecebimentoViaCaixa { get; set; }

        /// <summary>Comprovante de pagamento devolvido pelo banco, em Base64.</summary>
        public string ComprovanteBase64 { get; set; }

        public string Autenticacao { get; set; }

        public string IdentificadorFgts { get; set; }

        public int? AnoExercicio { get; set; }

        #endregion
    }

    public class MovimentoItemPagamentoTituloPIXQRCode : MovimentoItem
    {
        public DateTime DataVencimento { get; set; }
        public string URL_ChavePix { get; set; }
        public string TXID { get; set; }

        #region ::. APIs bancárias .::

        /// <summary>Payload Pix Copia e Cola (BR Code) a ser pago.</summary>
        public string QrCode { get; set; }

        /// <summary>Indica se o QR Code aceita pagamentos repetidos (QR estático reutilizável).</summary>
        public bool Repeticao { get; set; }

        /// <summary>Autoriza o banco a somar juros/multa ao valor pago quando o QR tem vencimento.</summary>
        public bool PagarComJurosMulta { get; set; }

        public decimal? ValorOriginal { get; set; }

        public decimal? Abatimento { get; set; }

        public decimal? Desconto { get; set; }

        public decimal? Multa { get; set; }

        public decimal? Juros { get; set; }

        /// <summary>Tipo do QR Code identificado pelo banco (estático, com vencimento, ...).</summary>
        public string TipoQrcode { get; set; }

        #endregion
    }

    /// <summary>
    /// Emissão (registro) de um boleto de cobrança, só via API bancária (Santander).
    /// No <see cref="Movimento"/>: <see cref="Movimento.Favorecido"/> é o <b>pagador</b> (quem
    /// será cobrado), <see cref="Movimento.ValorPagamento"/> é o valor nominal e
    /// <see cref="Movimento.NumeroDocumento"/> é o "seu número" (até 15 caracteres
    /// alfanuméricos). O beneficiário (quem recebe) é o <see cref="Correntista"/> passado a
    /// <c>BancoApi.EmitirBoletoAsync</c>, cujo <see cref="Correntista.Convenio"/> é o código do
    /// convênio de cobrança. Após a emissão, <see cref="Movimento.NumeroDocumentoNoBanco"/>
    /// traz o nosso número e <see cref="Movimento.SituacaoBancoBr"/> fica
    /// <see cref="BancoBrSituacaoEnum.Registrado"/>.
    /// </summary>
    public class MovimentoItemBoletoCobranca : MovimentoItem
    {
        #region ::. Dados do boleto .::

        /// <summary>Nosso número (1 a 13 dígitos).</summary>
        public string NossoNumero { get; set; }

        public DateTime DataVencimento { get; set; }

        /// <summary>Data de emissão; quando não informada, usa a data de hoje.</summary>
        public DateTime? DataEmissao { get; set; }

        /// <summary>Controle do participante (até 25 caracteres alfanuméricos).</summary>
        public string CodigoParticipante { get; set; }

        public EspecieDocumentoBoletoEnum EspecieDocumento { get; set; }

        /// <summary>Beneficiário final, quando diferente do beneficiário do convênio.</summary>
        public Pessoa BeneficiarioFinal { get; set; }

        /// <summary>Mensagens impressas no boleto.</summary>
        public List<string> Mensagens { get; set; }

        #endregion

        #region ::. Desconto, multa, juros e abatimento .::

        public TipoDescontoBoletoEnum TipoDesconto { get; set; }

        /// <summary>Até três descontos, por ordem de data limite; ignorados com <see cref="TipoDescontoBoletoEnum.Isento"/>.</summary>
        public List<DescontoBoleto> Descontos { get; set; }

        public decimal? PercentualMulta { get; set; }

        public int? DiasMulta { get; set; }

        public decimal? PercentualJuros { get; set; }

        public decimal? ValorAbatimento { get; set; }

        public decimal? PercentualIOF { get; set; }

        #endregion

        #region ::. Protesto e baixa .::

        public TipoProtestoBoletoEnum TipoProtesto { get; set; }

        public int? DiasProtesto { get; set; }

        public int? DiasBaixa { get; set; }

        #endregion

        #region ::. Pagamento divergente/parcial .::

        public TipoPagamentoBoletoEnum TipoPagamento { get; set; }

        public int? QuantidadeParciais { get; set; }

        public TipoValorPagamentoParcialEnum? TipoValorParcial { get; set; }

        public decimal? ValorMinimo { get; set; }

        public decimal? ValorMaximo { get; set; }

        #endregion

        #region ::. Partilha e Pix (Bolepix) .::

        public List<PartilhaBoleto> Partilha { get; set; }

        /// <summary>Tipo da chave DICT que recebe o Pix do boleto (Bolepix); junto com <see cref="ChavePIX"/>.</summary>
        public FormaIniciacaoEnum TipoChavePIX { get; set; }

        public string ChavePIX { get; set; }

        /// <summary>Identificador da transação Bolepix (26 a 35 caracteres).</summary>
        public string TxId { get; set; }

        #endregion

        #region ::. Controle da chamada .::

        /// <summary>Emite no ambiente de testes do banco (<c>TESTE</c>) em vez de produção.</summary>
        public bool AmbienteTeste { get; set; }

        /// <summary>
        /// NSU da chamada (até 20 caracteres alfanuméricos, exclusivo por dia). Quando vazio, é
        /// gerado na emissão e gravado aqui junto com <see cref="DataNsu"/> — assim um retry da
        /// mesma emissão reaproveita o mesmo NSU, sem duplicar o boleto.
        /// </summary>
        public string NsuCode { get; set; }

        public DateTime? DataNsu { get; set; }

        #endregion

        #region ::. Resultado da emissão .::

        public string CodigoBarras { get; set; }

        public string LinhaDigitavel { get; set; }

        /// <summary>Data de entrada (registro) informada pelo banco.</summary>
        public DateTime? DataEntrada { get; set; }

        /// <summary>Payload Pix Copia e Cola do boleto.</summary>
        public string QrCodePix { get; set; }

        public string UrlQrCodePix { get; set; }

        #endregion
    }

    /// <summary>
    /// Retorno de cobrança (consulta de boletos já emitidos), só via API bancária (Santander). Todo
    /// retorno vem neste mesmo formato; <see cref="TipoRetorno"/> diz o que ele representa e quais
    /// campos estão preenchidos (hoje só <see cref="TipoRetornoCobrancaEnum.Liquidacao"/>).
    /// No <see cref="Movimento"/>: <see cref="Movimento.Favorecido"/> é o pagador,
    /// <see cref="Movimento.NumeroDocumentoNoBanco"/> é o nosso número,
    /// <see cref="Movimento.NumeroDocumento"/> é o seu número, <see cref="Movimento.ValorPagamento"/>
    /// é o valor pago e <see cref="Movimento.DataPagamento"/> a data do pagamento (valor nominal em
    /// <see cref="ValorNominal"/>). <see cref="Movimento.SituacaoBancoBr"/> é
    /// <see cref="BancoBrSituacaoEnum.Efetivado"/> para boleto liquidado.
    /// </summary>
    public class MovimentoItemRetornoCobranca : MovimentoItem
    {
        public TipoRetornoCobrancaEnum TipoRetorno { get; set; }

        #region ::. Identificação do boleto .::

        public string Convenio { get; set; }

        public string NossoNumero { get; set; }

        /// <summary>NSU da emissão e sua data — junto com o nosso número, identificam o boleto no banco.</summary>
        public string NsuCode { get; set; }

        public DateTime? DataNsu { get; set; }

        /// <summary>Status do boleto como o banco informa (ex.: LIQUIDADO) e seu complemento (ex.: Baixa Operacional).</summary>
        public string StatusBanco { get; set; }

        public string ComplementoStatusBanco { get; set; }

        public decimal? ValorNominal { get; set; }

        public DateTime? DataVencimento { get; set; }

        public DateTime? DataEmissao { get; set; }

        /// <summary>Data de entrada (registro) informada pelo banco.</summary>
        public DateTime? DataEntrada { get; set; }

        public string CodigoBarras { get; set; }

        public string LinhaDigitavel { get; set; }

        /// <summary>Beneficiário final, quando informado na emissão.</summary>
        public Pessoa BeneficiarioFinal { get; set; }

        #endregion

        #region ::. Liquidação .::

        public decimal? ValorJuros { get; set; }

        public decimal? ValorMulta { get; set; }

        public decimal? ValorAbatimento { get; set; }

        /// <summary>Valor de descontos concedidos no pagamento.</summary>
        public decimal? ValorDesconto { get; set; }

        public decimal? ValorIOF { get; set; }

        /// <summary>Tipo do pagamento (ex.: PIX).</summary>
        public string TipoPagamento { get; set; }

        /// <summary>Banco onde foi pago (código, como o banco informa).</summary>
        public string BancoPagamento { get; set; }

        /// <summary>Canal do pagamento (ex.: INTERNET BANKING).</summary>
        public string CanalPagamento { get; set; }

        /// <summary>Espécie do pagamento (ex.: DEBITO EM CONTA).</summary>
        public string EspeciePagamento { get; set; }

        /// <summary>Identificador da transação Pix (Bolepix), quando pago por Pix.</summary>
        public string TxId { get; set; }

        #endregion
    }

    public class DescontoBoleto
    {
        public decimal Valor { get; set; }

        public DateTime DataLimite { get; set; }
    }

    public class PartilhaBoleto
    {
        /// <summary>Código de partilha (2 dígitos).</summary>
        public string Codigo { get; set; }

        public decimal Valor { get; set; }
    }

    public class MovimentoItemDDA : MovimentoItem
    {
        public string DescricaoTipoPagador { get; set; }

        public string TipoPessoaBeneficiario { get; set; }

        public string NumeroCpfCnpjBeneficiario { get; set; }

        public string NomeRazaoSocialBeneficiario { get; set; }

        public string TipoPessoaPagador { get; set; }

        public string NumeroCpfCnpjPagador { get; set; }

        public string NomeRazaoSocialPagador { get; set; }

        public string NomeFantasiaPagador { get; set; }

        public string DescricaoLogradouroPagador { get; set; }

        public string DescricaoCidadePagador { get; set; }

        public string SiglaUfPagador { get; set; }

        public string NumeroCepPagador { get; set; }

        public string TipoPessoaAvalista { get; set; }

        public string NumeroCpfCnpjAvalista { get; set; }

        public string NomeAvalista { get; set; }

        public decimal ValorBoleto { get; set; }

        public DateTime DataVencimentoBoleto { get; set; }

        public int CodigoTipoSituacaoBoleto { get; set; }

        public string DescricaoSituacaoBoleto { get; set; }

        public long NumeroIdentificadorBoletoCip { get; set; }

        public string NumeroCodigoBarras { get; set; }

        public string NumeroCpfCnpjPagadorEletronico { get; set; }

        public bool Aceite { get; set; }

        public string NumeroNossoNumero { get; set; }

        public string NumeroDocumento { get; set; }

        public DateTime? DataPagamento { get; set; }

        public decimal? ValorPagamento { get; set; }

        public int CodigoEspecieDocumento { get; set; }

        public DateTime DataEmissao { get; set; }

        public string DataLimitePagamento { get; set; }

        public int CodigoTipoJuros { get; set; }

        public DateTime? DataJuros { get; set; }

        public decimal ValorPercentualJuros { get; set; }

        public int CodigoTipoMulta { get; set; }

        public DateTime? DataMulta { get; set; }

        public decimal ValorPercentualMulta { get; set; }

        public decimal ValorAbatimento { get; set; }

        public string CodigoTipoDesconto1 { get; set; }

        public DateTime? DataDesconto1 { get; set; }

        public decimal ValorPercentualDesconto1 { get; set; }

        public string CodigoTipoDesconto2 { get; set; }

        public string DataDesconto2 { get; set; }

        public decimal ValorPercentualDesconto2 { get; set; }

        public string CodigoTipoDesconto3 { get; set; }

        public string DataDesconto3 { get; set; }

        public decimal ValorPercentualDesconto3 { get; set; }

        public int NumeroDiasProtesto { get; set; }

        public int QuantidadePagamentoParcial { get; set; }

        public int CodigoAutorizacaoValorDivergente { get; set; }

        public string CodigoIndicadorValorMaximo { get; set; }

        public decimal ValorPercentualMaximo { get; set; }

        public string CodigoIndicadorValorMinimo { get; set; }

        public decimal ValorPercentualMinimo { get; set; }
    }

    public class MovimentoItemExtratoSaldo : MovimentoItem
    {
        public decimal SaldoAtual { get; set; }

        public decimal SaldoBloqueado { get; set; }

        public decimal SaldoLimite { get; set; }

        public decimal SaldoAnterior { get; set; }

        public decimal SaldoBloqueioJudicial { get; set; }

        public decimal SaldoBloqueioJudicialAnterior { get; set; }
    }

    public class MovimentoItemExtratoTransacao : MovimentoItem
    {
        public string TransactionId { get; set; }

        /// <summary>
        /// Já normalizado a partir do vocabulário textual devolvido pelo banco (ex.:
        /// "CREDITO"/"DEBITO" ou "C"/"D") — cada cliente faz esse mapeamento, já que o
        /// vocabulário exato varia por banco.
        /// </summary>
        public BancoBrTipoCreditoDebitoEnum Tipo { get; set; }

        /// <summary>Sempre positivo — o sinal (crédito/débito) vem em <see cref="Tipo"/>.</summary>
        public decimal Valor { get; set; }

        public DateTime Data { get; set; }

        public DateTime? DataLote { get; set; }

        public string Descricao { get; set; }

        public string NumeroDocumento { get; set; }

        public string CpfCnpj { get; set; }

        public string DescricaoInformacaoComplementar { get; set; }
    }
}
