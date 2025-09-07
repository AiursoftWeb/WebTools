using Aiursoft.WebTools.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.WebTools.OfficialPlugins;

/// <summary>
/// Represents the Kevlar plugin for adding additional security measures to the web application.
/// </summary>
/// <remarks>
/// This plugin configuration adds secure cookie settings and iframe policies to the web application.
/// </remarks>
/// <seealso cref="IWebAppPlugin" />
public class KevlarPlugin : IWebAppPlugin
{
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
        builder.Services.ConfigureApplicationCookie(options =>
        {
            // Can be: Unspecified, None, Lax, Strict
            // Unspecified: No SameSite field will be set, the client should follow its default cookie policy.
            //   This is the default value.
            // None: Indicates the client should disable same-site restrictions. This is the default value. But it's not secure.
            //   Hackers can use CSRF attacks to steal your cookies.
            // Lax: Indicates the client should send the cookie with "same-site" requests, and with "cross-site" top-level navigations.
            //   This means that the cookie will be sent with same-site requests, and with cross-site requests that are top-level navigations. Better than None.
            //   If an app need to integrate with OpenID Connect, then setting to Lax is recommended.
            // Strict: Indicates the client should only send the cookie with "same-site" requests.
            //   This is the most secure option which means only send the cookie with same-site requests. Suggested if the front-end and back-end are deployed under the same domain.
            options.Cookie.SameSite = SameSiteMode.Lax;
            // Can be: SameAsRequest, Always, None
            // SameAsRequest: If the URI that provides the cookie is HTTPS, then the cookie will only be returned to the server on.
            //   If the application is deployed under HTTPS, then the cookie will only be returned to the server on subsequent HTTPS requests.
            // Always: Secure is always marked true. Use this value when your login page and all subsequent pages requiring the authenticated identity are HTTPS.
            //   Local development will also need to be done with HTTPS urls. Not recommended.
            // None: Secure is not marked true. Use this value when your login page is HTTPS, but other pages on the site which are HTTP also require authentication information.
            //   This setting is not recommended because the authentication information provided with an HTTP request may be observed and used by other computers on your local network or wireless connection.
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            // If the 'HttpOnly' flag is used, the cookie cannot be accessed through client-side script.
            // It can still be attached to HTTP requests.
            options.Cookie.HttpOnly = true;
            // The 'Expiration' property is used to set the expiration time of the cookie.
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            // Sliding expiration is a concept that allows the expiration time of the cookie to be extended if the user interacts with the server.
            // This is very suggested for user login cookies. So user don't need to log in every period of time.
            options.SlidingExpiration = true;
        });
        Console.WriteLine("Secure cookies plugin has been added. SameSite policy: Strict, Secure policy: SameAsRequest, HttpOnly: true, Expiration: 30 days, SlidingExpiration: true.");
        return Task.CompletedTask;
    }

    public Task AppConfiguration(WebApplication builder)
    {
        // Apply the iframe policy to the application.
        builder.UseMiddleware<SecureIFramePolicyMiddleware>();
        Console.WriteLine("Secure iframe policy has been added. X-Frame-Options: SAMEORIGIN, Content-Security-Policy: frame-ancestors 'self'.");
        return Task.CompletedTask;
    }
}

public class SecureIFramePolicyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // This middleware will add the X-Frame-Options and Content-Security-Policy headers to the response.
        // SAMEORIGIN: The page can only be displayed in a frame on the same origin as the page itself.
        context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
        // frame-ancestors: The page can only be displayed in a frame on the same origin as the page itself.
        context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors 'self'");
        await next(context);
    }
}
