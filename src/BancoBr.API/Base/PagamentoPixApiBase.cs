using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base.Models;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes da API de pagamentos Pix (iniciação por chave DICT +
    /// confirmação), independente do banco — mesma ideia de
    /// <see cref="PagamentoConvenioApiBase"/>: cada banco herda e implementa os métodos
    /// abaixo de acordo com sua própria API.
    /// </summary>
    public abstract class PagamentoPixApiBase
    {
        protected PagamentoPixApiBase(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; }

        public abstract Task<RetornoPagamento> ConsultarPagamentoAsync(string endToEndId, CancellationToken cancellationToken = default);

        public abstract Task<PixIniciacaoResponse> IniciarPagamentoAsync(string chave, DateTime? dataAgendamento, CancellationToken cancellationToken = default);

        public abstract Task<RetornoPagamento> ConfirmarPagamentoAsync(RequisicaoEfetivacaoPix request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Paga um Pix Copia e Cola (QR Code estático ou com vencimento) com execução
        /// direta — ao contrário de <see cref="IniciarPagamentoAsync"/> +
        /// <see cref="ConfirmarPagamentoAsync"/>, não há passo de confirmação separado.
        /// </summary>
        public abstract Task<PagamentoQrCodeResponse> PagarViaQrCodeAsync(RequisicaoPagamentoQrCode request, CancellationToken cancellationToken = default);
    }
}
