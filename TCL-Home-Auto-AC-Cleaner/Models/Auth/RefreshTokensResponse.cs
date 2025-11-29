using System.Text.Json.Serialization;

namespace TCL_Home_Auto_AC_Cleaner.Models.Auth;

public class RefreshTokensResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public RefreshTokenData? Data { get; set; }
}

