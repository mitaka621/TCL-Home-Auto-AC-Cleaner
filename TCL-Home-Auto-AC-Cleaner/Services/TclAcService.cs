using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentity.Model;
using Amazon.IotData;
using Amazon.IotData.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TCL_Home_Auto_AC_Cleaner.Data;
using TCL_Home_Auto_AC_Cleaner.Data.Entities;
using TCL_Home_Auto_AC_Cleaner.Models.Auth;
using TCL_Home_Auto_AC_Cleaner.Models.Devices;

namespace TCL_Home_Auto_AC_Cleaner.Services;

public class TclAcService : IDisposable
{
    private const string _clientId = "54148614";
    private const string _appId = "wx6e1af3fa84fbe523";
    private const string _baseAccountUrl = "https://pa.account.tcl.com";
    private const string _baseApiUrl = "https://prod-eu.aws.tcljd.com";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _useDatabase;
    private AuthTokens? _authTokens;

    public TclAcService(HttpClient httpClient, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _httpClient.DefaultRequestHeaders.Add("user-agent", "Android");
        _useDatabase = _configuration.GetValue<bool>("UseDatabase", false);
    }

    public async Task<AuthTokens> AuthenticateAsync()
    {
        var username = _configuration["TCL:Username"]
            ?? throw new InvalidOperationException("TCL:Username not found in user secrets");
        var password = _configuration["TCL:Password"]
            ?? throw new InvalidOperationException("TCL:Password not found in user secrets");

        var loginResponse = await DoAccountAuthAsync(username, password);

        var refreshResponse = await GetSaasTokenAsync(loginResponse.Token!, loginResponse.User!.Username!);

        _authTokens = new AuthTokens
        {
            AccessToken = loginResponse.Token,
            RefreshToken = loginResponse.RefreshToken,
            SaasToken = refreshResponse.Data!.SaasToken,
            SsoToken = loginResponse.Token,
            Username = loginResponse.User.Username,
            Country = loginResponse.User.CountryAbbr,
            CognitoId = refreshResponse.Data.CognitoId,
            CognitoToken = refreshResponse.Data.CognitoToken,
            MqttEndpoint = refreshResponse.Data.MqttEndpoint
        };

        return _authTokens;
    }

    public async Task<Dictionary<string, DeviceInfo>> GetDevicesAsync()
    {
        if (_authTokens == null)
        {
            await AuthenticateAsync();
        }

        if (_authTokens == null || string.IsNullOrEmpty(_authTokens.SaasToken))
        {
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var nonce = Guid.NewGuid().ToString();
        var sign = CalculateMd5Hash(timestamp + nonce + _authTokens.SaasToken);

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseApiUrl}/v3/central/control/user_groups/get");

        request.Headers.Add("ssotoken", _authTokens.SsoToken ?? _authTokens.AccessToken);
        request.Headers.Add("appid", _appId);
        request.Headers.Add("platform", "android");
        request.Headers.Add("appversion", "7.3.0");
        request.Headers.Add("thomeversion", "5.1.8");
        request.Headers.Add("accesstoken", _authTokens.SaasToken);
        request.Headers.Add("countrycode", _authTokens.Country ?? "BG");
        request.Headers.Add("timezone", "Africa/Harare");
        request.Headers.Add("accept-language", "en");
        request.Headers.Add("timestamp", timestamp);
        request.Headers.Add("nonce", nonce);
        request.Headers.Add("sign", sign);
        request.Headers.Add("accept-encoding", "gzip, deflate, br");

        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var deviceListResponse = JsonSerializer.Deserialize<DeviceListResponse>(json);

        if (deviceListResponse?.Data == null)
        {
            return new Dictionary<string, DeviceInfo>();
        }

        var devices = new Dictionary<string, DeviceInfo>();
        foreach (var group in deviceListResponse.Data)
        {
            if (group.Devices == null) continue;

            foreach (var device in group.Devices)
            {
                if (string.IsNullOrEmpty(device.DeviceId)) continue;

                if (!devices.ContainsKey(device.DeviceId))
                {
                    devices.Add(device.DeviceId, device);
                }
            }
        }

        if (_useDatabase)
        {
            await SaveDevicesToDatabaseAsync(devices);
        }

        return devices;
    }

