using System.Collections.Generic;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Extrato de conta corrente de um mês/ano, independente do banco.
    /// </summary>
    public class Extrato
    {
        public decimal SaldoAtual { get; set; }

        public decimal SaldoBloqueado { get; set; }

        public decimal SaldoLimite { get; set; }

        public decimal SaldoAnterior { get; set; }

        public decimal SaldoBloqueioJudicial { get; set; }

        public decimal SaldoBloqueioJudicialAnterior { get; set; }

        public IReadOnlyList<ExtratoTransacao> Transacoes { get; set; }
    }
}
