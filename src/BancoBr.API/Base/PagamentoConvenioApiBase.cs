using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Sicoob.Pagamentos.Convenios.Models;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes da API de pagamento de convênios (arrecadação por código de
    /// barras), independente do banco — mesma ideia de <see cref="PagamentoBoletoApiBase"/>:
    /// cada banco herda e implementa os métodos abaixo de acordo com sua própria API.
    /// </summary>
    public abstract class PagamentoConvenioApiBase
    {
        protected PagamentoConvenioApiBase(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; }

        public abstract Task<ConvenioConsultaResponse> ConsultarCodigoBarrasAsync(string codigoBarras, DateTime dataPagamento, bool? recebimentoViaCaixa = null, CancellationToken cancellationToken = default);

        public abstract Task<PagamentoConvenioResultado> PagarConvenioAsync(string codigoBarras, ArrecadacaoPagamentoRequest request, CancellationToken cancellationToken = default);

        public abstract Task<IReadOnlyList<ArrecadacaoConsultaItem>> ConsultarPagamentosAsync(string codigoBarras, long instituicao, DateTime dataMovimento, long? transacao = null, CancellationToken cancellationToken = default);

        public abstract Task<ComprovanteArrecadacao> ConsultarComprovantePorNsuAsync(long nsu, long instituicao, CancellationToken cancellationToken = default);

        public abstract Task<IReadOnlyList<ConciliacaoItem>> ConsultarConciliacoesAsync(long instituicao, DateTime dataMovimento, int? unidade = null, CancellationToken cancellationToken = default);

        public abstract Task<IReadOnlyList<ConvenioHabilitado>> ConsultarConveniosHabilitadosAsync(long transacao, long instituicao, CancellationToken cancellationToken = default);
    }
}
