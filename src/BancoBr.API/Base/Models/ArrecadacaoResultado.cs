namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Resultado de um pagamento de convênio efetivado, independente do banco.
    /// </summary>
    public class ArrecadacaoResultado
    {
        /// <summary>Comprovante de pagamento em Base64.</summary>
        public string Comprovante { get; set; }

        public Arrecadacao Arrecadacao { get; set; }
    }
}
