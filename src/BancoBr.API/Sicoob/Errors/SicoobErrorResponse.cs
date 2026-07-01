using System.Collections.Generic;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Errors
{
    public class SicoobErrorResponse
    {
        [JsonProperty("mensagens")]
        public List<SicoobMensagem> Mensagens { get; set; } = new List<SicoobMensagem>();
    }
}
