namespace BancoBr.API.Base.Models
{
    /// <summary>Segunda via do comprovante de um pagamento de convênio, independente do banco.</summary>
    public class ComprovanteArrecadacao
    {
        public string Comprovante { get; set; }

        public Arrecadacao Pagamento { get; set; }
    }
}
