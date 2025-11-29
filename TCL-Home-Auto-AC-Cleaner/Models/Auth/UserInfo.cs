using System.Text.Json.Serialization;

namespace TCL_Home_Auto_AC_Cleaner.Models.Auth;

public class UserInfo
{
    [JsonPropertyName("countryAbbr")]
    public string? CountryAbbr { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

