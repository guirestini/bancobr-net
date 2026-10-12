using System.Collections.Generic;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Cobranca.Boletos.Models
{
    /// <summary>
    /// Resposta de GET /workspaces/{id}/bank_slips (listagem paginada de boletos). Datas e valores
    /// ficam como texto: a documentação os dá como texto mas os exemplos trazem números
    /// (<c>paidValue: 90</c>, <c>bankCode: 33</c>), então a conversão é tolerante no mapeamento.
    /// </summary>
    internal class RetornoBoletoCobrancaListResponse
    {
        [JsonProperty("_pageable")]
        public PaginacaoCobranca Pageable { get; set; }

        [JsonProperty("_content")]
        public List<RetornoBoletoCobranca> Content { get; set; }
    }

    internal class RetornoBoletoCobranca
    {
        [JsonProperty("nsuCode")]
        public string NsuCode { get; set; }

        [JsonProperty("nsuDate")]
        public string NsuDate { get; set; }

        [JsonProperty("covenantCode")]
        public string CovenantCode { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("statusComplement")]
        public string StatusComplement { get; set; }

        [JsonProperty("payer")]
        public BoletoPagador Payer { get; set; }

        [JsonProperty("beneficiary")]
        public BoletoBeneficiarioFinal Beneficiary { get; set; }

        [JsonProperty("bankNumber")]
        public string BankNumber { get; set; }

        [JsonProperty("clientNumber")]
        public string ClientNumber { get; set; }

        [JsonProperty("dueDate")]
        public string DueDate { get; set; }

        [JsonProperty("issueDate")]
        public string IssueDate { get; set; }

        [JsonProperty("nominalValue")]
        public string NominalValue { get; set; }

        [JsonProperty("payment")]
        public PagamentoBoletoCobranca Payment { get; set; }

        [JsonProperty("barcode")]
        public string Barcode { get; set; }

        [JsonProperty("digitableLine")]
        public string DigitableLine { get; set; }

        [JsonProperty("entryDate")]
        public string EntryDate { get; set; }
    }

    internal class PagamentoBoletoCobranca
    {
        [JsonProperty("paidValue")]
        public string PaidValue { get; set; }

        [JsonProperty("interestValue")]
        public string InterestValue { get; set; }

        [JsonProperty("fineValue")]
        public string FineValue { get; set; }

        [JsonProperty("deductionValue")]
        public string DeductionValue { get; set; }

        [JsonProperty("rebateValue")]
        public string RebateValue { get; set; }

        [JsonProperty("iofValue")]
        public string IofValue { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("bankCode")]
        public string BankCode { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("txId")]
        public string TxId { get; set; }
    }
}
