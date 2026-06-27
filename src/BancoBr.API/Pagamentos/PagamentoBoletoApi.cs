using System;
using BancoBr.API.Base;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob;
using BancoBr.API.Sicoob.Pagamentos.Boletos;
using BancoBr.Common.Enums;

namespace BancoBr.API.Pagamentos
{
    /// <summary>
    /// Ponto de entrada único para a API de pagamento de boletos, independente do banco —
    /// espelha o padrão já usado em BancoBr.CNAB.ArquivoCNAB, que resolve a implementação
    /// correta a partir de um BancoEnum em vez de o consumidor (ERP) instanciar diretamente
    /// a classe específica do banco (ex.: PagamentoBoletoClient do Sicoob).
    /// </summary>
    public static class PagamentoBoletoApi
    {
        public static PagamentoBoletoApiBase Criar(BancoEnum banco, BancoApiOptions options)
        {
            switch (banco)
            {
                case BancoEnum.Sicoob:
                    return new PagamentoBoletoClient((SicoobApiOptions)options);
                default:
                    throw new Exception("Banco não implementado para API de pagamento de boletos!");
            }
        }

        public static PagamentoBoletoApiBase Criar(BancoEnum banco, BancoApiOptions options, IAccessTokenProvider tokenProvider)
        {
            switch (banco)
            {
                case BancoEnum.Sicoob:
                    return new PagamentoBoletoClient((SicoobApiOptions)options, tokenProvider);
                default:
                    throw new Exception("Banco não implementado para API de pagamento de boletos!");
            }
        }
    }
}
