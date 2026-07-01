using Newtonsoft.Json;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Resultado de um pagamento de convênio, independente do banco. Cobre tanto o caso de
    /// efetivação imediata (com comprovante) quanto o de pendência de assinatura.
    /// </summary>
    public class PagamentoConvenioResultado
    {
        public bool PendenteAssinatura { get; }

        public ArrecadacaoResultado Resultado { get; }

        [JsonProperty("BancoBrSituacao")]
        public BancoBrSituacaoEnum BancoBrSituacao { get; }

        private PagamentoConvenioResultado(bool pendenteAssinatura, ArrecadacaoResultado resultado, BancoBrSituacaoEnum bancoBrSituacao)
        {
            PendenteAssinatura = pendenteAssinatura;
            Resultado = resultado;
            BancoBrSituacao = bancoBrSituacao;
        }

        public static PagamentoConvenioResultado Efetivado(ArrecadacaoResultado resultado) =>
            new PagamentoConvenioResultado(pendenteAssinatura: false, resultado, BancoBrSituacaoEnum.Efetivado);

        public static PagamentoConvenioResultado PendenteDeAssinatura() =>
            new PagamentoConvenioResultado(pendenteAssinatura: true, resultado: null, BancoBrSituacaoEnum.Agendado);
    }
}
