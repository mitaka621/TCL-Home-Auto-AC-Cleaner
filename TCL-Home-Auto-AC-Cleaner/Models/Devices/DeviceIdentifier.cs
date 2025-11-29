using System.Text.Json.Serialization;

namespace TCL_Home_Auto_AC_Cleaner.Models.Devices;

public class DeviceIdentifier
{
    [JsonPropertyName("identifier")]
    public string? Identifier { get; set; }

    [JsonPropertyName("value")]
    public int Value { get; set; }
}

