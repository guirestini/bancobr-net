using System;
using System.Text.Json.Serialization;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Corpo de POST /boletos/pagamentos/{codigoBarras}.
    /// </summary>
    public class BoletoPagamentoRequest
    {
        /// <summary>Identificador retornado pela consulta prévia (BoletoConsultaResponse.IdentificadorConsulta).</summary>
        [JsonPropertyName("identificadorConsulta")]
        public string IdentificadorConsulta { get; set; }

        [JsonPropertyName("valorBoleto")]
        public decimal ValorBoleto { get; set; }

        [JsonPropertyName("valorDescontoAbatimento")]
        public decimal ValorDescontoAbatimento { get; set; }

        [JsonPropertyName("valorMultaMora")]
        public decimal ValorMultaMora { get; set; }

        [JsonPropertyName("descricaoObservacao")]
        public string DescricaoObservacao { get; set; }

        [JsonPropertyName("aceitaValorDivergente")]
        public bool AceitaValorDivergente { get; set; }

        [JsonPropertyName("numeroCpfCnpjPortador")]
        public string NumeroCpfCnpjPortador { get; set; }

        [JsonPropertyName("nomePortador")]
        public string NomePortador { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("date")]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime Date { get; set; }

        [JsonPropertyName("debtorAccount")]
        public DebtorAccount DebtorAccount { get; set; }
    }
}
