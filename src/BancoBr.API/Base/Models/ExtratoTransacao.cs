using System;
using BancoBr.Common.Enums;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Uma transação (lançamento) dentro do extrato de conta corrente, independente do banco.
    /// </summary>
    public class ExtratoTransacao
    {
        public string TransactionId { get; set; }

        /// <summary>
        /// Já normalizado a partir do vocabulário textual devolvido pelo banco (ex.:
        /// "CREDITO"/"DEBITO" ou "C"/"D") — cada cliente faz esse mapeamento, já que o
        /// vocabulário exato varia por banco.
        /// </summary>
        public BancoBrTipoCreditoDebitoEnum Tipo { get; set; }

        /// <summary>Sempre positivo — o sinal (crédito/débito) vem em <see cref="Tipo"/>.</summary>
        public decimal Valor { get; set; }

        public DateTime Data { get; set; }

        public DateTime? DataLote { get; set; }

        public string Descricao { get; set; }

        public string NumeroDocumento { get; set; }

        public string CpfCnpj { get; set; }

        public string DescricaoInformacaoComplementar { get; set; }
    }
}
