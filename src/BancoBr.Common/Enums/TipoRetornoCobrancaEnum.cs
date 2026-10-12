namespace BancoBr.Common.Enums
{
    /// <summary>
    /// Tipo de retorno de cobrança — o que a consulta de retorno devolve para cada boleto.
    /// Novos tipos (ex.: registro) entram aqui sem mudar o formato do resultado.
    /// </summary>
    public enum TipoRetornoCobrancaEnum
    {
        /// <summary>Boleto liquidado (pago): traz os dados do pagamento.</summary>
        Liquidacao = 0,
    }
}
