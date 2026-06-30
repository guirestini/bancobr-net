namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Conta utilizada para débito do pagamento, independente do banco.
    /// </summary>
    public class DebtorAccount
    {
        /// <summary>Número da agência/cooperativa da conta.</summary>
        public int Issuer { get; set; }

        /// <summary>Número da conta habilitada para pagamentos via API.</summary>
        public long Number { get; set; }

        /// <summary>0 - Conta Corrente.</summary>
        public int AccountType { get; set; }

        /// <summary>0 - Pessoa Física, 1 - Pessoa Jurídica.</summary>
        public int PersonType { get; set; }
    }
}
