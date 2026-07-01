using System;
using Newtonsoft.Json;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Item de consulta de pagamento(s) de convênio já realizado(s), independente do banco.
    /// </summary>
    public class ArrecadacaoConsultaItem
    {
        public decimal ValorPago { get; set; }

        public long? Nsu { get; set; }

        public DateTime DataPagamento { get; set; }

        public decimal ValorDocumento { get; set; }

        public decimal ValorDesconto { get; set; }

        public decimal ValorJuros { get; set; }

        public decimal ValorMulta { get; set; }

        public string IdentificadorFgts { get; set; }

        public int? AnoExercicio { get; set; }

        public bool? RecebimentoViaCaixa { get; set; }

        public string Autenticacao { get; set; }

        public SituacaoArrecadacao Situacao { get; set; }

        public string Convenio { get; set; }

        public string SiglaConvenio { get; set; }

        public long? Transacao { get; set; }

        [JsonProperty("BancoBrSituacao")]
        public BancoBrSituacaoEnum BancoBrSituacao { get; set; }
    }
}
