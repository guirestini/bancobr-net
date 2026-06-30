using System;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Resultado da consulta de um boleto, independente do banco. Espelha a estrutura do
    /// Sicoob (único banco implementado hoje) — cada cliente de banco é responsável por mapear
    /// sua resposta específica para esta estrutura.
    /// </summary>
    public class BoletoConsultaResponse
    {
        public int NumeroInstituicaoEmissora { get; set; }

        public string NomeInstituicaoEmissora { get; set; }

        public string TipoPessoaBeneficiario { get; set; }

        public string NumeroCpfCnpjBeneficiario { get; set; }

        public string NomeRazaoSocialBeneficiario { get; set; }

        public string NomeFantasiaBeneficiario { get; set; }

        public string TipoPessoaPagador { get; set; }

        public string NumeroCpfCnpjPagador { get; set; }

        public string NomeRazaoSocialPagador { get; set; }

        public string CodigoBarras { get; set; }

        public string NumeroLinhaDigitavel { get; set; }

        public DateTime DataVencimentoBoleto { get; set; }

        public DateTime? DataLimitePagamentoBoleto { get; set; }

        public decimal ValorBoleto { get; set; }

        public decimal ValorAbatimentoDesconto { get; set; }

        public decimal ValorMultaMora { get; set; }

        public decimal ValorPagamento { get; set; }

        public DateTime DataPagamento { get; set; }

        public bool PermiteAlterarValor { get; set; }

        public bool ConsultaEmContingencia { get; set; }

        public int? CodigoEspecieDocumento { get; set; }

        public string CodigoSituacaoBoletoPagamento { get; set; }

        public string NossoNumero { get; set; }

        public string NumeroDocumento { get; set; }

        /// <summary>
        /// Identificador retornado pela consulta. Deve ser enviado de volta no pagamento
        /// (BoletoPagamentoRequest.IdentificadorConsulta).
        /// </summary>
        public string IdentificadorConsulta { get; set; }

        public string DescricaoInstrucaoValorMinMax { get; set; }

        public bool BloquearPagamento { get; set; }

        public string MensagemBloqueioPagamento { get; set; }
    }
}
