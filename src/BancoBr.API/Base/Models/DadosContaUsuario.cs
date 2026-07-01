namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Dados de conta (origem ou destino) de um pagamento Pix, independente do banco.
    /// </summary>
    public class DadosContaUsuario
    {
        public string Ispb { get; set; }

        public string CpfCnpj { get; set; }

        public string Nome { get; set; }

        public string Conta { get; set; }

        public string Agencia { get; set; }

        public string Tipo { get; set; }

        public string ChaveDict { get; set; }

        public bool? BoolFavorecido { get; set; }
    }
}
