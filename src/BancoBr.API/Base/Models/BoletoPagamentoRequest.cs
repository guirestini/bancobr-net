using System;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Requisição de pagamento de um boleto, independente do banco.
    /// </summary>
    public class BoletoPagamentoRequest
    {
        /// <summary>Identificador retornado pela consulta prévia (BoletoConsultaResponse.IdentificadorConsulta).</summary>
        public string IdentificadorConsulta { get; set; }

        public decimal ValorBoleto { get; set; }

        public decimal ValorDescontoAbatimento { get; set; }

        public decimal ValorMultaMora { get; set; }

        public string DescricaoObservacao { get; set; }

        public bool AceitaValorDivergente { get; set; }

        public string NumeroCpfCnpjPortador { get; set; }

        public string NomePortador { get; set; }

        public decimal Amount { get; set; }

        public DateTime Date { get; set; }

        public DebtorAccount DebtorAccount { get; set; }
    }
}
