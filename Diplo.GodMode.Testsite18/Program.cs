using Microsoft.AspNetCore.Authentication.Cookies;
using Umbraco.Cms.Core;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args);
}

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .Build();

if (builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("DiploGodModeTestsite:AllowHttpBackOffice"))
{
    // Keep explicitly opted-in local acceptance independent of IIS Express and
    // development-certificate state. OpenIddict still requires HTTPS otherwise.
    builder.Services.Configure<OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreOptions>(options =>
        options.DisableTransportSecurityRequirement = true);
    builder.Services.PostConfigure<CookieAuthenticationOptions>(
        Constants.Security.BackOfficeAuthenticationType,
        options => options.Cookie.SecurePolicy = CookieSecurePolicy.None);
}

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
