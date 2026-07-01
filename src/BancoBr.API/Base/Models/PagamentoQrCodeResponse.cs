using System;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Retorno do pagamento de um Pix Copia e Cola (QR Code), independente do banco.
    /// </summary>
    public class PagamentoQrCodeResponse
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

        public string TxId { get; set; }

        public decimal? ValorOriginal { get; set; }

        public decimal? Abatimento { get; set; }

        public decimal? Desconto { get; set; }

        public decimal? Multa { get; set; }

        public decimal? Juros { get; set; }

        public string TipoQrcode { get; set; }

        public BancoBrSituacaoEnum BancoBrSituacao { get; set; }
    }
}
