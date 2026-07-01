namespace BancoBr.API.Base.Models
{
    /// <summary>Dados do titular da chave Pix resolvida pela iniciação, independente do banco.</summary>
    public class PixProprietario
    {
        public string Identificador { get; set; }

        public string Nome { get; set; }

        public string Tipo { get; set; }
    }
}
