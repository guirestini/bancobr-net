using System;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Cobranca.Boletos.Models
{
    /// <summary>
    /// Resposta 201 de POST bank_slips: devolve a requisição inteira acrescida do resultado do
    /// registro. Só os campos de resultado interessam; o restante é ignorado na desserialização.
    /// </summary>
    internal class BoletoCobrancaResponse
    {
        [JsonProperty("bankNumber")]
        public string BankNumber { get; set; }

        [JsonProperty("barcode")]
        public string Barcode { get; set; }

        [JsonProperty("digitableLine")]
        public string DigitableLine { get; set; }

        [JsonProperty("entryDate"), JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime? EntryDate { get; set; }

        [JsonProperty("qrCodePix")]
        public string QrCodePix { get; set; }

        [JsonProperty("qrCodeUrl")]
        public string QrCodeUrl { get; set; }
    }
}
