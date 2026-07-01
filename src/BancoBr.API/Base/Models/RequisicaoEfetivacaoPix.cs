using System;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Requisição de efetivação de um pagamento Pix já iniciado, independente do banco.
    /// </summary>
    public class RequisicaoEfetivacaoPix
    {
        public string EndToEndId { get; set; }

        public decimal Valor { get; set; }

        public string Descricao { get; set; }

        public DateTime? DataAgendamento { get; set; }
    }
}