    public async Task CleanAcsAsync(List<string> deviceIds, string? version = null)
    {
        if (_authTokens == null)
        {
            await AuthenticateAsync();
        }

        if (_authTokens == null || string.IsNullOrEmpty(_authTokens.CognitoToken))
        {
            throw new InvalidOperationException("Not authenticated or missing Cognito token. Call AuthenticateAsync first.");
        }

        var mqttEndpoint = _authTokens.MqttEndpoint ?? "data.iot.eu-central-1.amazonaws.com";
        var region = ExtractRegionFromEndpoint(mqttEndpoint);

        foreach (var deviceId in deviceIds)
        {
            await SendCleanCommandAsync(deviceId, region, _authTokens.CognitoToken, version);
        }
    }

    private async Task<UserLoginResponse> DoAccountAuthAsync(string username, string password)
    {
        var passwordHash = CalculateMd5Hash(password);

        var loginRequest = new
        {
            equipment = 2,
            password = passwordHash,
            osType = 1,
            username = username,
            clientVersion = "4.8.1",
            osVersion = "6.0",
            deviceModel = "AndroidAndroid SDK built for x86",
            captchaRule = 2,
            channel = "app"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseAccountUrl}/account/login?clientId={_clientId}");
        request.Headers.Add("th_platform", "android");
        request.Headers.Add("th_version", "4.8.1");
        request.Headers.Add("th_appbulid", "830");

        request.Content = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UserLoginResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize login response");
    }

    private async Task<RefreshTokensResponse> GetSaasTokenAsync(string accessToken, string username)
    {
        var refreshRequest = new
        {
            userId = username,
            ssoToken = accessToken,
            appId = _appId
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseApiUrl}/v3/auth/refresh_tokens");
        request.Headers.Add("accept-encoding", "gzip, deflate, br");

        request.Content = new StringContent(
            JsonSerializer.Serialize(refreshRequest),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RefreshTokensResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize refresh token response");
    }

    private async Task SendCleanCommandAsync(string deviceId, string region, string cognitoToken, string? version)
    {
        try
        {
            var credentials = await GetAwsCredentialsAsync(cognitoToken, region);

            var cleanCommand = new
            {
                state = new
                {
                    desired = new
                    {
                        selfClean = 1,
                        powerSwitch = 0
                    }
                },
                clientToken = $"mobile_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            };

            var payload = JsonSerializer.Serialize(cleanCommand);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            var sessionCredentials = new SessionAWSCredentials(
                credentials.AccessKeyId,
                credentials.SecretAccessKey,
                credentials.SessionToken
            );

            var clientConfig = new AmazonIotDataConfig
            {
                ServiceURL = $"https://data.iot.{region}.amazonaws.com"
            };
            clientConfig.HttpClientFactory = new BypassSslHttpClientFactory();

            var iotDataClient = new AmazonIotDataClient(sessionCredentials, clientConfig);

            var topic = $"$aws/things/{deviceId}/shadow/update";
            var publishRequest = new PublishRequest
            {
                Topic = topic,
                Payload = new MemoryStream(payloadBytes),
                Qos = 1
            };

            var response = await iotDataClient.PublishAsync(publishRequest);
            Console.WriteLine($"Clean command sent to device {deviceId}");

            if (_useDatabase)
            {
                await SaveCleaningToDatabaseAsync(deviceId);
            }
        }
        catch (Exception ex)
        {
            if (_useDatabase)
            {
                await LogExceptionToDatabaseAsync("SendCleanCommand", ex.ToString());
            }
            throw;
        }
    }

    private async Task<CognitoCredentials> GetAwsCredentialsAsync(string cognitoToken, string region)
    {
        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken jsonToken;

        try
        {
            jsonToken = handler.ReadJwtToken(cognitoToken);
        }
        catch
        {
            throw new InvalidOperationException("Failed to parse cognitoToken as JWT");
        }

        var identityId = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (string.IsNullOrEmpty(identityId))
        {
            identityId = _authTokens?.CognitoId;
        }

        if (string.IsNullOrEmpty(identityId))
        {
            throw new InvalidOperationException("Cannot determine Identity ID from cognitoToken");
        }

        const string providerKey = "cognito-identity.amazonaws.com";

        var cognitoClient = new AmazonCognitoIdentityClient(
            new AnonymousAWSCredentials(),
            new AmazonCognitoIdentityConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
            });

        var logins = new Dictionary<string, string> { { providerKey, cognitoToken } };
        var request = new GetCredentialsForIdentityRequest
        {
            IdentityId = identityId,
            Logins = logins
        };

        var response = await cognitoClient.GetCredentialsForIdentityAsync(request);

        if (response.Credentials == null)
        {
            throw new InvalidOperationException("Failed to obtain AWS credentials from Cognito");
        }

        return new CognitoCredentials
        {
            AccessKeyId = response.Credentials.AccessKeyId,
            SecretAccessKey = response.Credentials.SecretKey,
            SessionToken = response.Credentials.SessionToken
        };
    }

    private string ExtractRegionFromEndpoint(string endpoint)
    {
        var parts = endpoint.Split('.');
        if (parts.Length >= 3 && parts[1] == "iot")
        {
            return parts[2];
        }
        return "eu-central-1";
    }

    private static string CalculateMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);

