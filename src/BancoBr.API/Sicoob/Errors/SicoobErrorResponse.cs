using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BancoBr.API.Sicoob.Errors
{
    public class SicoobErrorResponse
    {
        [JsonPropertyName("mensagens")]
        public List<SicoobMensagem> Mensagens { get; set; } = new List<SicoobMensagem>();
    }
}
