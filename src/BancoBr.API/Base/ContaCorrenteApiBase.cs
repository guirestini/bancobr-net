using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base.Models;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes da API de conta corrente (saldo/extrato), independente do
    /// banco — mesmo papel que <see cref="PagamentoBoletoApiBase"/> tem para pagamento de
    /// boletos.
    /// </summary>
    public abstract class ContaCorrenteApiBase
    {
        protected ContaCorrenteApiBase(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; }

        /// <summary>
        /// Consulta o extrato da conta no mês/ano informado. <paramref name="diaInicial"/> e
        /// <paramref name="diaFinal"/> recortam o período dentro daquele mês (nulos = mês
        /// inteiro). <paramref name="agruparCnab"/> agrupa o movimento proveniente de CNAB.
        /// </summary>
        public abstract Task<Extrato> ConsultarExtratoAsync(long numeroContaCorrente, int mes, int ano, int? diaInicial, int? diaFinal, bool agruparCnab, CancellationToken cancellationToken = default);
    }
}
