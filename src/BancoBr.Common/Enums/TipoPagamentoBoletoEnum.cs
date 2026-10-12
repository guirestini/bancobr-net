namespace BancoBr.Common.Enums
{
    /// <summary>Regra de pagamento aceita para um boleto de cobrança.</summary>
    public enum TipoPagamentoBoletoEnum
    {
        /// <summary>Só aceita o valor registrado.</summary>
        Registro = 0,

        /// <summary>Aceita valor divergente, dentro dos limites mínimo/máximo.</summary>
        Divergente,

        /// <summary>Aceita pagamentos parciais.</summary>
        Parcial,
    }
}
