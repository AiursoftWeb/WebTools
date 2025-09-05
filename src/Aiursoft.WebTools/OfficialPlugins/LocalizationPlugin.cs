using System.Globalization;
using Aiursoft.WebTools.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Primitives;

namespace Aiursoft.WebTools.OfficialPlugins;

public class LocalizationPlugin : IWebAppPlugin
{
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly Dictionary<string, string> SupportedCultures = new()
    {
        { "en-US", "English (United States)" },
        { "en-GB", "English (United Kingdom)" },
        { "zh-CN", "中文 (中国大陆)" },
        { "zh-TW", "中文 (台灣)" },
        { "zh-HK", "中文 (香港)" },
        { "ja-JP", "日本語 (日本)" },
        { "ko-KR", "한국어 (대한민국)" },
        { "vi-VN", "Tiếng Việt (Việt Nam)" },
        { "th-TH", "ภาษาไทย (ประเทศไทย)" },
        { "de-DE", "Deutsch (Deutschland)" },
        { "fr-FR", "Français (France)" },
        { "es-ES", "Español (España)" },
        { "ru-RU", "Русский (Россия)" },
        { "it-IT", "Italiano (Italia)" },
        { "pt-PT", "Português (Portugal)" },
        { "pt-BR", "Português (Brasil)" },
        { "ar-SA", "العربية (المملكة العربية السعودية)" },
        { "nl-NL", "Nederlands (Nederland)" },
        { "sv-SE", "Svenska (Sverige)" },
        { "pl-PL", "Polski (Polska)" },
        { "tr-TR", "Türkçe (Türkiye)" },
        { "ro-RO", "Română (România)" },
        { "da-DK", "Dansk (Danmark)" },
        { "uk-UA", "Українська (Україна)" },
        { "id-ID", "Bahasa Indonesia (Indonesia)" },
        { "fi-FI", "Suomi (Suomi)" },
        { "hi-IN", "हिन्दी (भारत)" },
        { "el-GR", "Ελληνικά (Ελλάδα)" }
    };

    public bool ShouldAddThisPlugin()
    {
        return true;
    }

    public Task PreServiceConfigure(WebApplicationBuilder builder)
    {
        return Task.CompletedTask;
    }

    public Task PostServiceConfigure(WebApplicationBuilder builder)
    {
        return Task.CompletedTask;
    }

    public Task AppConfiguration(WebApplication builder)
    {
        var supportedCultures = SupportedCultures.Select(c => new CultureInfo(c.Key)).ToList();
        var defaultLanguage = supportedCultures.First();

        var localizationOptions = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(defaultLanguage),
            SupportedCultures      = supportedCultures,
            SupportedUICultures    = supportedCultures,
            FallBackToParentCultures    = true,
            FallBackToParentUICultures  = true,
            RequestCultureProviders =
            [
                new CookieRequestCultureProvider(),
                new StartsWithAcceptLanguageProvider()
            ]
        };

        builder.UseRequestLocalization(localizationOptions);
        Console.WriteLine("Localization plugin has been added. Supported languages: " +
                          string.Join(", ", SupportedCultures.Values));
        return Task.CompletedTask;
    }
}

public class StartsWithAcceptLanguageProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (httpContext == null)
            throw new ArgumentNullException(nameof(httpContext));

        var acceptLangHeader = httpContext.Request.Headers["Accept-Language"];
        if (StringValues.IsNullOrEmpty(acceptLangHeader))
        {
            return Task.FromResult<ProviderCultureResult?>(null);
        }

        var languages = acceptLangHeader
            .ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var langWithQ in languages)
        {
            var pureLang = langWithQ.Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            if (string.IsNullOrEmpty(pureLang))
                continue;

            foreach (var result in from supported in LocalizationPlugin.SupportedCultures.Keys
                     where supported.StartsWith(pureLang, StringComparison.OrdinalIgnoreCase)
                     select new ProviderCultureResult(supported, supported))
            {
                return Task.FromResult<ProviderCultureResult?>(result);
            }
        }

        return Task.FromResult<ProviderCultureResult?>(null);
    }
}
