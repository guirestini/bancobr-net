using System;
using Newtonsoft.Json;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Comprovante de um pagamento de boleto, independente do banco.
    /// </summary>
    public class ComprovantePagamento
    {
        public string NumeroAgencia { get; set; }

        public string NomeAgencia { get; set; }

        public long NumeroConta { get; set; }

        public string NomeProprietarioContaCorrente { get; set; }

        public string NumeroLinhaDigitavel { get; set; }

        public int NumeroInstituicaoEmissora { get; set; }

        public string NomeInstituicaoEmissora { get; set; }

        public string NumeroCpfCnpjBeneficiario { get; set; }

        public string NomeRazaoSocialBeneficiario { get; set; }

        public string NumeroCpfCnpjPagador { get; set; }

        public string NomeRazaoSocialPagador { get; set; }

        public DateTime DataVencimento { get; set; }

        public decimal ValorBoleto { get; set; }

        public decimal ValorAbatimentoDesconto { get; set; }

        public decimal ValorMultaMora { get; set; }

        public decimal ValorPagamento { get; set; }

        public DateTime DataPagamento { get; set; }

        public string SituacaoPagamento { get; set; }

        public string DescricaoDetalheSituacao { get; set; }

        public DateTime? DataHoraCadastro { get; set; }

        public bool AceitaValorDivergente { get; set; }

        public string NossoNumero { get; set; }

        public string NumeroDocumento { get; set; }

        public string DescricaoObservacao { get; set; }

        public string DescricaoOuvidoria { get; set; }

        public string DescricaoTituloComprovante { get; set; }

        public long IdPagamento { get; set; }

        public string NumeroAutenticacaoPagamento { get; set; }

        [JsonProperty("BancoBrSituacao")]
        public BancoBrSituacaoEnum BancoBrSituacao { get; set; }
    }
}
