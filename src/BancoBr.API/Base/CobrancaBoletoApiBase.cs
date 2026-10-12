using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base.Models;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes da API de cobrança (emissão/registro de boletos), independente
    /// do banco — mesma ideia de <see cref="PagamentoPixApiBase"/>: cada banco herda e
    /// implementa os métodos abaixo de acordo com sua própria API.
    ///
    /// O contrato público é o mesmo <see cref="Movimento"/>/<see cref="MovimentoItem"/> do
    /// restante da BancoBr.API: o método recebe o movimento, mapeia para o formato da API do
    /// banco, e aplica a resposta de volta no mesmo objeto — que é então devolvido (mesma
    /// instância, mutada in-place).
    ///
    /// Ao contrário dos pagamentos, aqui o <see cref="Correntista"/> é o <b>beneficiário</b>
    /// (quem emite e recebe o boleto), e o pagador é <see cref="Movimento.Favorecido"/>.
    /// </summary>
    internal abstract class CobrancaBoletoApiBase
    {
        protected CobrancaBoletoApiBase(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; }

        /// <summary>
        /// Registra um boleto de cobrança. Espera um <see cref="MovimentoItemBoletoCobranca"/>
        /// como <see cref="Movimento.MovimentoItem"/>; devolve nele o código de barras, a linha
        /// digitável e o QR Code Pix.
        /// </summary>
        internal abstract Task<Movimento> EmitirBoletoAsync(Movimento movimento, Correntista beneficiario, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consulta única de retorno de cobrança: devolve um <see cref="Movimento"/> por boleto,
        /// sempre com um <see cref="MovimentoItemRetornoCobranca"/> — o <paramref name="tipo"/>
        /// define o que é consultado (liquidações, ...) e o formato do resultado é o mesmo para
        /// todos. <paramref name="nossoNumero"/> e <paramref name="seuNumero"/> são filtros
        /// opcionais; um tipo que o banco não suporta lança <see cref="System.NotSupportedException"/>.
        /// </summary>
        internal abstract Task<IReadOnlyList<Movimento>> ConsultarRetornoAsync(Correntista beneficiario, TipoRetornoCobrancaEnum tipo, DateTime dataInicial, DateTime dataFinal, TipoDataRetornoCobrancaEnum tipoData, string nossoNumero, string seuNumero, CancellationToken cancellationToken = default);
    }
}
