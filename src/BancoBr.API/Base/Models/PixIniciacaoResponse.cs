namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Retorno da iniciação de pagamento Pix por chave DICT, independente do banco.
    /// </summary>
    public class PixIniciacaoResponse
    {
        public string EndToEndId { get; set; }

        public string Chave { get; set; }

        public string Tipo { get; set; }

        public PixProprietario Proprietario { get; set; }
    }
}
