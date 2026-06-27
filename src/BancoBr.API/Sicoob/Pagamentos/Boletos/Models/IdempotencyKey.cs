using System;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Constrói o valor exigido pelo header x-idempotency-key:
    /// {numeroCooperativa}-{numeroContaCorrente}-{uuid}.
    /// </summary>
    public static class IdempotencyKey
    {
        public static string New(int numeroCooperativa, long numeroContaCorrente)
        {
            return $"{numeroCooperativa}-{numeroContaCorrente}-{Guid.NewGuid()}";
        }
    }
}
