using System.Text.Json.Serialization;

namespace BancoBr.API.Sicoob.Errors
{
    public class SicoobMensagem
    {
        [JsonPropertyName("mensagem")]
        public string Mensagem { get; set; }

        [JsonPropertyName("codigo")]
        public string Codigo { get; set; }
    }
}
