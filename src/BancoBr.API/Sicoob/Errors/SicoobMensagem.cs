using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Errors
{
    public class SicoobMensagem
    {
        [JsonProperty("mensagem")]
        public string Mensagem { get; set; }

        [JsonProperty("codigo")]
        public string Codigo { get; set; }
    }
}
