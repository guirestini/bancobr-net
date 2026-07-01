using System;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Resultado de um item processado dentro de um lote. Um item com falha não aborta o
    /// restante do lote — fica registrado aqui para o ERP decidir o que fazer.
    /// </summary>
    public class PagamentoBoletoLoteResultadoItem
    {
        public PagamentoBoletoLoteItem Item { get; }

        public PagamentoBoletoResultado Resultado { get; }

        public Exception Erro { get; }

        public bool Sucesso => Erro == null;

        private PagamentoBoletoLoteResultadoItem(PagamentoBoletoLoteItem item, PagamentoBoletoResultado resultado, Exception erro)
        {
            Item = item;
            Resultado = resultado;
            Erro = erro;
        }

        public static PagamentoBoletoLoteResultadoItem ComSucesso(PagamentoBoletoLoteItem item, PagamentoBoletoResultado resultado) =>
            new PagamentoBoletoLoteResultadoItem(item, resultado, erro: null);

        public static PagamentoBoletoLoteResultadoItem ComFalha(PagamentoBoletoLoteItem item, Exception erro) =>
            new PagamentoBoletoLoteResultadoItem(item, resultado: null, erro);
    }
}
