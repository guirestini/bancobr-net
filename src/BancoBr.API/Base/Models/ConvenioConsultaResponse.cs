namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Informações de um código de barras de convênio antes do pagamento, independente do banco.
    /// </summary>
    public class ConvenioConsultaResponse
    {
        public string Convenio { get; set; }

        public string SiglaConvenio { get; set; }

        public decimal ValorDocumento { get; set; }

        public decimal ValorDesconto { get; set; }

        public decimal ValorMulta { get; set; }

        public decimal ValorJuros { get; set; }

        public decimal ValorOutrosEncargos { get; set; }

        public decimal ValorTotal { get; set; }

        public string CodigoConvenioFebraban { get; set; }

        public long? Nsu { get; set; }

        public long Transacao { get; set; }
    }
}
