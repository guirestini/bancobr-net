using System;
using Newtonsoft.Json;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Retorno da consulta ou confirmação de um pagamento Pix, independente do banco.
    /// </summary>
    public class RetornoPagamento
    {
        public string EndToEndId { get; set; }

        public string Estado { get; set; }

        public decimal Valor { get; set; }

        public string DetalheRejeicao { get; set; }

        public string Descricao { get; set; }

        public DateTime? Horario { get; set; }

        public DadosContaUsuario Origem { get; set; }

        public DadosContaUsuario Destino { get; set; }

        public DateTime? DataAgendamento { get; set; }

        [JsonProperty("BancoBrSituacao")]
        public BancoBrSituacaoEnum BancoBrSituacao { get; set; }
    }
}
