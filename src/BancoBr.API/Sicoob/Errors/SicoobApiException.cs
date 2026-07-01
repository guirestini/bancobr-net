using System.Collections.Generic;
using BancoBr.API.Core.Errors;

namespace BancoBr.API.Sicoob.Errors
{
    /// <summary>
    /// Erro de negócio retornado pela API do Sicoob (HTTP 400/406/500 com corpo
    /// { "mensagens": [{ "mensagem", "codigo" }] }). Os códigos numéricos (ex.: "10013")
    /// estão documentados pelo Sicoob; não são mapeados exaustivamente em tipos próprios
    /// nesta versão — o chamador pode inspecionar Mensagens[].Codigo quando precisar.
    /// </summary>
    public class SicoobApiException : BancoApiException
    {
        public IReadOnlyList<SicoobMensagem> Mensagens { get; }

        public SicoobApiException(int httpStatusCode, IReadOnlyList<SicoobMensagem> mensagens)
            : base(BuildMessage(mensagens), httpStatusCode)
        {
            Mensagens = mensagens;
        }

        private static string BuildMessage(IReadOnlyList<SicoobMensagem> mensagens)
        {
            if (mensagens == null || mensagens.Count == 0)
            {
                return "Erro de negócio retornado pela API do Sicoob.";
            }

            var primeira = mensagens[0];
            return $"[{primeira.Codigo}] {primeira.Mensagem}";
        }
    }
}
