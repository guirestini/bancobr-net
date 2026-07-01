namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Identificação da instituição e unidade correspondente ao pagamento de convênio.
    /// </summary>
    public class Identificacao
    {
        public long Instituicao { get; set; }

        public int Unidade { get; set; }
    }
}
