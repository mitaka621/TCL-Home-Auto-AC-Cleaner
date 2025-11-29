using System.Text.Json.Serialization;

namespace TCL_Home_Auto_AC_Cleaner.Models.Devices;

public class UserGroup
{
    [JsonPropertyName("centralControlId")]
    public string? CentralControlId { get; set; }

    [JsonPropertyName("centralControlName")]
    public string? CentralControlName { get; set; }

    [JsonPropertyName("properties")]
    public List<string>? Properties { get; set; }

    [JsonPropertyName("centralControlIdentifier")]
    public string? CentralControlIdentifier { get; set; }

    [JsonPropertyName("devices")]
    public List<DeviceInfo>? Devices { get; set; }
}

