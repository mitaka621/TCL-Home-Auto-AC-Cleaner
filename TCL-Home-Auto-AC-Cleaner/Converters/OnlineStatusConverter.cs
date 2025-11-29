using System.Text.Json;
using System.Text.Json.Serialization;
using TCL_Home_Auto_AC_Cleaner.Enums;

namespace TCL_Home_Auto_AC_Cleaner.Converters;

public class OnlineStatusConverter : JsonConverter<OnlineStatusEnum>
{
    public override OnlineStatusEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            return stringValue == "1" ? OnlineStatusEnum.Online : OnlineStatusEnum.Offline;
        }
        
        if (reader.TokenType == JsonTokenType.Number)
        {
            var intValue = reader.GetInt32();
            return intValue == 1 ? OnlineStatusEnum.Online : OnlineStatusEnum.Offline;
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, OnlineStatusEnum value, JsonSerializerOptions options)
    {
        var stringValue = value == OnlineStatusEnum.Online ? "1" : "0";
        writer.WriteStringValue(stringValue);
    }
}

