# TCL Home Auto AC Cleaner

A C# service for interacting with TCL air conditioner devices through the TCL Home API. This service can authenticate with TCL, retrieve device lists, and trigger cleaning functionality on AC devices.

## Features

- **Authentication**: Authenticates with TCL API using email and password from user secrets
- **Device Discovery**: Retrieves list of all AC devices with their status and properties
- **AC Cleaning**: Triggers self-cleaning functionality on specified AC devices via AWS IoT Core
- **Database Logging** (Optional): When enabled, logs exceptions, device information, status history, and cleaning operations to SQL Server database

## Setup

### Prerequisites

- .NET 9.0 SDK or later
- TCL Home account credentials (if you are logging in with google in the tcl app it will not work)

### 1. Configure User Secrets

Add your TCL credentials and optionally database settings to Visual Studio User Secrets:

**Using .NET CLI:**

```bash
dotnet user-secrets set "TCL:Username" "your-email@example.com"
dotnet user-secrets set "TCL:Password" "your-password"

# Optional: Enable database logging
dotnet user-secrets set "UseDatabase" "true"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=TCLHome;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

**Using Visual Studio:**

- Right-click the project -> Manage User Secrets
- Add the following JSON:

```json
{
  "TCL": {
    "Username": "your-email@example.com",
    "Password": "your-password"
  },
  "UseDatabase": false,
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=TCLHome;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

**Note:** Set `UseDatabase` to `true` to enable database logging. When enabled, the application will:

- Create database tables automatically on startup
- Log all exceptions to the `Exceptions` table
- Store device information in the `Devices` table
- Track device status history in the `DeviceStatuses` table
- Record cleaning operations in the `Cleanings` table

### 2. Build and Run

```bash
dotnet build
dotnet run
```

## Usage

The application will:

1. Authenticate with TCL API
2. Fetch and display all available devices
3. Automatically send clean commands to all online devices

### Programmatic Usage

```csharp
var service = host.Services.GetRequiredService<TclAcService>();

// Authenticate
await service.AuthenticateAsync();

// Get devices
var devices = await service.GetDevicesAsync();

// Clean specific devices
await service.CleanAcsAsync(new List<string> { "DP2bNBGFAAAE" });
```

## API Methods

### `AuthenticateAsync()`

Authenticates with TCL API using credentials from user secrets. Returns `AuthTokens` containing access tokens, Cognito tokens, and MQTT endpoint information.

### `GetDevicesAsync()`

Retrieves list of all devices from TCL API. Returns `Dictionary<string, DeviceInfo>` where the key is the device ID.

**DeviceInfo Properties:**

- `DeviceId` - Unique device identifier
- `DeviceName` - Device name
- `NickName` - User-assigned nickname
- `IsOnline` - Online status (Online/Offline/Unknown)
- `Identifiers` - Device state identifiers (power switch, etc.)
- `TemperatureType` - Temperature unit preference
- `Ssid` - WiFi network name
- `LocationName` - Device location

### `CleanAcsAsync(List<string> deviceIds, string? version = null)`

Triggers cleaning functionality for specified AC devices. The method:

- Obtains AWS credentials via Cognito Identity
- Publishes a shadow update command to AWS IoT Core
- Sets `selfClean: 1` and `powerSwitch: 0` in the device shadow

## Database Schema

When `UseDatabase` is enabled, the following tables are created:

### Exceptions

- `Id` (int, PK)
- `Name` (string) - Exception type name
- `Value` (string) - Full exception details
- `TimeOccurred` (DateTime) - When the exception occurred

### Devices

- `DeviceId` (string, PK) - Unique device identifier
- `DeviceName` (string) - Device name
- `NickName` (string) - User-assigned nickname
- `IsOnline` (enum) - Online status
- `TemperatureType` (int) - Temperature unit preference
- `Ssid` (string) - WiFi network name
- `LocationName` (string) - Device location
- `LastUpdated` (DateTime) - Last update timestamp

### DeviceStatuses

- `Id` (int, PK)
- `DeviceId` (string, FK -> Devices) - Reference to device
- `TimeStatusExtracted` (DateTime) - When status was extracted
- `StatusProperties` (string) - JSON serialized device identifiers/status

### Cleanings

- `Id` (int, PK)
- `DeviceId` (string, FK -> Devices) - Reference to device
- `TimeCleaningStarted` (DateTime) - When cleaning was initiated

## Dependencies

- `AWSSDK.CognitoIdentity` - AWS Cognito Identity for credential management
- `AWSSDK.IotData` - AWS IoT Data Plane for device shadow updates
- `Microsoft.EntityFrameworkCore.SqlServer` - Entity Framework Core for SQL Server
- `Microsoft.EntityFrameworkCore.Design` - EF Core design-time tools
- `Microsoft.Extensions.Configuration.UserSecrets` - Secure credential storage
- `Microsoft.Extensions.Hosting` - Dependency injection and hosting
- `Microsoft.Extensions.Http` - HTTP client factory
- `System.IdentityModel.Tokens.Jwt` - JWT token parsing
- `System.Text.Json` - JSON serialization

## License

This project is for educational purposes. Use at your own risk.
