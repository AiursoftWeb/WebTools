using System.Globalization;
using System.Text.RegularExpressions;
using Aiursoft.CSTools.Tools;
using Aiursoft.WebTools.Abstractions;
using Aiursoft.WebTools.Abstractions.Models;
using Aiursoft.WebTools.OfficialPlugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.WebTools;

public static partial class Extends
{
    // ReSharper disable once UseVerbatimString
    // ReSharper disable once StringLiteralTypo
    [GeneratedRegex(
        "(android|bb\\d+|meego).+mobile|avantgo|bada\\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\\.(browser|link)|vodafone|wap|windows ce|xda|xiino",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "en-US")]
    private static partial Regex MobileUa();

    // ReSharper disable once UseVerbatimString
    // ReSharper disable once StringLiteralTypo
    [GeneratedRegex(
        "1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\\-(n|u)|c55\\/|capi|ccwa|cdm\\-|cell|chtm|cldc|cmd\\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\\-s|devi|dica|dmob|do(c|p)o|ds(12|\\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\\-|_)|g1 u|g560|gene|gf\\-5|g\\-mo|go(\\.w|od)|gr(ad|un)|haie|hcit|hd\\-(m|p|t)|hei\\-|hi(pt|ta)|hp( i|ip)|hs\\-c|ht(c(\\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\\-(20|go|ma)|i230|iac( |\\-|\\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\\/)|klon|kpt |kwc\\-|kyo(c|k)|le(no|xi)|lg( g|\\/(k|l|u)|50|54|\\-[a-w])|libw|lynx|m1\\-w|m3ga|m50\\/|ma(te|ui|xo)|mc(01|21|ca)|m\\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\\-2|po(ck|rt|se)|prox|psio|pt\\-g|qa\\-a|qc(07|12|21|32|60|\\-[2-7]|i\\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\\-|oo|p\\-)|sdk\\/|se(c(\\-|0|1)|47|mc|nd|ri)|sgh\\-|shar|sie(\\-|m)|sk\\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\\-|v\\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\\-|tdg\\-|tel(i|m)|tim\\-|t\\-mo|to(pl|sh)|ts(70|m\\-|m3|m5)|tx\\-9|up(\\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\\-|your|zeto|zte\\-",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "en-US")]
    private static partial Regex MobileVersions();

    public static bool IsMobileBrowser(this HttpRequest request)
    {
        var userAgent = request.UserAgent();
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return false;
        }

        if (MobileUa().IsMatch(userAgent) || MobileVersions().IsMatch(userAgent.Substring(0, 4)))
        {
            return true;
        }

        return false;
    }

    public static bool IsWeChat(this HttpRequest request)
    {
        // ReSharper disable once StringLiteralTypo
        return request.UserAgent()?.ToLower().Contains("micromessenger") ?? false;
    }

    public static bool IsIos(this HttpRequest request)
    {
        return request.UserAgent()?.ToLower().Contains("iphone") ?? false;
    }

    public static bool IsAndroid(this HttpRequest request)
    {
        return request.UserAgent()?.ToLower().Contains("android") ?? false;
    }

    public static string? UserAgent(this HttpRequest request)
    {
        return request.Headers["User-Agent"];
    }

    public static bool AllowTrack(this HttpContext httpContext)
    {
        return httpContext.Request.Headers.TryGetValue("dnt", out var dntFlag) && dntFlag.ToString().Trim() != "1";
    }

    public static void SetClientLang(this ControllerBase controller, string culture)
    {
        controller.HttpContext.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });
    }

    public static async Task<WebApplication> AppAsync<T>(
        string[] args,
        int port = -1,
        List<IWebAppPlugin>? plugins = null) where T : IWebStartup, new()
    {
        plugins ??=
        [
            new DockerPlugin(),
            new MaxBodySizePlugin(),
            new KevlarPlugin(),
            new HandleRobotsPlugin(),
            new DataProtectionPlugin(),
            new ResponseCompressionPlugin(),
            new LocalizationPlugin(),
            new AlwaysAddMemoryCachePlugin(),

            // In docker, we trust any proxy.
            // This is because usually when deployed in docker, we will use a reverse proxy, like Caddy.
            // Caddy will drop all requests' header: X-Forwarded-For and attach real IP. So we trust it.
            new SupportForwardHeadersPlugin(trustAnyProxy: EntryExtends.IsInDocker())
        ];
        var builder = WebApplication.CreateBuilder(args);
        if (port > 0)
        {
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        }

        foreach (var plugin in plugins.Where(plugin => plugin.ShouldAddThisPlugin()))
        {
            await plugin.PreServiceConfigure(builder);
        }

        var startup = new T();
        startup.ConfigureServices(builder.Configuration, builder.Environment, builder.Services);
        foreach (var plugin in plugins.Where(plugin => plugin.ShouldAddThisPlugin()))
        {
            await plugin.PostServiceConfigure(builder);
        }
        var app = builder.Build();
        foreach (var plugin in plugins.Where(plugin => plugin.ShouldAddThisPlugin()))
        {
            await plugin.AppConfiguration(app);
        }
        startup.Configure(app);
        return app;
    }

    public static string ToHtmlDateTime(this DateTime source)
    {
        return source.ToString(CultureInfo.InvariantCulture);
    }
}
