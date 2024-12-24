using System.Net.Mime;
using System.Reflection;
using Ardalis.ListStartupServices;
using BlazorAdmin;
using BlazorAdmin.Services;
using Blazored.LocalStorage;
using BlazorShared;
using Elastic.Apm.NetCoreAll;
using Elastic.Apm.SerilogEnricher;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.eShopWeb;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Infrastructure.Data;
using Microsoft.eShopWeb.Infrastructure.Identity;
using Microsoft.eShopWeb.Web;
using Microsoft.eShopWeb.Web.Configuration;
using Microsoft.eShopWeb.Web.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.AspNetCore;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Elastic.CommonSchema.Serilog;
using Serilog.Events;
using System.Transactions;
using System.Diagnostics;
using sys;

var builder = WebApplication.CreateBuilder(args);

var Configuration = builder.Configuration;

ConfigureLogging();
builder.Host.UseSerilog();


var ram1 = 123 



builder.Logging.AddConsole();

Microsoft.eShopWeb.Infrastructure.Dependencies.ConfigureServices(builder.Configuration, builder.Services);

builder.Services.AddCookieSettings();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
           .AddDefaultUI()
           .AddEntityFrameworkStores<AppIdentityDbContext>()
                           .AddDefaultTokenProviders();

builder.Services.AddScoped<ITokenClaimsService, IdentityTokenClaimService>();

builder.Services.AddCoreServices(builder.Configuration);
builder.Services.AddWebServices(builder.Configuration);

// Add memory cache services
builder.Services.AddMemoryCache();
builder.Services.AddRouting(options =>
{
    // Replace the type and the name used to refer to it with your own
    // IOutboundParameterTransformer implementation
    options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);
});

builder.Services.AddMvc(options =>
{
    options.Conventions.Add(new RouteTokenTransformerConvention(
             new SlugifyParameterTransformer()));

});
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Basket/Checkout");
});
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddHealthChecks()
    .AddCheck<ApiHealthCheck>("api_health_check", tags: new[] { "apiHealthCheck" })
    .AddCheck<HomePageHealthCheck>("home_page_health_check", tags: new[] { "homePageHealthCheck" });
builder.Services.Configure<ServiceConfig>(config =>
{
    config.Services = new List<ServiceDescriptor>(builder.Services);
    config.Path = "/allservices";
});

// blazor configuration
var configSection = builder.Configuration.GetRequiredSection(BaseUrlConfiguration.CONFIG_NAME);
builder.Services.Configure<BaseUrlConfiguration>(configSection);
var baseUrlConfig = configSection.Get<BaseUrlConfiguration>();

// Blazor Admin Required Services for Prerendering
builder.Services.AddScoped<HttpClient>(s => new HttpClient
{
    BaseAddress = new Uri(baseUrlConfig.WebBase)
});

// add blazor services
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<HttpService>();
builder.Services.AddBlazorServices();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();


var app = builder.Build();




//app.UseAllElasticApm(Configuration);



//var Configuration = GetConfiguration();
//app.UseAllElasticApm(Configuration);

//app.UseElasticApm(Configuration,
//    new HttpDiagnosticsSubscriber(),  /* Enable tracing of outgoing HTTP requests */
//    new EfCoreDiagnosticsSubscriber()); /* Enable tracing of database calls through EF Core*/

//app.UseAllElasticApm(Configuration);


app.Logger.LogInformation("App created...");

app.Logger.LogInformation("Seeding Database...");

app.UseAllElasticApm(Configuration);


// transaction 1
var trans1 = Agent.Tracer.StartTransaction("Dist Trans 1", ApiConstants.TypeRequest);
await trans1.CaptureSpan("step 1 processing", ApiConstants.ActionExec, async () => await Task.Delay(30));

using (var scope = app.Services.CreateScope())
{
    var scopedProvider = scope.ServiceProvider;
    //var trans1 = Agent.Tracer.StartTransaction("Dist Trans 1", ApiConstants.TypeRequest);

    try
    {
        var catalogContext = scopedProvider.GetRequiredService<CatalogContext>();
        //await trans1.CaptureSpan("step 1 processing", ApiConstants.ActionExec, async () => await Task.Delay(30));
        await CatalogContextSeed.SeedAsync(catalogContext, app.Logger);

        var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scopedProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var identityContext = scopedProvider.GetRequiredService<AppIdentityDbContext>();
        await AppIdentityDbContextSeed.SeedAsync(identityContext, userManager, roleManager);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred seeding the DB.");
    }
    trans1.End();
}



// transaction 2

var trans2 = Agent.Tracer.StartTransaction("Dist Trans 2", ApiConstants.TypeRequest,
    DistributedTracingData.TryDeserializeFromString(trans1.OutgoingDistributedTracingData.SerializeToString()));

//await trans2.CaptureSpan("step 2 processing", ApiConstants.ActionExec, async () => await Task.Delay(30));

await trans2.CaptureSpan("step 2 processing", ApiConstants.ActionExec, async () =>
{

    var catalogBaseUrl = builder.Configuration.GetValue(typeof(string), "CatalogBaseUrl") as string;

    if (!string.IsNullOrEmpty(catalogBaseUrl))

    {

        app.Use((context, next) =>
        {

            context.Request.PathBase = new PathString(catalogBaseUrl);

            return next();
        });
    }

    app.UseHealthChecks("/health",
        new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                var result = new
                {
                    status = report.Status.ToString(),
                    errors = report.Entries.Select(e => new
                    {
                        key = e.Key,
                        value = Enum.GetName(typeof(HealthStatus), e.Value.Status)
                    })
                }.ToJson();
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsync(result);
            }
        });
    if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
    {
        app.Logger.LogInformation("Adding Development middleware...");
        app.UseDeveloperExceptionPage();
        app.UseShowAllServicesMiddleware();
        app.UseMigrationsEndPoint();
        app.UseWebAssemblyDebugging();
    }
    else
    {
        app.Logger.LogInformation("Adding non-Development middleware...");
        app.UseExceptionHandler("/Error");
        app.UseHsts();

    }

});
trans2.End();





app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute("default", "{controller:slugify=Home}/{action:slugify=Index}/{id?}");
    endpoints.MapRazorPages();
    endpoints.MapHealthChecks("home_page_health_check", new HealthCheckOptions { Predicate = check => check.Tags.Contains("homePageHealthCheck") });
    endpoints.MapHealthChecks("api_health_check", new HealthCheckOptions { Predicate = check => check.Tags.Contains("apiHealthCheck") });
    //endpoints.MapBlazorHub("/admin");
    endpoints.MapFallbackToFile("index.html");
});



app.Logger.LogInformation("LAUNCHING");



app.Run();



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
        .WriteTo.Elasticsearch(ConfigureElasticSink(configuration, environment))
        .WriteTo.File(new EcsTextFormatter(), "D:\\log\\blazoradmin.txt")
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
