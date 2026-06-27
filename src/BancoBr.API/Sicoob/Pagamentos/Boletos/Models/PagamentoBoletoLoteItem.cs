using System;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Um boleto a pagar dentro de um lote. Espelha os parâmetros de
    /// <see cref="PagamentoBoletoClient.PagarBoletoComConsultaAsync"/> — o Sicoob não tem um
    /// endpoint de lote, então cada item ainda gera consulta + pagamento como chamadas HTTP
    /// separadas; isto só evita que o ERP reimplemente o loop e o tratamento de erro parcial.
    /// </summary>
    public class PagamentoBoletoLoteItem
    {
        public string CodigoBarras { get; set; }

        public long NumeroConta { get; set; }

        public int NumeroCooperativa { get; set; }

        public string IdLancamento { get; set; }

        public string NumeroCpfCnpjPortador { get; set; }

        public string NomePortador { get; set; }

        public bool AceitaValorDivergente { get; set; }

        public string DescricaoObservacao { get; set; }

        public DateTime? DataPagamento { get; set; }

        /// <summary>0 - Pessoa Física, 1 - Pessoa Jurídica.</summary>
        public int PersonType { get; set; }
    }
}
