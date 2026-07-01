using System;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Dados da arrecadação de um convênio já pago, independente do banco.
    /// </summary>
    public class Arrecadacao
    {
        public decimal ValorPago { get; set; }

        public long? Nsu { get; set; }

        public DateTime DataPagamento { get; set; }

        public decimal ValorDocumento { get; set; }

        public decimal ValorDesconto { get; set; }

        public decimal ValorJuros { get; set; }

        public decimal ValorMulta { get; set; }

        public string Autenticacao { get; set; }

        public bool? RecebimentoViaCaixa { get; set; }
    }
}
