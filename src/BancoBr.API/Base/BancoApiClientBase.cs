using System.Net.Http;
using BancoBr.Common.Interfaces.API;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes de API bancária. Não herda de Common.Instances.Banco
    /// (usado pelo CNAB) pois aquela classe é modelada para geração de arquivo
    /// (Correntista, versão de arquivo), conceitos que não existem em uma integração HTTP.
    /// </summary>
    public abstract class BancoApiClientBase : IBancoApiClient
    {
        protected BancoApiClientBase(int codigo, string nome, HttpClient httpClient)
        {
            Codigo = codigo;
            Nome = nome;
            HttpClient = httpClient;
        }

        public int Codigo { get; set; }

        public string Nome { get; set; }

        protected HttpClient HttpClient { get; }
    }
}
