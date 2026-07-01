namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Requisição de pagamento de um convênio (arrecadação por código de barras), independente
    /// do banco.
    /// </summary>
    public class ArrecadacaoPagamentoRequest
    {
        public Identificacao Identificacao { get; set; }

        public PagamentoConvenio Pagamento { get; set; }

        public long Transacao { get; set; }
    }
}
