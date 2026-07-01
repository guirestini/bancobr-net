using System;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Requisição de pagamento de um Pix Copia e Cola (QR Code estático ou com
    /// vencimento), independente do banco. Ao contrário do fluxo por chave DICT
    /// (<see cref="RequisicaoIniciacaoPix"/>), a execução é direta — não há confirmação
    /// separada.
    /// </summary>
    public class RequisicaoPagamentoQrCode
    {
        /// <summary>
        /// Payload "Copia e Cola" (EMV) lido do QR Code, cru.
        /// </summary>
        public string QrCode { get; set; }

        public bool Repeticao { get; set; }

        public DateTime? DataVencimento { get; set; }

        public DateTime? DataAgendamento { get; set; }

        public decimal Valor { get; set; }

        public bool PagarComJurosMulta { get; set; }

        public DadosContaUsuario Origem { get; set; }

        public DadosContaUsuario Destino { get; set; }
    }
}
