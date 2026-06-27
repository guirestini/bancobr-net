using System;
using BancoBr.API.Base;

namespace BancoBr.API.Sicoob
{
    /// <summary>
    /// Configuração de autenticação do cooperado junto ao Sicoob — independe de qual API/produto
    /// (Boletos, PIX, TED...) está sendo chamada. Detalhes específicos de cada API (base URL,
    /// scopes exigidos, rate limit) ficam embutidos no cliente daquela API, não aqui — o
    /// consumidor (ERP) não deveria precisar conhecer URLs ou nomes de scope do Sicoob.
    /// </summary>
    public class SicoobApiOptions : BancoApiOptions
    {
        /// <summary>
        /// Endpoint OAuth2 (client_credentials) do Sicoob, igual para qualquer API/produto e
        /// qualquer cooperado. Só precisa ser sobrescrito em cenários fora do padrão
        /// (ex.: um ambiente de testes com realm próprio).
        /// </summary>
        public Uri TokenEndpoint { get; set; } = new Uri("https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token");
    }
}
