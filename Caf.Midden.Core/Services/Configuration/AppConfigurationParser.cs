using System.Text.Json;
using AppConfiguration = Caf.Midden.Core.Models.v0_2.Configuration;

namespace Caf.Midden.Core.Services.Configuration;

public static class AppConfigurationVersions
{
    public const string Current = "v0.2";
    public const string Legacy = "v0.1";

    public static bool IsSupported(string? version) =>
        version is Current or Legacy;
}

public static class AppConfigurationParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static AppConfiguration Parse(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("schemaVersion", out var versionElement)
            || versionElement.ValueKind is not JsonValueKind.String
            || string.IsNullOrWhiteSpace(versionElement.GetString()))
        {
            throw new JsonException("The app configuration requires a 'schemaVersion'.");
        }

        var version = versionElement.GetString();

        if (!AppConfigurationVersions.IsSupported(version))
        {
            throw new JsonException(
                $"App configuration schema version '{version}' is not supported. "
                + $"Supported versions are '{AppConfigurationVersions.Current}' and '{AppConfigurationVersions.Legacy}'.");
        }

        return JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions)
            ?? throw new JsonException("The app configuration is empty.");
    }
}