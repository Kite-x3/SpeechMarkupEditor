using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SpeechMarkupEditor.Assets;
using SpeechMarkupEditor.Infrastructure.Configuration;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.Localization;

public class LocalizationService(IOptions<LocalizationSettings> settings, IHostEnvironment hostEnvironment) : ILocalizationService
{
    private static readonly IReadOnlyList<LanguageOption> AVAILABLE_LANGUAGES =
    [
        new() { Code = "ru", DisplayName = "Русский" },
        new() { Code = "en", DisplayName = "English" }
    ];

    private readonly string _settingsPath = Path.Combine(hostEnvironment.ContentRootPath, "appsettings.json");
    private readonly LocalizationSettings _settings = settings.Value;

    public string CurrentLanguageCode => NormalizeLanguageCode(_settings.Language);

    public IReadOnlyList<LanguageOption> GetAvailableLanguages()
    {
        return AVAILABLE_LANGUAGES;
    }

    public void ApplySavedCulture()
    {
        ApplyCulture(CurrentLanguageCode);
    }

    public async Task SetLanguageAsync(string languageCode)
    {
        string normalizedLanguageCode = NormalizeLanguageCode(languageCode);
        _settings.Language = normalizedLanguageCode;
        await SaveSettingsAsync(normalizedLanguageCode);
        ApplyCulture(normalizedLanguageCode);
    }

    private async Task SaveSettingsAsync(string languageCode)
    {
        JsonObject rootNode;

        try
        {
            string json = await File.ReadAllTextAsync(_settingsPath);
            rootNode = JsonNode.Parse(json)?.AsObject() ?? [];
        }
        catch
        {
            rootNode = [];
        }

        if (rootNode["LocalizationSettings"] is not JsonObject localizationNode)
        {
            localizationNode = new JsonObject();
            rootNode["LocalizationSettings"] = localizationNode;
        }

        localizationNode["Language"] = languageCode;

        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, rootNode, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static void ApplyCulture(string languageCode)
    {
        var culture = new CultureInfo(languageCode);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        Resources.Culture = culture;
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return "ru";

        string normalized = languageCode.Trim().ToLowerInvariant();
        return AVAILABLE_LANGUAGES.Any(item => item.Code == normalized) ? normalized : "ru";
    }
}
