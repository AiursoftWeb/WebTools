# Aiursoft WebTools

<a name="aiursoft-webtools"></a>

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/AiursoftWeb/webtools/blob/master/LICENSE)
[![Pipeline stat](https://badge-proxy.aiursoft.com/aiursoft/webtools/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/aiursoft/webtools/-/pipelines)
[![Test Coverage](https://badge-proxy.aiursoft.com/aiursoft/webtools/badges/master/coverage.svg)](https://gitlab.aiursoft.com/aiursoft/webtools/-/pipelines)
[![S360 compliance](https://s360.aiursoft.com/badge.svg?project=aiursoft%2Fwebtools)](https://s360.aiursoft.com/SupplyChain?tab=projects&project=aiursoft%2Fwebtools&scope=baseline&ecosystem=all)
[![NuGet version (Aiursoft.WebTools)](https://img.shields.io/nuget/v/Aiursoft.WebTools.svg)](https://www.nuget.org/packages/Aiursoft.WebTools/)
[![Man hours](https://manhours.aiursoft.com/r/github.com/aiursoftweb/WebTools.svg)](https://manhours.aiursoft.com/r/github.com/aiursoftweb/WebTools.html)

A collection of tools for web development.

## Overview

### What is WebTools?

<a name="features"></a>

* Easier application startup
* QRCode generation
* HttpContext extensions

## Installation

Add the package to your .NET project:

<a name="how-to-install"></a>

To install `Aiursoft.WebTools` to your project from [nuget.org](https://www.nuget.org/packages/Aiursoft.WebTools/):

```bash
dotnet add package Aiursoft.WebTools
```

Requires .NET 10 or later.

## Usage

<a name="easier-application-startup"></a>

It is a common practice to create a `Program` class and a `Startup` class in a ASP.NET Core application. However, it is a little bit annoying to write the same code again and again. So we created a `Extends` class to help you to write less code.

```csharp
using System.Reflection;
using Aiursoft.WebTools;
using Aiursoft.WebTools.Abstractions.Models;

namespace DemoApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var app = await Extends.AppAsync<Startup>(args);
        await app.RunAsync();
    }
}

public class Startup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        services
            .AddControllers()
            .AddApplicationPart(Assembly.GetExecutingAssembly());
    }

    public void Configure(WebApplication app)
    {
        app.UseRouting();
        app.MapDefaultControllerRoute();
    }
}
```

### QRCode generation

<a name="qrcode-generation"></a>

We provide a QRCode generation service for you to generate QRCode image from a string.

```csharp
var base64 = _qrCodeService.ToQRCodeBase64(somestring);
```

### HttpContext extensions

<a name="httpcontext-extensions"></a>

We provide some useful extensions for `HttpContext`:

```csharp
var isWeChat = HttpContext.Request.IsWeChat();
var isMobile = HttpContext.Request.IsMobileBrowser();
var allowTrack = HttpContext.AllowTrack();
```

## Configuration

**Optional application-wide rate limiting**

<a name="optional-application-wide-rate-limiting"></a>

Register the optional middleware in your startup and explicitly enable it in configuration.
It is disabled by default; existing `LimitPerMin` attributes keep their per-path behavior.

```csharp
// ConfigureServices:
services.AddMacroRateLimiting(options => configuration.GetSection("MacroRateLimit").Bind(options));

// Configure: after forwarded headers and static files, before routing, authentication and controllers.
app.UseStaticFiles();
app.UseMacroRateLimiting();
app.UseRouting();
```

```json
{
"MacroRateLimit": {
  "Enabled": true,
  "PermitLimit": 300,
  "WindowSeconds": 60,
  "PerIpConcurrencyLimit": 4,
  "ConcurrencyLimit": 32
}
}
```

Each client IP shares its request budget across all URLs reaching the middleware. Separate limits
cap simultaneous requests per IP and per application process; excess requests receive `429` with
`Retry-After`, without queuing or executing controllers. It uses the validated `RemoteIpAddress`;
configure trusted proxies appropriately. Limits are per process, not shared between replicas or
applications. Configuration changes take effect after restarting the application.

## Development

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

From the repository root, restore, build and test:

```bash
dotnet restore Aiursoft.WebTools.sln
dotnet build Aiursoft.WebTools.sln --no-restore
dotnet test Aiursoft.WebTools.sln --no-restore
```

Run `./lint.sh` for the existing static checks.

## How to contribute

<a name="how-to-contribute"></a>

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you have push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your workflow cruft out of sight.

We're also interested in your feedback on the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.

## License

Licensed under the [MIT License](https://github.com/AiursoftWeb/webtools/blob/master/LICENSE).
