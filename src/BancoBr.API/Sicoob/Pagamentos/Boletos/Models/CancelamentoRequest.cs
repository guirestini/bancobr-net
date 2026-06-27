using System.Text.Json.Serialization;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    public class CancelamentoRequest
    {
        [JsonPropertyName("numeroConta")]
        public long NumeroConta { get; set; }
    }
}
