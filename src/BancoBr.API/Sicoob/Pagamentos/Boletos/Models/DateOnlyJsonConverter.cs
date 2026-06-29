using System;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// O Sicoob espera datas no formato yyyy-MM-dd (sem componente de hora) nos payloads
    /// de requisição. O conversor padrão do Newtonsoft.Json grava o horário também.
    /// </summary>
    public class DateOnlyJsonConverter : JsonConverter
    {
        private const string Format = "yyyy-MM-dd";

        public override bool CanConvert(Type objectType) => objectType == typeof(DateTime);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return DateTime.Parse((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((DateTime)value).ToString(Format));
        }
    }
}
