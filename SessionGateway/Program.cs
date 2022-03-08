using BackMeUp.SessionGateway;
using System;
using System.Net;

var builder = WebApplication.CreateBuilder(args);


AuthenticationConfiguration? config = new AuthenticationConfiguration
(
    builder.Configuration["Authentication:TenantId"],
    builder.Configuration["Authentication:ClientId"],
    builder.Configuration["Authentication:ClientSecret"],
    builder.Configuration["Authentication:RedirectEndpoint"],
    scope: builder.Configuration["Authentication:Scope"],
    redirectUri: builder.Configuration["Authentication:RedirectUri"]
);

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<TokenStorage>();
builder.Services.AddTransient<AuthenticationService>();
builder.Services.AddHttpClient();
builder.WebHost.UseUrls(builder.Configuration["Url"]);

var app = builder.Build();

app.MapGet("/", (AuthenticationService authenticationService) =>
{
    return Results.Text(
        "Graph API OAuth2 Session Gateway. \n" +
        $"Token valid: {authenticationService.IsValidToken()}"
    );
});

app.MapGet("/authorize", () =>
{
    try
    {
        return Results.Redirect(config.RequestEndpoint);
    }
    catch (Exception)
    {
        return Results.StatusCode(500);
    }
});

app.MapGet("/access-token", async (AuthenticationService authenticationService) =>
{
    try
    {
        return Results.Ok(await authenticationService.GetAccessToken());
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine(ex.Message);
        if (ex.StatusCode != HttpStatusCode.BadRequest)
        {
            return Results.StatusCode((int)ex.StatusCode.Value);
        }

        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.StatusCode(500);
    }
});

app.MapGet(config.RedirectEndpoint, async (string code, string state, AuthenticationService authenticationService) =>
{
    await authenticationService.FetchAcessTokenFromCode(code, state);
    return Results.Redirect("/");
}).WithDisplayName("Redirect");

app.Run();