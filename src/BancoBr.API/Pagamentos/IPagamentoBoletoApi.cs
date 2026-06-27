using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;

namespace BancoBr.API.Pagamentos
{
    public interface IPagamentoBoletoApi
    {
        Task<BoletoConsultaResponse> ConsultarBoletoAsync(string codigoBarras, long numeroConta, DateTime? dataPagamento = null, CancellationToken cancellationToken = default);

        Task<PagamentoBoletoResultado> PagarBoletoAsync(string codigoBarras, BoletoPagamentoRequest request, string idempotencyKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Conveniência para o caso sem aprovação humana entre consulta e pagamento: consulta o
        /// boleto, e só se ele não estiver bloqueado, monta a requisição de pagamento (usando o
        /// IdentificadorConsulta retornado) e a envia. Quando o ERP precisa exibir o valor para
        /// confirmação antes de pagar, use ConsultarBoletoAsync e PagarBoletoAsync separadamente.
        /// </summary>
        Task<PagamentoBoletoResultado> PagarBoletoComConsultaAsync(string codigoBarras, long numeroConta, int numeroCooperativa, string numeroCpfCnpjPortador, string nomePortador, bool aceitaValorDivergente = false, string descricaoObservacao = null, DateTime? dataPagamento = null, int personType = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Paga vários boletos em sequência (o Sicoob não tem endpoint de lote — cada item ainda
        /// gera uma consulta + um pagamento como chamadas HTTP separadas, throttladas pelo rate
        /// limit já configurado no cliente). Um item com erro não interrompe os demais.
        /// </summary>
        Task<IReadOnlyList<PagamentoBoletoLoteResultadoItem>> PagarLoteBoletosAsync(IEnumerable<PagamentoBoletoLoteItem> itens, CancellationToken cancellationToken = default);

        Task<ComprovantePagamento> ConsultarComprovantePorIdAsync(long idPagamento, long numeroConta, CancellationToken cancellationToken = default);

        Task CancelarAgendamentoAsync(long idPagamento, long numeroConta, CancellationToken cancellationToken = default);

        Task<ComprovantePagamento> ConsultarComprovantePorIdempotencyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BoletoDDA>> ConsultarBoletosDdaAsync(long numeroConta, DateTime dataInicial, DateTime dataFinal, SituacaoBoletoEnum situacao, TipoDataConsultaEnum tipoData, CancellationToken cancellationToken = default);
    }
}
