using System;
using System.Text.Json.Serialization;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Resultado de GET /boletos/{codigoBarras}. O Sicoob envelopa a resposta em "resultado";
    /// o envelope é resolvido internamente pelo PagamentoBoletoClient.
    /// </summary>
    public class BoletoConsultaResponse
    {
        [JsonPropertyName("numeroInstituicaoEmissora")]
        public int NumeroInstituicaoEmissora { get; set; }

        [JsonPropertyName("nomeInstituicaoEmissora")]
        public string NomeInstituicaoEmissora { get; set; }

        [JsonPropertyName("tipoPessoaBeneficiario")]
        public string TipoPessoaBeneficiario { get; set; }

        [JsonPropertyName("numeroCpfCnpjBeneficiario")]
        public string NumeroCpfCnpjBeneficiario { get; set; }

        [JsonPropertyName("nomeRazaoSocialBeneficiario")]
        public string NomeRazaoSocialBeneficiario { get; set; }

        [JsonPropertyName("nomeFantasiaBeneficiario")]
        public string NomeFantasiaBeneficiario { get; set; }

        [JsonPropertyName("tipoPessoaPagador")]
        public string TipoPessoaPagador { get; set; }

        [JsonPropertyName("numeroCpfCnpjPagador")]
        public string NumeroCpfCnpjPagador { get; set; }

        [JsonPropertyName("nomeRazaoSocialPagador")]
        public string NomeRazaoSocialPagador { get; set; }

        [JsonPropertyName("codigoBarras")]
        public string CodigoBarras { get; set; }

        [JsonPropertyName("numeroLinhaDigitavel")]
        public string NumeroLinhaDigitavel { get; set; }

        [JsonPropertyName("dataVencimentoBoleto")]
        public DateTime DataVencimentoBoleto { get; set; }

        [JsonPropertyName("dataLimitePagamentoBoleto")]
        public DateTime? DataLimitePagamentoBoleto { get; set; }

        [JsonPropertyName("valorBoleto")]
        public decimal ValorBoleto { get; set; }

        [JsonPropertyName("valorAbatimentoDesconto")]
        public decimal ValorAbatimentoDesconto { get; set; }

        [JsonPropertyName("valorMultaMora")]
        public decimal ValorMultaMora { get; set; }

        [JsonPropertyName("valorPagamento")]
        public decimal ValorPagamento { get; set; }

        [JsonPropertyName("dataPagamento")]
        public DateTime DataPagamento { get; set; }

        [JsonPropertyName("permiteAlterarValor")]
        public bool PermiteAlterarValor { get; set; }

        [JsonPropertyName("consultaEmContingencia")]
        public bool ConsultaEmContingencia { get; set; }

        [JsonPropertyName("codigoEspecieDocumento")]
        public int? CodigoEspecieDocumento { get; set; }

        [JsonPropertyName("codigoSituacaoBoletoPagamento")]
        public string CodigoSituacaoBoletoPagamento { get; set; }

        [JsonPropertyName("nossoNumero")]
        public string NossoNumero { get; set; }

        [JsonPropertyName("numeroDocumento")]
        public string NumeroDocumento { get; set; }

        /// <summary>
        /// Identificador retornado pela consulta. Deve ser enviado de volta no corpo de
        /// POST /boletos/pagamentos/{codigoBarras} (BoletoPagamentoRequest.IdentificadorConsulta).
        /// </summary>
        [JsonPropertyName("identificadorConsulta")]
        public string IdentificadorConsulta { get; set; }

        [JsonPropertyName("descricaoInstrucaoValorMinMax")]
        public string DescricaoInstrucaoValorMinMax { get; set; }

        [JsonPropertyName("bloquearPagamento")]
        public bool BloquearPagamento { get; set; }

        [JsonPropertyName("mensagemBloqueioPagamento")]
        public string MensagemBloqueioPagamento { get; set; }
    }
}
