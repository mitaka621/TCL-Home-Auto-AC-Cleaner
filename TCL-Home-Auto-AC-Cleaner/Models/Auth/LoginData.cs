using System.Text.Json.Serialization;

namespace TCL_Home_Auto_AC_Cleaner.Models.Auth;

public class LoginData
{
    [JsonPropertyName("loginAccount")]
    public string? LoginAccount { get; set; }

    [JsonPropertyName("loginType")]
    public int LoginType { get; set; }

    [JsonPropertyName("loginCountryAbbr")]
    public string? LoginCountryAbbr { get; set; }
}

