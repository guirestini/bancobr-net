using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes da API de pagamentos Pix (iniciação por chave DICT +
    /// confirmação), independente do banco — mesma ideia de
    /// <see cref="PagamentoConvenioApiBase"/>: cada banco herda e implementa os métodos
    /// abaixo de acordo com sua própria API.
    ///
    /// O contrato público é o mesmo <see cref="Movimento"/>/<see cref="MovimentoItem"/> usado
    /// pelo BancoBr.CNAB: cada método recebe o movimento, mapeia para o formato da API do
    /// banco, e aplica a resposta de volta no mesmo objeto — que é então devolvido (mesma
    /// instância, mutada in-place).
    /// </summary>
    public abstract class PagamentoPixApiBase
    {
        protected PagamentoPixApiBase(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; }

        /// <summary>
        /// Consulta a situação de um pagamento já iniciado. O identificador do pagamento no
        /// banco (EndToEndId) é lido de <see cref="Movimento.NumeroDocumentoNoBanco"/>.
        /// </summary>
        public abstract Task<Movimento> ConsultarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve a chave DICT informada em
        /// <see cref="MovimentoItemTransferenciaPIX.ChavePIX"/> para os dados do titular, sem
        /// movimentar valores. Espera um <see cref="MovimentoItemTransferenciaPIX"/> como
        /// <see cref="Movimento.MovimentoItem"/>.
        /// </summary>
        public abstract Task<Movimento> IniciarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default);

        /// <summary>
        /// Efetiva um pagamento previamente iniciado por
        /// <see cref="IniciarPagamentoAsync"/>.
        /// </summary>
        public abstract Task<Movimento> ConfirmarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default);

        /// <summary>
        /// Paga um Pix Copia e Cola (QR Code estático ou com vencimento) com execução
        /// direta — ao contrário de <see cref="IniciarPagamentoAsync"/> +
        /// <see cref="ConfirmarPagamentoAsync"/>, não há passo de confirmação separado.
        /// Espera um <see cref="MovimentoItemPagamentoTituloPIXQRCode"/> como
        /// <see cref="Movimento.MovimentoItem"/>.
        /// </summary>
        public abstract Task<Movimento> PagarViaQrCodeAsync(Correntista origem, Movimento movimento, CancellationToken cancellationToken = default);
    }
}
