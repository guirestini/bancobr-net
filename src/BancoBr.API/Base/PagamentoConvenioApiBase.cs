using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base.Models;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes da API de pagamento de convênios (arrecadação por código de
    /// barras), independente do banco — mesma ideia de <see cref="PagamentoBoletoApiBase"/>:
    /// cada banco herda e implementa os métodos abaixo de acordo com sua própria API.
    ///
    /// O contrato público é o mesmo <see cref="Movimento"/>/<see cref="MovimentoItem"/> usado
    /// pelo BancoBr.CNAB (a operação de convênio espera um
    /// <see cref="MovimentoItemPagamentoConvenioCodigoBarra"/> como
    /// <see cref="Movimento.MovimentoItem"/>).
    /// </summary>
    public abstract class PagamentoConvenioApiBase
    {
        protected PagamentoConvenioApiBase(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; }

        /// <summary>
        /// Consulta os dados de arrecadação do código de barras informado em
        /// <see cref="MovimentoItemPagamentoConvenioCodigoBarra.CodigoBarra"/>, preenchendo
        /// valores, convênio e — importante para o pagamento seguinte — a
        /// <see cref="MovimentoItemPagamentoConvenioCodigoBarra.Transacao"/>.
        /// </summary>
        public abstract Task<Movimento> ConsultarCodigoBarrasAsync(Movimento movimento, Correntista origem, bool? recebimentoViaCaixa = null, int unidade = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Efetiva o pagamento do convênio. Reenvia a
        /// <see cref="MovimentoItemPagamentoConvenioCodigoBarra.Transacao"/> obtida na consulta,
        /// que é o que permite ao banco deduplicar reenvios.
        /// </summary>
        public abstract Task<Movimento> PagarConvenioAsync(Movimento movimento, Correntista origem, int unidade = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Recupera a segunda via do comprovante do pagamento cujo NSU está em
        /// <see cref="MovimentoItemPagamentoConvenioCodigoBarra.Nsu"/> (ou em
        /// <see cref="Movimento.NumeroDocumentoNoBanco"/>).
        /// </summary>
        public abstract Task<Movimento> ConsultarComprovantePorNsuAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consulta de lista (pagamentos já realizados para um código de barras numa data) — não
        /// corresponde a um único <see cref="Movimento"/>, por isso mantém o contrato próprio.
        /// </summary>
        public abstract Task<IReadOnlyList<ArrecadacaoConsultaItem>> ConsultarPagamentosAsync(string codigoBarras, long instituicao, DateTime dataMovimento, long? transacao = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dado de referência em nível de conta (síntese das arrecadações de uma data).
        /// </summary>
        public abstract Task<IReadOnlyList<ConciliacaoItem>> ConsultarConciliacoesAsync(long instituicao, DateTime dataMovimento, int? unidade = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dado de referência em nível de conta (convênios habilitados para arrecadação).
        /// </summary>
        public abstract Task<IReadOnlyList<ConvenioHabilitado>> ConsultarConveniosHabilitadosAsync(long transacao, long instituicao, CancellationToken cancellationToken = default);
    }
}
