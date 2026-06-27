using System;
using System.Text.Json.Serialization;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Item retornado por GET /boletos (consulta de boletos DDA de uma conta corrente).
    /// </summary>
    public class BoletoDDA
    {
        [JsonPropertyName("descricaoTipoPagador")]
        public string DescricaoTipoPagador { get; set; }

        [JsonPropertyName("tipoPessoaBeneficiario")]
        public string TipoPessoaBeneficiario { get; set; }

        [JsonPropertyName("numeroCpfCnpjBeneficiario")]
        public string NumeroCpfCnpjBeneficiario { get; set; }

        [JsonPropertyName("nomeRazaoSocialBeneficiario")]
        public string NomeRazaoSocialBeneficiario { get; set; }

        [JsonPropertyName("tipoPessoaPagador")]
        public string TipoPessoaPagador { get; set; }

        [JsonPropertyName("numeroCpfCnpjPagador")]
        public string NumeroCpfCnpjPagador { get; set; }

        [JsonPropertyName("nomeRazaoSocialPagador")]
        public string NomeRazaoSocialPagador { get; set; }

        [JsonPropertyName("nomeFantasiaPagador")]
        public string NomeFantasiaPagador { get; set; }

        [JsonPropertyName("descricaoLogradouroPagador")]
        public string DescricaoLogradouroPagador { get; set; }

        [JsonPropertyName("descricaoCidadePagador")]
        public string DescricaoCidadePagador { get; set; }

        [JsonPropertyName("siglaUfPagador")]
        public string SiglaUfPagador { get; set; }

        [JsonPropertyName("numeroCepPagador")]
        public string NumeroCepPagador { get; set; }

        [JsonPropertyName("tipoPessoaAvalista")]
        public string TipoPessoaAvalista { get; set; }

        [JsonPropertyName("numeroCpfCnpjAvalista")]
        public string NumeroCpfCnpjAvalista { get; set; }

        [JsonPropertyName("nomeAvalista")]
        public string NomeAvalista { get; set; }

        [JsonPropertyName("valorBoleto")]
        public decimal ValorBoleto { get; set; }

        [JsonPropertyName("dataVencimentoBoleto")]
        public DateTime DataVencimentoBoleto { get; set; }

        [JsonPropertyName("codigoTipoSituacaoBoleto")]
        public int CodigoTipoSituacaoBoleto { get; set; }

        [JsonPropertyName("descricaoSituacaoBoleto")]
        public string DescricaoSituacaoBoleto { get; set; }

        [JsonPropertyName("numeroIdentificadorBoletoCip")]
        public long NumeroIdentificadorBoletoCip { get; set; }

        [JsonPropertyName("numeroCodigoBarras")]
        public string NumeroCodigoBarras { get; set; }

        [JsonPropertyName("numeroCpfCnpjPagadorEletronico")]
        public string NumeroCpfCnpjPagadorEletronico { get; set; }

        [JsonPropertyName("aceite")]
        public bool Aceite { get; set; }

        [JsonPropertyName("numeroNossoNumero")]
        public string NumeroNossoNumero { get; set; }

        [JsonPropertyName("numeroDocumento")]
        public string NumeroDocumento { get; set; }

        [JsonPropertyName("dataPagamento")]
        public DateTime? DataPagamento { get; set; }

        [JsonPropertyName("valorPagamento")]
        public decimal? ValorPagamento { get; set; }

        [JsonPropertyName("codigoEspecieDocumento")]
        public int CodigoEspecieDocumento { get; set; }

        [JsonPropertyName("dataEmissao")]
        public DateTime DataEmissao { get; set; }

        [JsonPropertyName("dataLimitePagamento")]
        public string DataLimitePagamento { get; set; }

        [JsonPropertyName("codigoTipoJuros")]
        public int CodigoTipoJuros { get; set; }

        [JsonPropertyName("dataJuros")]
        public DateTime? DataJuros { get; set; }

        [JsonPropertyName("valorPercentualJuros")]
        public decimal ValorPercentualJuros { get; set; }

        [JsonPropertyName("codigoTipoMulta")]
        public int CodigoTipoMulta { get; set; }

        [JsonPropertyName("dataMulta")]
        public DateTime? DataMulta { get; set; }

        [JsonPropertyName("valorPercentualMulta")]
        public decimal ValorPercentualMulta { get; set; }

        [JsonPropertyName("valorAbatimento")]
        public decimal ValorAbatimento { get; set; }

        [JsonPropertyName("codigoTipoDesconto1")]
        public string CodigoTipoDesconto1 { get; set; }

        [JsonPropertyName("dataDesconto1")]
        public DateTime? DataDesconto1 { get; set; }

        [JsonPropertyName("valorPercentualDesconto1")]
        public decimal ValorPercentualDesconto1 { get; set; }

        [JsonPropertyName("codigoTipoDesconto2")]
        public string CodigoTipoDesconto2 { get; set; }

        [JsonPropertyName("dataDesconto2")]
        public string DataDesconto2 { get; set; }

        [JsonPropertyName("valorPercentualDesconto2")]
        public decimal ValorPercentualDesconto2 { get; set; }

        [JsonPropertyName("codigoTipoDesconto3")]
        public string CodigoTipoDesconto3 { get; set; }

        [JsonPropertyName("dataDesconto3")]
        public string DataDesconto3 { get; set; }

        [JsonPropertyName("valorPercentualDesconto3")]
        public decimal ValorPercentualDesconto3 { get; set; }

        [JsonPropertyName("numeroDiasProtesto")]
        public int NumeroDiasProtesto { get; set; }

        [JsonPropertyName("quantidadePagamentoParcial")]
        public int QuantidadePagamentoParcial { get; set; }

        [JsonPropertyName("codigoAutorizacaoValorDivergente")]
        public int CodigoAutorizacaoValorDivergente { get; set; }

        [JsonPropertyName("codigoIndicadorValorMaximo")]
        public string CodigoIndicadorValorMaximo { get; set; }

        [JsonPropertyName("valorPercentualMaximo")]
        public decimal ValorPercentualMaximo { get; set; }

        [JsonPropertyName("codigoIndicadorValorMinimo")]
        public string CodigoIndicadorValorMinimo { get; set; }

        [JsonPropertyName("valorPercentualMinimo")]
        public decimal ValorPercentualMinimo { get; set; }
    }
}
