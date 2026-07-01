using System;
using System.Collections.Generic;
using BancoBr.API.Base;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.Pagamentos.Boletos;
using BancoBr.API.Sicoob.Pagamentos.Convenios;
using BancoBr.API.Sicoob.Pagamentos.Pix;
using BancoBr.Common.Enums;

namespace BancoBr.API.Core
{
    /// <summary>
    /// Ponto de entrada único para instanciar a API de qualquer operação (pagamento de boleto,
    /// e futuramente outras), independente do banco. Os parâmetros de <see cref="Criar{TApiBase}(BancoEnum, string, string, CertificateSource, Uri)"/>
    /// e suas sobrecargas são só de autenticação — a mesma assinatura serve para qualquer
    /// operação, então o tipo da operação (ex.: <see cref="PagamentoBoletoApiBase"/>) é o
    /// parâmetro genérico, e cada banco/operação só precisa se registrar uma vez abaixo, sem
    /// repetir um switch por operação.
    /// </summary>
    public static class BancoApi
    {
        private static readonly Dictionary<(BancoEnum, Type), Func<string, string, CertificateSource, Uri, object>> ComClientSecret = new Dictionary<(BancoEnum, Type), Func<string, string, CertificateSource, Uri, object>>();
        private static readonly Dictionary<(BancoEnum, Type), Func<string, CertificateSource, IAccessTokenProvider, object>> ComTokenProvider = new Dictionary<(BancoEnum, Type), Func<string, CertificateSource, IAccessTokenProvider, object>>();

        static BancoApi()
        {
            Registrar<PagamentoBoletoApiBase>(BancoEnum.Sicoob,
                (clientId, clientSecret, certificateSource, tokenEndpoint) => new PagamentoBoletoClient(clientId, clientSecret, certificateSource, tokenEndpoint),
                (clientId, certificateSource, tokenProvider) => new PagamentoBoletoClient(clientId, certificateSource, tokenProvider));

            Registrar<PagamentoConvenioApiBase>(BancoEnum.Sicoob,
                (clientId, clientSecret, certificateSource, tokenEndpoint) => new PagamentoConvenioClient(clientId, clientSecret, certificateSource, tokenEndpoint),
                (clientId, certificateSource, tokenProvider) => new PagamentoConvenioClient(clientId, certificateSource, tokenProvider));

            Registrar<PagamentoPixApiBase>(BancoEnum.Sicoob,
                (clientId, clientSecret, certificateSource, tokenEndpoint) => new PagamentoPixClient(clientId, clientSecret, certificateSource, tokenEndpoint),
                (clientId, certificateSource, tokenProvider) => new PagamentoPixClient(clientId, certificateSource, tokenProvider));
        }

        private static void Registrar<TApiBase>(BancoEnum banco, Func<string, string, CertificateSource, Uri, TApiBase> comClientSecret, Func<string, CertificateSource, IAccessTokenProvider, TApiBase> comTokenProvider)
            where TApiBase : class
        {
            ComClientSecret[(banco, typeof(TApiBase))] = (clientId, clientSecret, certificateSource, tokenEndpoint) => comClientSecret(clientId, clientSecret, certificateSource, tokenEndpoint);
            ComTokenProvider[(banco, typeof(TApiBase))] = (clientId, certificateSource, tokenProvider) => comTokenProvider(clientId, certificateSource, tokenProvider);
        }

        /// <summary>
        /// Fluxo padrão: autenticação OAuth2 client_credentials, renovada automaticamente pelo cliente.
        /// </summary>
        /// <param name="tokenEndpointOverride">
        /// Só necessário em ambientes fora do padrão (ex.: sandbox com realm próprio); cada banco já
        /// tem um endpoint OAuth2 padrão embutido.
        /// </param>
        public static TApiBase Criar<TApiBase>(BancoEnum banco, string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpointOverride = null)
            where TApiBase : class
        {
            if (ComClientSecret.TryGetValue((banco, typeof(TApiBase)), out var factory))
                return (TApiBase)factory(clientId, clientSecret, certificateSource, tokenEndpointOverride);
            throw new Exception($"Banco não implementado para API de {typeof(TApiBase).Name}!");
        }

        /// <summary>
        /// Fluxo com token já emitido (ex.: portal de sandbox), pulando o fluxo OAuth2 client_credentials.
        /// </summary>
        public static TApiBase Criar<TApiBase>(BancoEnum banco, string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            where TApiBase : class
        {
            if (ComTokenProvider.TryGetValue((banco, typeof(TApiBase)), out var factory))
                return (TApiBase)factory(clientId, certificateSource, tokenProvider);
            throw new Exception($"Banco não implementado para API de {typeof(TApiBase).Name}!");
        }
    }
}
