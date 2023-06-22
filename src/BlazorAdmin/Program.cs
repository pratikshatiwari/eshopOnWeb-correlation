using System;
using System.Configuration;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using BlazorAdmin;
using BlazorAdmin.Services;
using Blazored.LocalStorage;
using BlazorShared;
using BlazorShared.Models;
using Elastic.Apm.Config;
using Elastic.Apm.SerilogEnricher;
using Elastic.CommonSchema.Serilog;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;

var builder = WebAssemblyHostBuilder.CreateDefault(args);


var Configuration = builder.Configuration;



builder.RootComponents.Add<App>("#admin");
builder.RootComponents.Add<HeadOutlet>("head::after");



var configSection = builder.Configuration.GetRequiredSection(BaseUrlConfiguration.CONFIG_NAME);
builder.Services.Configure<BaseUrlConfiguration>(configSection);

builder.Services.AddScoped(sp => new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<HttpService>();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped(sp => (CustomAuthStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

builder.Services.AddBlazorServices();

builder.Logging.AddConfiguration(builder.Configuration.GetRequiredSection("Logging"));

await ClearLocalStorageCache(builder.Services);

await builder.Build().RunAsync();

static async Task ClearLocalStorageCache(IServiceCollection services)
{
    var sp = services.BuildServiceProvider();
    var localStorageService = sp.GetRequiredService<ILocalStorageService>();

    await localStorageService.RemoveItemAsync(typeof(CatalogBrand).Name);
    await localStorageService.RemoveItemAsync(typeof(CatalogType).Name);
}


void ConfigureLogging()
{
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile(
            $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json",
            optional: true)

        .Build();

    Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service.name", "clothing")
    .Enrich.WithElasticApmCorrelationInfo()
    .Enrich.WithExceptionDetails()
    .WriteTo.Debug()
    .WriteTo.Console()
    .WriteTo.Console(outputTemplate: "[{ElasticApmTraceId} {ElasticApmTransactionId} {Message:lj} {NewLine}{Exception}")
//.WriteTo.Console(outputTemplate: "[{TraceId} {TransactionId} {Message:lj} {NewLine}{Exception}")
   // .WriteTo.Elasticsearch(ConfigureElasticSink(configuration, environment))
    .WriteTo.File(new EcsTextFormatter(), "D:\\log\\web_270323_4.txt")
    .Enrich.WithProperty("Environment", environment)
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
}

ElasticsearchSinkOptions ConfigureElasticSink(IConfigurationRoot configuration, string environment)
{
    return new ElasticsearchSinkOptions(new Uri(configuration["ElasticConfiguration:Uri"]))
    {
        AutoRegisterTemplate = true,
        IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name.ToLower().Replace("log", "-")}-{environment?.ToLower().Replace("log", "-")}-{DateTime.UtcNow:yyyy-MM}",
        InlineFields = true
    };

}
