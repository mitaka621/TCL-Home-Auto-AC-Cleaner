using System.Text.Json.Serialization;
using TCL_Home_Auto_AC_Cleaner.Converters;
using TCL_Home_Auto_AC_Cleaner.Enums;

namespace TCL_Home_Auto_AC_Cleaner.Models.Devices;

public class DeviceInfo
{
    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("nickName")]
    public string? NickName { get; set; }

    [JsonPropertyName("deviceName")]
    public string? DeviceName { get; set; }

    [JsonPropertyName("identifiers")]
    public List<DeviceIdentifier>? Identifiers { get; set; }

    [JsonPropertyName("isOnline")]
    [JsonConverter(typeof(OnlineStatusConverter))]
    public OnlineStatusEnum IsOnline { get; set; }

    [JsonPropertyName("temperatureType")]
    public int TemperatureType { get; set; }

    [JsonPropertyName("ssid")]
    public string? Ssid { get; set; }

    [JsonPropertyName("locationName")]
    public string? LocationName { get; set; }

    public override string ToString()
    {
        var identifiers = Identifiers != null
            ? string.Join(", ", Identifiers.Select(i => $"{i.Identifier ?? "N/A"}: {i.Value}"))
            : "N/A";
        return
            $"DeviceId: {DeviceId ?? "N/A"}, {Environment.NewLine}" +
            $"NickName: {NickName ?? "N/A"}, {Environment.NewLine}" +
            $"DeviceName: {DeviceName ?? "N/A"}, {Environment.NewLine}" +
            $"Identifiers: [{identifiers}], {Environment.NewLine}" +
            $"IsOnline: {IsOnline}, {Environment.NewLine}" +
            $"TemperatureType: {TemperatureType}, {Environment.NewLine}" +
            $"Ssid: {Ssid ?? "N/A"}, {Environment.NewLine}" +
            $"LocationName: {LocationName ?? "N/A"}";
    }
}

