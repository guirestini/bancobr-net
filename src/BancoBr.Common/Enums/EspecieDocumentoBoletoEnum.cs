namespace BancoBr.Common.Enums
{
    /// <summary>Espécie do documento de um boleto de cobrança.</summary>
    public enum EspecieDocumentoBoletoEnum
    {
        DuplicataMercantil = 0,
        DuplicataServico,
        NotaPromissoria,
        NotaPromissoriaRural,
        Recibo,
        ApoliceSeguro,
        BoletoCartaoCredito,
        BoletoProposta,
        BoletoDepositoAporte,
        Cheque,
        NotaPromissoriaDireta,
        Outros,
    }
}
