namespace TCL_Home_Auto_AC_Cleaner.Models.Auth;

public class AuthTokens
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? SaasToken { get; set; }
    public string? SsoToken { get; set; }
    public string? Username { get; set; }
    public string? Country { get; set; }
    public string? CognitoId { get; set; }
    public string? CognitoToken { get; set; }
    public string? MqttEndpoint { get; set; }
}

