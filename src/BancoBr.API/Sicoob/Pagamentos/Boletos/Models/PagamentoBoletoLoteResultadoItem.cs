using System;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Resultado de um item processado dentro de um lote. Um item com falha (ex.:
    /// <see cref="BancoBr.API.Sicoob.Errors.SicoobApiException"/> ou erro de transporte) não
    /// aborta o restante do lote — fica registrado aqui para o ERP decidir o que fazer.
    /// </summary>
    public class PagamentoBoletoLoteResultadoItem
    {
        public PagamentoBoletoLoteItem Item { get; }

        public BancoBr.API.Base.Models.PagamentoBoletoResultado Resultado { get; }

        public Exception Erro { get; }

        public bool Sucesso => Erro == null;

        private PagamentoBoletoLoteResultadoItem(PagamentoBoletoLoteItem item, BancoBr.API.Base.Models.PagamentoBoletoResultado resultado, Exception erro)
        {
            Item = item;
            Resultado = resultado;
            Erro = erro;
        }

        public static PagamentoBoletoLoteResultadoItem ComSucesso(PagamentoBoletoLoteItem item, BancoBr.API.Base.Models.PagamentoBoletoResultado resultado) =>
            new PagamentoBoletoLoteResultadoItem(item, resultado, erro: null);

        public static PagamentoBoletoLoteResultadoItem ComFalha(PagamentoBoletoLoteItem item, Exception erro) =>
            new PagamentoBoletoLoteResultadoItem(item, resultado: null, erro);
    }
}
