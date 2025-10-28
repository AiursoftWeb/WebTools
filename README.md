# Aiursoft WebTools

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.com/aiursoft/webtools/-/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.com/aiursoft/webtools/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/aiursoft/webtools/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.com/aiursoft/webtools/badges/master/coverage.svg)](https://gitlab.aiursoft.com/aiursoft/webtools/-/pipelines)
[![NuGet version (Aiursoft.WebTools)](https://img.shields.io/nuget/v/Aiursoft.WebTools.svg)](https://www.nuget.org/packages/Aiursoft.WebTools/)
[![ManHours](https://manhours.aiursoft.com/r/gitlab.aiursoft.com/aiursoft/WebTools.svg)](https://gitlab.aiursoft.com/aiursoft/WebTools/-/commits/master?ref_type=heads)

A collection of tools for web development.

## How to install

To install `Aiursoft.WebTools` to your project from [nuget.org](https://www.nuget.org/packages/Aiursoft.WebTools/):

```bash
dotnet add package Aiursoft.WebTools
```

## Features

* Easier application startup
* QRCode generation
* HttpContext extensions

## Easier application startup

It is a common practice to create a `Program` class and a `Startup` class in a ASP.NET Core application. However, it is a little bit annoying to write the same code again and again. So we created a `Extends` class to help you to write less code.

```csharp
using System.Reflection;
using Aiursoft.WebTools.Models;

namespace DemoApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var app = Extends.App<Startup>(args);
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

## QRCode generation

We provide a QRCode generation service for you to generate QRCode image from a string.

```csharp
var base64 = _qrCodeService.ToQRCodeBase64(somestring);
```

## HttpContext extensions

We provide some useful extensions for `HttpContext`:

```csharp
var isWeChat = HttpContext.IsWeChat();
var isMobile = HttpContext.IsMobileBrowser();
var allowTrack = HttpContext.AllowTrack();
```

## How to contribute

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you with push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your workflow cruft out of sight.

We're also interested in your feedback on the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.
