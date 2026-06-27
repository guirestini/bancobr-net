using System;
using System.Text.Json.Serialization;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Resultado retornado pelo pagamento (200), pela consulta de comprovante por id e pela
    /// consulta de comprovante por idempotency key. O Sicoob envelopa em "resultado",
    /// resolvido internamente pelo PagamentoBoletoClient.
    /// </summary>
    public class ComprovantePagamento
    {
        [JsonPropertyName("numeroAgencia")]
        public string NumeroAgencia { get; set; }

        [JsonPropertyName("nomeAgencia")]
        public string NomeAgencia { get; set; }

        [JsonPropertyName("numeroConta")]
        public long NumeroConta { get; set; }

        [JsonPropertyName("nomeProprietarioContaCorrente")]
        public string NomeProprietarioContaCorrente { get; set; }

        [JsonPropertyName("numeroLinhaDigitavel")]
        public string NumeroLinhaDigitavel { get; set; }

        [JsonPropertyName("numeroInstituicaoEmissora")]
        public int NumeroInstituicaoEmissora { get; set; }

        [JsonPropertyName("nomeInstituicaoEmissora")]
        public string NomeInstituicaoEmissora { get; set; }

        [JsonPropertyName("numeroCpfCnpjBeneficiario")]
        public string NumeroCpfCnpjBeneficiario { get; set; }

        [JsonPropertyName("nomeRazaoSocialBeneficiario")]
        public string NomeRazaoSocialBeneficiario { get; set; }

        [JsonPropertyName("numeroCpfCnpjPagador")]
        public string NumeroCpfCnpjPagador { get; set; }

        [JsonPropertyName("nomeRazaoSocialPagador")]
        public string NomeRazaoSocialPagador { get; set; }

        [JsonPropertyName("dataVencimento")]
        public DateTime DataVencimento { get; set; }

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

        [JsonPropertyName("situacaoPagamento")]
        public string SituacaoPagamento { get; set; }

        [JsonPropertyName("descricaoDetalheSituacao")]
        public string DescricaoDetalheSituacao { get; set; }

        [JsonPropertyName("dataHoraCadastro")]
        public DateTime? DataHoraCadastro { get; set; }

        [JsonPropertyName("aceitaValorDivergente")]
        public bool AceitaValorDivergente { get; set; }

        [JsonPropertyName("nossoNumero")]
        public string NossoNumero { get; set; }

        [JsonPropertyName("numeroDocumento")]
        public string NumeroDocumento { get; set; }

        [JsonPropertyName("descricaoObservacao")]
        public string DescricaoObservacao { get; set; }

        [JsonPropertyName("descricaoOuvidoria")]
        public string DescricaoOuvidoria { get; set; }

        [JsonPropertyName("descricaoTituloComprovante")]
        public string DescricaoTituloComprovante { get; set; }

        [JsonPropertyName("idPagamento")]
        public long IdPagamento { get; set; }

        [JsonPropertyName("numeroAutenticacaoPagamento")]
        public string NumeroAutenticacaoPagamento { get; set; }
    }
}
