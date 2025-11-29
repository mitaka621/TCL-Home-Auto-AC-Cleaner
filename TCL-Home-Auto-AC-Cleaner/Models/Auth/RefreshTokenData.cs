using System.Text.Json.Serialization;

namespace TCL_Home_Auto_AC_Cleaner.Models.Auth;

public class RefreshTokenData
{
    [JsonPropertyName("saasToken")]
    public string? SaasToken { get; set; }

    [JsonPropertyName("cognitoId")]
    public string? CognitoId { get; set; }

    [JsonPropertyName("cognitoToken")]
    public string? CognitoToken { get; set; }

    [JsonPropertyName("mqttEndpoint")]
    public string? MqttEndpoint { get; set; }
}

