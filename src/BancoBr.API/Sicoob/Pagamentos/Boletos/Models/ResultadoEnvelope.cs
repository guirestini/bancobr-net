using System.Text.Json.Serialization;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// O Sicoob envelopa as respostas de consulta/pagamento em { "resultado": ... }.
    /// Usado apenas internamente pelo PagamentoBoletoClient para desserialização.
    /// </summary>
    internal class ResultadoEnvelope<T>
    {
        [JsonPropertyName("resultado")]
        public T Resultado { get; set; }
    }
}
