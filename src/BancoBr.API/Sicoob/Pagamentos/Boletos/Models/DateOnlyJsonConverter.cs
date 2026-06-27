using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// O Sicoob espera datas no formato yyyy-MM-dd (sem componente de hora) nos payloads
    /// de requisição. O conversor padrão do System.Text.Json grava o horário também.
    /// </summary>
    public class DateOnlyJsonConverter : JsonConverter<DateTime>
    {
        private const string Format = "yyyy-MM-dd";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(Format));
        }
    }
}
