using Aiursoft.WebTools.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.WebTools.OfficialPlugins;

public class DockerPlugin : IWebAppPlugin
{
    public bool ShouldAddThisPlugin()
    {
        return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    }
    
    private static async Task<MemoryConfigurationSource> GetDockerSecrets()
    {
        if (!Directory.Exists("/run/secrets"))
        {
            return new MemoryConfigurationSource();
        }
        
        var source = new MemoryConfigurationSource();
        var secrets = new Dictionary<string, string>();
        var files = Directory.GetFiles("/run/secrets");
        foreach (var file in files)
        {
            var key = Path.GetFileName(file); 
            // Key might be: ConnectionStrings-Key. However, ASP.NET Core may expect it to be: ConnectionStrings:Key
            key = key.Replace('-', ':');
            var value = (await File.ReadAllTextAsync(file)).Trim();
            
            Console.WriteLine($"Secret: {key}={value}");
            secrets.Add(key, value);
        }
        source.InitialData = secrets!;
        return source;
    }

    public async Task PreConfigure(WebApplicationBuilder builder)
    {
        Console.WriteLine("Running in Docker. Loading secrets from /run/secrets.");
        builder.Configuration.Sources.Add(await GetDockerSecrets());
        builder.Services.AddHealthChecks();
    }

    public Task AppConfiguration(WebApplication builder)
    {
        builder.UseHealthChecks("/health");
        return Task.CompletedTask;
    }
}