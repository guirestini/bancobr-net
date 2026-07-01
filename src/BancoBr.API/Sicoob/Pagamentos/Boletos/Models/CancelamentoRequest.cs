using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    public class CancelamentoRequest
    {
        [JsonProperty("numeroConta")]
        public long NumeroConta { get; set; }
    }
}
