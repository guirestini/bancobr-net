using System;

namespace BancoBr.API.Core.Models
{
    /// <summary>
    /// Constrói o valor exigido pelo header x-idempotency-key: {idLancamento}-{acao}.
    /// Determinística por design — duas chamadas para o mesmo lançamento e mesma ação
    /// produzem a mesma key, permitindo que o banco deduplique pagamentos repetidos
    /// (ex.: duplo clique do usuário no ERP).
    /// </summary>
    public static class IdempotencyKey
    {
        public const string AcaoInclusao = "INCLUSAO";

        public static string New(string idLancamento, string acao = AcaoInclusao)
        {
            if (string.IsNullOrWhiteSpace(idLancamento))
                throw new ArgumentException("idLancamento é obrigatório.", nameof(idLancamento));

            return $"{idLancamento}-{acao}";
        }
    }
}
