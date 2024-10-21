using System.Reflection;
using Aiursoft.CSTools.Tools;
using Aiursoft.WebTools.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.WebTools.OfficialPlugins;

public class DataProtectionPlugin : IWebAppPlugin
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
        // By default, this plugin uses /data/Keys as the keys path.
        // To override the behavior, simply register the IDataProtectionProvider service in your app. (This plugin will not override it.)
        var inDocker = EntryExtends.IsInDocker();
        var keysPath = inDocker ? 
            Path.Combine("/data", ".aspnet", "DataProtection-Keys") :
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspnet", "DataProtection-Keys");
        if (!Directory.Exists(keysPath))
        {
            Directory.CreateDirectory(keysPath);
        }
        
        var applicationName = Assembly.GetEntryAssembly()!.GetName().Name!;
        Console.WriteLine($"Created directory: {keysPath} to persist data protection keys for application: {applicationName}.");
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName(applicationName);
        Console.WriteLine($"Data protection plugin has been added. Keys path: {keysPath}, Application name: {applicationName}.");
        return Task.CompletedTask;
    }

    public Task AppConfiguration(WebApplication builder)
    {
        return Task.CompletedTask;
    }
}