        var hexChars = new StringBuilder();
        foreach (var b in hashBytes)
        {
            var byteValue = b & 255;
            if (byteValue < 16)
            {
                hexChars.Append('0');
            }
            hexChars.Append(byteValue.ToString("x"));
        }

        return hexChars.ToString();
    }

    private async Task SaveDevicesToDatabaseAsync(Dictionary<string, DeviceInfo> devices)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TclHomeDbContext>();

            foreach (var kvp in devices)
            {
                var deviceInfo = kvp.Value;
                if (string.IsNullOrEmpty(deviceInfo.DeviceId)) continue;

                var device = await dbContext.Devices.FindAsync(deviceInfo.DeviceId);
                if (device == null)
                {
                    device = new Device
                    {
                        DeviceId = deviceInfo.DeviceId,
                        DeviceName = deviceInfo.DeviceName,
                        NickName = deviceInfo.NickName,
                        IsOnline = deviceInfo.IsOnline,
                        TemperatureType = deviceInfo.TemperatureType,
                        Ssid = deviceInfo.Ssid,
                        LocationName = deviceInfo.LocationName,
                        LastUpdated = DateTime.Now
                    };
                    dbContext.Devices.Add(device);
                }
                else
                {
                    device.DeviceName = deviceInfo.DeviceName;
                    device.NickName = deviceInfo.NickName;
                    device.IsOnline = deviceInfo.IsOnline;
                    device.TemperatureType = deviceInfo.TemperatureType;
                    device.Ssid = deviceInfo.Ssid;
                    device.LocationName = deviceInfo.LocationName;
                    device.LastUpdated = DateTime.Now;
                }

                var statusProperties = JsonSerializer.Serialize(deviceInfo.Identifiers ?? new List<DeviceIdentifier>());
                var deviceStatus = new DeviceStatus
                {
                    DeviceId = deviceInfo.DeviceId,
                    TimeStatusExtracted = DateTime.Now,
                    StatusProperties = statusProperties
                };
                dbContext.DeviceStatuses.Add(deviceStatus);
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            await LogExceptionToDatabaseAsync("SaveDevicesToDatabase", ex.ToString());
            throw;
        }
    }

    private async Task SaveCleaningToDatabaseAsync(string deviceId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TclHomeDbContext>();

            var cleaning = new Cleaning
            {
                DeviceId = deviceId,
                TimeCleaningStarted = DateTime.Now
            };
            dbContext.Cleanings.Add(cleaning);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            await LogExceptionToDatabaseAsync("SaveCleaningToDatabase", ex.ToString());
            throw;
        }
    }

    private async Task LogExceptionToDatabaseAsync(string name, string? value)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TclHomeDbContext>();

            var exceptionLog = new ExceptionLog
            {
                Name = name,
                Value = value,
                TimeOccurred = DateTime.Now
            };
            dbContext.Exceptions.Add(exceptionLog);
            await dbContext.SaveChangesAsync();
        }
        catch
        {
            // If we can't log to database, silently fail to avoid infinite loops
        }
    }

    public void Dispose()
    {
    }
}

public class CognitoCredentials
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
}

public class BypassSslHttpClientFactory : Amazon.Runtime.HttpClientFactory
{
    public override HttpClient CreateHttpClient(IClientConfig clientConfig)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
            CheckCertificateRevocationList = false
        };
        return new HttpClient(handler);
    }
}

