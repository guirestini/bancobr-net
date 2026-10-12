using System;
using System.Collections.Generic;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Cobranca.Boletos.Models
{
    /// <summary>
    /// Corpo de POST /workspaces/{workspace_id}/bank_slips (Hub de Cobrança). Valores monetários e
    /// percentuais vão como texto com ponto decimal (<c>"10.15"</c>), como na documentação; só os
    /// valores de desconto vão como número. Campos opcionais não preenchidos são omitidos.
    /// </summary>
    internal class RequisicaoBoletoCobranca
    {
        [JsonProperty("environment")]
        public string Environment { get; set; }

        [JsonProperty("nsuCode")]
        public string NsuCode { get; set; }

        [JsonProperty("nsuDate"), JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime NsuDate { get; set; }

        [JsonProperty("covenantCode")]
        public string CovenantCode { get; set; }

        [JsonProperty("bankNumber")]
        public string BankNumber { get; set; }

        [JsonProperty("clientNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string ClientNumber { get; set; }

        [JsonProperty("dueDate"), JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime DueDate { get; set; }

        [JsonProperty("issueDate"), JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime IssueDate { get; set; }

        [JsonProperty("participantCode", NullValueHandling = NullValueHandling.Ignore)]
        public string ParticipantCode { get; set; }

        [JsonProperty("nominalValue")]
        public string NominalValue { get; set; }

        [JsonProperty("payer")]
        public BoletoPagador Payer { get; set; }

        [JsonProperty("beneficiary", NullValueHandling = NullValueHandling.Ignore)]
        public BoletoBeneficiarioFinal Beneficiary { get; set; }

        [JsonProperty("documentKind")]
        public string DocumentKind { get; set; }

        [JsonProperty("discount")]
        public BoletoDesconto Discount { get; set; }

        [JsonProperty("finePercentage", NullValueHandling = NullValueHandling.Ignore)]
        public string FinePercentage { get; set; }

        [JsonProperty("fineQuantityDays", NullValueHandling = NullValueHandling.Ignore)]
        public string FineQuantityDays { get; set; }

        [JsonProperty("interestPercentage", NullValueHandling = NullValueHandling.Ignore)]
        public string InterestPercentage { get; set; }

        [JsonProperty("deductionValue", NullValueHandling = NullValueHandling.Ignore)]
        public string DeductionValue { get; set; }

        [JsonProperty("protestType")]
        public string ProtestType { get; set; }

        [JsonProperty("protestQuantityDays", NullValueHandling = NullValueHandling.Ignore)]
        public string ProtestQuantityDays { get; set; }

        [JsonProperty("writeOffQuantityDays", NullValueHandling = NullValueHandling.Ignore)]
        public string WriteOffQuantityDays { get; set; }

        [JsonProperty("paymentType")]
        public string PaymentType { get; set; }

        [JsonProperty("parcelsQuantity", NullValueHandling = NullValueHandling.Ignore)]
        public string ParcelsQuantity { get; set; }

        [JsonProperty("valueType", NullValueHandling = NullValueHandling.Ignore)]
        public string ValueType { get; set; }

        [JsonProperty("minValueOrPercentage", NullValueHandling = NullValueHandling.Ignore)]
        public string MinValueOrPercentage { get; set; }

        [JsonProperty("maxValueOrPercentage", NullValueHandling = NullValueHandling.Ignore)]
        public string MaxValueOrPercentage { get; set; }

        [JsonProperty("iofPercentage", NullValueHandling = NullValueHandling.Ignore)]
        public string IofPercentage { get; set; }

        [JsonProperty("sharing", NullValueHandling = NullValueHandling.Ignore)]
        public List<BoletoPartilha> Sharing { get; set; }

        [JsonProperty("key", NullValueHandling = NullValueHandling.Ignore)]
        public BoletoChaveDict Key { get; set; }

        [JsonProperty("txId", NullValueHandling = NullValueHandling.Ignore)]
        public string TxId { get; set; }

        [JsonProperty("messages", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Messages { get; set; }
    }

    internal class BoletoPagador
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("documentType")]
        public string DocumentType { get; set; }

        [JsonProperty("documentNumber")]
        public string DocumentNumber { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("neighborhood")]
        public string Neighborhood { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("zipCode")]
        public string ZipCode { get; set; }
    }

    internal class BoletoBeneficiarioFinal
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("documentType", NullValueHandling = NullValueHandling.Ignore)]
        public string DocumentType { get; set; }

        [JsonProperty("documentNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string DocumentNumber { get; set; }
    }

    internal class BoletoDesconto
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("discountOne", NullValueHandling = NullValueHandling.Ignore)]
        public BoletoDescontoItem DiscountOne { get; set; }

        [JsonProperty("discountTwo", NullValueHandling = NullValueHandling.Ignore)]
        public BoletoDescontoItem DiscountTwo { get; set; }

        [JsonProperty("discountThree", NullValueHandling = NullValueHandling.Ignore)]
        public BoletoDescontoItem DiscountThree { get; set; }
    }

    internal class BoletoDescontoItem
    {
        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("limitDate"), JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime LimitDate { get; set; }
    }

    internal class BoletoPartilha
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    internal class BoletoChaveDict
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("dictKey")]
        public string DictKey { get; set; }
    }
}
