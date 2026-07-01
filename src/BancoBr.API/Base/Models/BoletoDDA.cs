using System;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Item de uma consulta de boletos DDA de uma conta corrente, independente do banco.
    /// </summary>
    public class BoletoDDA
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
}
