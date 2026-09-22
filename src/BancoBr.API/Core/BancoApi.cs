using System;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.ContaCorrente;
using BancoBr.API.Sicoob.Pagamentos.Boletos;
using BancoBr.API.Sicoob.Pagamentos.Convenios;
using BancoBr.API.Sicoob.Pagamentos.Pix;
using BancoBr.API.Sicoob.Pagamentos.Ted;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.API.Core
{
    /// <summary>
    /// Ponto de entrada único da BancoBr.API: <see cref="Conectar(BancoEnum, string, string, CertificateSource, Uri)"/>
    /// devolve, numa chamada por banco/credenciais, um cliente já pronto para todas as operações
    /// (Pix, Boleto, Convênio, TED, Conta Corrente) — sem o chamador precisar escolher, por tipo,
    /// qual cliente instanciar.
    ///
    /// Para o caminho comum (mandar um <see cref="Movimento"/>, consultar, cancelar), os três
    /// métodos de instância abaixo despacham para o cliente tipado certo com base no tipo
    /// concreto de <see cref="Movimento.MovimentoItem"/> — o chamador não precisa saber que Pix,
    /// Boleto, Convênio e TED são clientes diferentes por baixo.
    ///
    /// Os clientes tipados continuam expostos (<see cref="Boleto"/>, <see cref="Convenio"/>,
    /// <see cref="Pix"/>, <see cref="Ted"/>, <see cref="ContaCorrente"/>) para o que não cabe
    /// nesse formato: consultas de lista (DDA, conciliações, convênios habilitados, extrato) não
    /// partem de um Movimento, e fluxos com aprovação humana entre consulta e pagamento precisam
    /// dos passos separados.
    /// </summary>
    public sealed class BancoApi
    {
        /// <summary>
        /// Fluxo padrão: autenticação OAuth2 client_credentials, renovada automaticamente por
        /// cada cliente interno (cada API do banco tem seus próprios scopes).
        /// </summary>
        /// <param name="tokenEndpointOverride">
        /// Só necessário em ambientes fora do padrão (ex.: sandbox com realm próprio); cada banco
        /// já tem um endpoint OAuth2 padrão embutido.
        /// </param>
        public static BancoApi Conectar(BancoEnum banco, string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpointOverride = null)
        {
            switch (banco)
            {
                case BancoEnum.Sicoob:
                    return new BancoApi(
                        new PagamentoBoletoClient(clientId, clientSecret, certificateSource, tokenEndpointOverride),
                        new PagamentoConvenioClient(clientId, clientSecret, certificateSource, tokenEndpointOverride),
                        new PagamentoPixClient(clientId, clientSecret, certificateSource, tokenEndpointOverride),
                        new TedClient(clientId, clientSecret, certificateSource, tokenEndpointOverride),
                        new ExtratoClient(clientId, clientSecret, certificateSource, tokenEndpointOverride));

                default:
                    throw new Exception($"Banco não implementado: {banco}!");
            }
        }

        /// <summary>
        /// Fluxo com token já emitido (ex.: portal de sandbox), pulando o fluxo OAuth2
        /// client_credentials — o mesmo token é reaproveitado pelos cinco clientes internos.
        /// </summary>
        public static BancoApi Conectar(BancoEnum banco, string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
        {
            switch (banco)
            {
                case BancoEnum.Sicoob:
                    return new BancoApi(
                        new PagamentoBoletoClient(clientId, certificateSource, tokenProvider),
                        new PagamentoConvenioClient(clientId, certificateSource, tokenProvider),
                        new PagamentoPixClient(clientId, certificateSource, tokenProvider),
                        new TedClient(clientId, certificateSource, tokenProvider),
                        new ExtratoClient(clientId, certificateSource, tokenProvider));

                default:
                    throw new Exception($"Banco não implementado: {banco}!");
            }
        }

        private BancoApi(PagamentoBoletoApiBase boleto, PagamentoConvenioApiBase convenio, PagamentoPixApiBase pix, PagamentoTedApiBase ted, ContaCorrenteApiBase contaCorrente)
        {
            Boleto = boleto;
            Convenio = convenio;
            Pix = pix;
            Ted = ted;
            ContaCorrente = contaCorrente;
        }

        public PagamentoBoletoApiBase Boleto { get; }
        public PagamentoConvenioApiBase Convenio { get; }
        public PagamentoPixApiBase Pix { get; }
        public PagamentoTedApiBase Ted { get; }
        public ContaCorrenteApiBase ContaCorrente { get; }

        /// <summary>
        /// Envia o movimento — consulta prévia (quando o banco exige) + pagamento, na mesma
        /// chamada. <paramref name="idempotencyKey"/> é opcional: para Boleto (que exige um GUID
        /// de lançamento por baixo) uma chave que não seja um GUID válido gera um novo; para TED
        /// é usada como veio, e uma nova é gerada quando omitida. <paramref name="unidade"/> só
        /// se aplica a Convênio (perfil "Parceiro Banco" multi-unidade). Pix por chave: se o
        /// movimento já foi iniciado por uma chamada prévia a <see cref="ConsultarAsync"/> (ex.:
        /// pra exibir o titular resolvido antes de confirmar), só confirma — não inicia de novo,
        /// o que geraria um EndToEndId novo e poderia trazer um titular diferente do aprovado.
        /// </summary>
        public async Task<Movimento> EnviarAsync(Movimento movimento, Correntista origem = null, string idempotencyKey = null, int unidade = 0, CancellationToken cancellationToken = default)
        {
            switch (ExtrairItem(movimento))
            {
                case MovimentoItemPagamentoTituloCodigoBarra _:
                    return await Boleto.PagarBoletoComConsultaAsync(movimento, origem, ParseOuGerarGuid(idempotencyKey), cancellationToken).ConfigureAwait(false);

                case MovimentoItemPagamentoConvenioCodigoBarra _:
                    await Convenio.ConsultarCodigoBarrasAsync(movimento, origem, unidade: unidade, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return await Convenio.PagarConvenioAsync(movimento, origem, unidade, cancellationToken).ConfigureAwait(false);

                case MovimentoItemTransferenciaTED _:
                    return await Ted.PagarTedAsync(movimento, origem, idempotencyKey ?? Guid.NewGuid().ToString(), cancellationToken).ConfigureAwait(false);

                case MovimentoItemTransferenciaPIX _:
                    return string.IsNullOrEmpty(movimento.NumeroDocumentoNoBanco)
                        ? await Pix.PagarComIniciacaoAsync(movimento, cancellationToken).ConfigureAwait(false)
                        : await Pix.ConfirmarPagamentoAsync(movimento, cancellationToken).ConfigureAwait(false);

                case MovimentoItemPagamentoTituloPIXQRCode _:
                    return await Pix.PagarViaQrCodeAsync(origem, movimento, cancellationToken).ConfigureAwait(false);

                default:
                    throw OperacaoNaoSuportada("envio", movimento);
            }
        }

        /// <summary>
        /// Consulta o movimento. Antes do envio (<see cref="Movimento.NumeroDocumentoNoBanco"/>
        /// ainda vazio), é a consulta prévia — pra exibir dados ao usuário antes de confirmar o
        /// pagamento (Boleto, Convênio) ou resolver a chave Pix. Depois do envio, é a consulta de
        /// situação/comprovante. Pix Copia e Cola não tem consulta prévia (o pagamento é direto),
        /// só de situação depois de pago.
        /// </summary>
        public Task<Movimento> ConsultarAsync(Movimento movimento, Correntista origem = null, int unidade = 0, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            var jaEnviado = !string.IsNullOrEmpty(movimento.NumeroDocumentoNoBanco);

            switch (item)
            {
                case MovimentoItemPagamentoTituloCodigoBarra _:
                    return jaEnviado
                        ? Boleto.ConsultarComprovantePorIdAsync(movimento, origem, cancellationToken)
                        : Boleto.ConsultarBoletoAsync(movimento, origem, cancellationToken);

                case MovimentoItemPagamentoConvenioCodigoBarra _:
                    return jaEnviado
                        ? Convenio.ConsultarComprovantePorNsuAsync(movimento, origem, cancellationToken)
                        : Convenio.ConsultarCodigoBarrasAsync(movimento, origem, unidade: unidade, cancellationToken: cancellationToken);

                case MovimentoItemTransferenciaTED _:
                    return Ted.ConsultarTedAsync(movimento, cancellationToken);

                case MovimentoItemTransferenciaPIX _:
                    return jaEnviado
                        ? Pix.ConsultarPagamentoAsync(movimento, cancellationToken)
                        : Pix.IniciarPagamentoAsync(movimento, cancellationToken);

                case MovimentoItemPagamentoTituloPIXQRCode _:
                    if (!jaEnviado)
                        throw new NotSupportedException("Pix Copia e Cola não tem consulta prévia — chame EnviarAsync diretamente; ConsultarAsync só se aplica depois do pagamento.");
                    return Pix.ConsultarPagamentoAsync(movimento, cancellationToken);

                default:
                    throw OperacaoNaoSuportada("consulta", movimento);
            }
        }

        /// <summary>
        /// Cancela o agendamento do movimento. Só Boleto e TED têm cancelamento na API do
        /// Sicoob — para Pix e Convênio, lança <see cref="NotSupportedException"/>.
        /// </summary>
        public Task<Movimento> CancelarAsync(Movimento movimento, Correntista origem = null, string idempotencyKey = null, CancellationToken cancellationToken = default)
        {
            switch (ExtrairItem(movimento))
            {
                case MovimentoItemPagamentoTituloCodigoBarra _:
                    return Boleto.CancelarAgendamentoAsync(movimento, origem, cancellationToken);

                case MovimentoItemTransferenciaTED _:
                    return Ted.CancelarAgendamentoAsync(movimento, idempotencyKey ?? Guid.NewGuid().ToString(), cancellationToken);

                default:
                    throw new NotSupportedException($"Cancelamento não é suportado para {movimento?.MovimentoItem?.GetType().Name ?? "null"} — a API do Sicoob não expõe esse endpoint para Pix/Convênio.");
            }
        }

        private static MovimentoItem ExtrairItem(Movimento movimento)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));
            if (movimento.MovimentoItem == null) throw new ArgumentException("Movimento.MovimentoItem é obrigatório — é ele que determina qual operação bancária será chamada.", nameof(movimento));
            return movimento.MovimentoItem;
        }

        private static NotSupportedException OperacaoNaoSuportada(string operacao, Movimento movimento)
            => new NotSupportedException($"Não há {operacao} implementada para {movimento.MovimentoItem.GetType().Name}.");

        private static Guid ParseOuGerarGuid(string idempotencyKey)
            => Guid.TryParse(idempotencyKey, out var guid) ? guid : Guid.NewGuid();
    }
}
