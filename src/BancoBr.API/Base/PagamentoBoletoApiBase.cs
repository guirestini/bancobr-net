using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes da API de pagamento de boletos, independente do banco —
    /// espelha o papel de BancoBr.CNAB.Base.Banco no domínio de geração de CNAB: cada banco
    /// herda e implementa os métodos abaixo de acordo com sua própria API.
    /// </summary>
    public abstract class PagamentoBoletoApiBase
    {
        protected PagamentoBoletoApiBase(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; }

        public abstract Task<BoletoConsultaResponse> ConsultarBoletoAsync(string codigoBarras, long numeroConta, DateTime? dataPagamento = null, CancellationToken cancellationToken = default);

        public abstract Task<PagamentoBoletoResultado> PagarBoletoAsync(string codigoBarras, BoletoPagamentoRequest request, string idempotencyKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Conveniência para o caso sem aprovação humana entre consulta e pagamento: consulta o
        /// boleto, e só se ele não estiver bloqueado, monta a requisição de pagamento (usando o
        /// IdentificadorConsulta retornado) e a envia. Quando o ERP precisa exibir o valor para
        /// confirmação antes de pagar, use ConsultarBoletoAsync e PagarBoletoAsync separadamente.
        /// </summary>
        public abstract Task<PagamentoBoletoResultado> PagarBoletoComConsultaAsync(string codigoBarras, long numeroConta, int numeroCooperativa, string idLancamento, string numeroCpfCnpjPortador, string nomePortador, bool aceitaValorDivergente = false, string descricaoObservacao = null, DateTime? dataPagamento = null, int personType = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Paga vários boletos em sequência (o Sicoob não tem endpoint de lote — cada item ainda
        /// gera uma consulta + um pagamento como chamadas HTTP separadas, throttladas pelo rate
        /// limit já configurado no cliente). Um item com erro não interrompe os demais.
        /// </summary>
        public abstract Task<IReadOnlyList<PagamentoBoletoLoteResultadoItem>> PagarLoteBoletosAsync(IEnumerable<PagamentoBoletoLoteItem> itens, CancellationToken cancellationToken = default);

        public abstract Task<ComprovantePagamento> ConsultarComprovantePorIdAsync(long idPagamento, long numeroConta, CancellationToken cancellationToken = default);

        public abstract Task CancelarAgendamentoAsync(long idPagamento, long numeroConta, CancellationToken cancellationToken = default);

        public abstract Task<ComprovantePagamento> ConsultarComprovantePorIdempotencyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

        public abstract Task<IReadOnlyList<BoletoDDA>> ConsultarBoletosDdaAsync(long numeroConta, DateTime dataInicial, DateTime dataFinal, SituacaoBoletoEnum situacao, TipoDataConsultaEnum tipoData, CancellationToken cancellationToken = default);
    }
}
