using System;
using BancoBr.API.Base;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.Pagamentos.Boletos;
using BancoBr.Common.Enums;

namespace BancoBr.API.Pagamentos
{
    /// <summary>
    /// Ponto de entrada único para a API de pagamento de boletos, independente do banco —
    /// espelha o padrão já usado em BancoBr.CNAB.ArquivoCNAB, que resolve a implementação
    /// correta a partir de um BancoEnum em vez de o consumidor (ERP) instanciar diretamente
    /// a classe específica do banco. O ERP nunca referencia SicoobApiOptions (ou qualquer
    /// classe de Options por banco) — só os dados primitivos comuns abaixo.
    /// </summary>
    public static class PagamentoBoletoApi
    {
        /// <summary>
        /// Fluxo padrão: autenticação OAuth2 client_credentials, renovada automaticamente pelo cliente.
        /// </summary>
        /// <param name="tokenEndpointOverride">
        /// Só necessário em ambientes fora do padrão (ex.: sandbox com realm próprio); cada banco já
        /// tem um endpoint OAuth2 padrão embutido.
        /// </param>
        public static PagamentoBoletoApiBase Criar(BancoEnum banco, string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpointOverride = null)
        {
            switch (banco)
            {
                case BancoEnum.Sicoob:
                    return new PagamentoBoletoClient(clientId, clientSecret, certificateSource, tokenEndpointOverride);
                default:
                    throw new Exception("Banco não implementado para API de pagamento de boletos!");
            }
        }

        /// <summary>
        /// Fluxo com token já emitido (ex.: portal de sandbox), pulando o fluxo OAuth2 client_credentials.
        /// </summary>
        public static PagamentoBoletoApiBase Criar(BancoEnum banco, string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
        {
            switch (banco)
            {
                case BancoEnum.Sicoob:
                    return new PagamentoBoletoClient(clientId, certificateSource, tokenProvider);
                default:
                    throw new Exception("Banco não implementado para API de pagamento de boletos!");
            }
        }
    }
}
