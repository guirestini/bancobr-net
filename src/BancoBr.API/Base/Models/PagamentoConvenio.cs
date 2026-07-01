using System;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Dados de pagamento de um convênio, independente do banco.
    /// </summary>
    public class PagamentoConvenio
    {
        public decimal ValorPago { get; set; }

        /// <summary>Número Sequencial Único. Obrigatório apenas para o perfil "Parceiro Banco Sicoob".</summary>
        public long? Nsu { get; set; }

        public DateTime DataPagamento { get; set; }

        public decimal ValorDocumento { get; set; }

        public decimal ValorDesconto { get; set; }

        public decimal ValorJuros { get; set; }

        public decimal ValorMulta { get; set; }

        /// <summary>Uso exclusivo do perfil "Parceiro Banco Sicoob".</summary>
        public bool? RecebimentoViaCaixa { get; set; }
    }
}
