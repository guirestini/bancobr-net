using BancoBr.API.Core.Http;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Configuração comum de autenticação para integração via API com qualquer banco.
    /// Detalhes específicos de um banco (endpoints, scopes, particularidades de protocolo)
    /// ficam na classe de opções daquele banco, que herda desta.
    /// </summary>
    public abstract class BancoApiOptions
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public CertificateSource CertificateSource { get; set; }
    }
}
