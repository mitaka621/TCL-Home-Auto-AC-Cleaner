using System.Text.Json.Serialization;

namespace TCL_Home_Auto_AC_Cleaner.Models.Auth;

public class UserLoginResponse
{
    [JsonPropertyName("user")]
    public UserInfo? User { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("refreshtoken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("data")]
    public LoginData? Data { get; set; }

    [JsonPropertyName("msg")]
    public string? Message { get; set; }

    [JsonPropertyName("firstLogin")]
    public int FirstLogin { get; set; }
}

