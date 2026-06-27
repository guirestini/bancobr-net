using System.Text.Json.Serialization;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Conta do associado utilizada para débito do pagamento.
    /// </summary>
    public class DebtorAccount
    {
        /// <summary>Número da cooperativa da conta.</summary>
        [JsonPropertyName("issuer")]
        public int Issuer { get; set; }

        /// <summary>Número da conta habilitada para pagamentos via API.</summary>
        [JsonPropertyName("number")]
        public long Number { get; set; }

        /// <summary>0 - Conta Corrente.</summary>
        [JsonPropertyName("accountType")]
        public int AccountType { get; set; }

        /// <summary>0 - Pessoa Física, 1 - Pessoa Jurídica.</summary>
        [JsonPropertyName("personType")]
        public int PersonType { get; set; }
    }
}
