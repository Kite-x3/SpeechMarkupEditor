using System.Collections.Generic;
using System.Threading.Tasks;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Services.Localization;

public interface ILocalizationService
{
    string CurrentLanguageCode { get; }
    IReadOnlyList<LanguageOption> GetAvailableLanguages();
    void ApplySavedCulture();
    Task SetLanguageAsync(string languageCode);
}
