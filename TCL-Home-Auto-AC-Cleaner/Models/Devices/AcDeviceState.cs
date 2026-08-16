using System.Text.Json;
using System.Text.Json.Serialization;

namespace TCL_Home_Auto_AC_Cleaner.Models.Devices;

public class AcDeviceState
{
    private static readonly string[] RestorePropertyNames =
    [
        "powerSwitch",
        "targetTemperature",
        "targetFahrenheitTemp",
        "workMode",
        "windSpeed7Gear",
        "windSpeed",
        "verticalWind",
        "horizontalWind",
        "horizontalSwitch",
        "verticalSwitch",
        "temperatureType",
        "windSpeedAutoSwitch",
        "ECO",
        "turbo",
        "sleep",
        "screen",
        "beepSwitch",
        "silenceSwitch",
        "AIECOSwitch"
    ];

    public string DeviceId { get; init; } = string.Empty;

    public int? PowerSwitch { get; set; }

    public int? SelfClean { get; set; }

    public double? TargetTemperature { get; set; }

    public int? WorkMode { get; set; }

    public int? WindSpeed7Gear { get; set; }

    [JsonIgnore]
    public Dictionary<string, JsonElement> RawProperties { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public bool WasPoweredOn => PowerSwitch == 1;

    public bool IsCleaningActive => SelfClean == 1;

    public static AcDeviceState FromShadow(string deviceId, string shadowJson)
    {
        using var document = JsonDocument.Parse(shadowJson);
        return FromShadow(deviceId, document.RootElement);
    }

    public static AcDeviceState FromShadow(string deviceId, JsonElement shadowRoot)
    {
        var state = new AcDeviceState { DeviceId = deviceId };

        if (!shadowRoot.TryGetProperty("state", out var stateElement))
        {
            return state;
        }

        JsonElement reportedElement = default;
        var hasReported = stateElement.TryGetProperty("reported", out reportedElement);

        if (!hasReported && stateElement.TryGetProperty("desired", out var desiredFallback))
        {
            reportedElement = desiredFallback;
            hasReported = true;
        }

        if (!hasReported)
        {
            return state;
        }

        foreach (var property in reportedElement.EnumerateObject())
        {
            state.RawProperties[property.Name] = property.Value.Clone();
        }

        state.PowerSwitch = ReadInt(reportedElement, "powerSwitch");
        state.SelfClean = ReadInt(reportedElement, "selfClean");
        state.TargetTemperature = ReadDouble(reportedElement, "targetTemperature");
        state.WorkMode = ReadInt(reportedElement, "workMode");
        state.WindSpeed7Gear = ReadInt(reportedElement, "windSpeed7Gear")
            ?? ReadInt(reportedElement, "windSpeed");

        return state;
    }

    public Dictionary<string, object?> BuildRestoreDesiredState()
    {
        var desired = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["selfClean"] = 0
        };

        if (!WasPoweredOn)
        {
            return desired;
        }

        desired["powerSwitch"] = 1;

        foreach (var propertyName in RestorePropertyNames)
        {
            if (propertyName is "powerSwitch" or "selfClean")
            {
                continue;
            }

            if (RawProperties.TryGetValue(propertyName, out var value))
            {
                desired[propertyName] = ConvertJsonElement(value);
            }
        }

        return desired;
    }

    public override string ToString()
    {
        return
            $"DeviceId: {DeviceId}, PowerSwitch: {PowerSwitch?.ToString() ?? "N/A"}, " +
            $"SelfClean: {SelfClean?.ToString() ?? "N/A"}, TargetTemperature: {TargetTemperature?.ToString() ?? "N/A"}, " +
            $"WorkMode: {WorkMode?.ToString() ?? "N/A"}, WindSpeed7Gear: {WindSpeed7Gear?.ToString() ?? "N/A"}";
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            JsonValueKind.True => 1,
            JsonValueKind.False => 0,
            _ => null
        };
    }

    private static double? ReadDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.String when double.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static object? ConvertJsonElement(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var intValue) => intValue,
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => 1,
            JsonValueKind.False => 0,
            JsonValueKind.Array => value.GetRawText(),
            JsonValueKind.Object => value.GetRawText(),
            _ => null
        };
    }
}
