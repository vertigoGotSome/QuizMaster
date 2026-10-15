using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Features.Game;
using QuizMaster.Api.Features.Jeopardy;
using QuizMaster.Api.Features.Storage;
using QuizMaster.Data;

var builder = WebApplication.CreateBuilder(args);
var publicOrigin = builder.Configuration["Hosting:PublicOrigin"]?.TrimEnd('/');

builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
);
builder.Services.AddSignalR();
builder.Services.AddAuthorization();

// Tailscale Serve/Funnel terminates public HTTPS before proxying to the local
// HTTP listener. Trust its forwarded scheme and host for OAuth redirect URLs.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "Discord";
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "quizmaster.sid";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        // The local Angular development proxy uses HTTP, while Discord OAuth
        // starts and completes on the HTTPS backend. Only the application
        // session cookie is permitted over HTTP in Development.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.None
            : CookieSecurePolicy.Always;

        options.LoginPath = "/api/auth/login";
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect("/");
            return Task.CompletedTask;
        };
    })
    .AddOAuth(
        "Discord",
        options =>
        {
            options.ClientId = builder.Configuration["Discord:ClientId"] ?? "";
            options.ClientSecret = builder.Configuration["Discord:ClientSecret"] ?? "";
            options.CallbackPath =
                builder.Configuration["Discord:CallbackPath"] ?? "/signin-discord";
            options.AuthorizationEndpoint = "https://discord.com/oauth2/authorize";
            options.TokenEndpoint = "https://discord.com/api/oauth2/token";
            options.UserInformationEndpoint = "https://discord.com/api/users/@me";
            options.Scope.Add("identify");
            options.SaveTokens = false;
            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "global_name");

            options.Events = new OAuthEvents
            {
                // A reverse proxy (for example Tailscale Serve) terminates TLS before
                // forwarding to Kestrel. Configure the public URL once so Discord
                // always receives its registered HTTPS callback, not localhost.
                OnRedirectToAuthorizationEndpoint = context =>
                {
                    if (IsHttpsOrigin(publicOrigin))
                    {
                        var authorizationUri = new Uri(context.RedirectUri);
                        var query = QueryHelpers
                            .ParseQuery(authorizationUri.Query)
                            .ToDictionary(pair => pair.Key, pair => (string?)pair.Value.ToString());
                        query["redirect_uri"] = $"{publicOrigin}{options.CallbackPath}";
                        context.Response.Redirect(
                            QueryHelpers.AddQueryString(
                                authorizationUri.GetLeftPart(UriPartial.Path),
                                query
                            )
                        );
                        return Task.CompletedTask;
                    }
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                },
                OnCreatingTicket = async context =>
                {
                    using var request = new HttpRequestMessage(
                        HttpMethod.Get,
                        context.Options.UserInformationEndpoint
                    );
                    request.Headers.Authorization = new AuthenticationHeaderValue(
                        "Bearer",
                        context.AccessToken
                    );

                    using var response = await context.Backchannel.SendAsync(
                        request,
                        context.HttpContext.RequestAborted
                    );
                    response.EnsureSuccessStatusCode();

                    using var document = JsonDocument.Parse(
                        await response.Content.ReadAsStringAsync()
                    );
                    var discordUser = document.RootElement;
                    var discordId = discordUser.GetProperty("id").GetString()!;
                    var displayName =
                        discordUser.TryGetProperty("global_name", out var globalName)
                        && globalName.ValueKind == JsonValueKind.String
                            ? globalName.GetString()
                            : discordUser.GetProperty("username").GetString();
                    var avatar =
                        discordUser.TryGetProperty("avatar", out var avatarHash)
                        && avatarHash.ValueKind == JsonValueKind.String
                            ? $"https://cdn.discordapp.com/avatars/{discordId}/{avatarHash.GetString()}.png"
                            : "";

                    var services = context.HttpContext.RequestServices;
                    var database = services.GetRequiredService<GameDbContext>();
                    var player = await database.Players.SingleOrDefaultAsync(item =>
                        item.DiscordId == discordId
                    );

                    if (player is null)
                    {
                        player = new Player { DiscordId = discordId };
                        database.Players.Add(player);
                    }

                    player.Name = displayName ?? "Player";
                    player.AvatarUrl = avatar;
                    player.IsHost =
                        discordId
                        == services.GetRequiredService<IConfiguration>()["Discord:HostUserId"];
                    player.LastSeenUtc = DateTime.UtcNow;
                    await database.SaveChangesAsync();

                    context.RunClaimActions(discordUser);
                    context.Identity!.AddClaim(new Claim("player_id", player.Id.ToString()));
                    context.Identity.AddClaim(new Claim("avatar", avatar));
                    context.Identity.AddClaim(new Claim("host", player.IsHost ? "true" : "false"));
                    context.Identity.AddClaim(new Claim(ClaimTypes.Name, player.Name));
                },
            };
        }
    );

var app = builder.Build();
app.UseForwardedHeaders();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapGameController();
app.MapJeopardyController();
app.MapStorageController();
app.MapFallbackToFile("index.html");
app.Run();

static bool IsHttpsOrigin(string? value) =>
    Uri.TryCreate(value, UriKind.Absolute, out var uri)
    && uri.Scheme == Uri.UriSchemeHttps
    && string.IsNullOrEmpty(uri.Query)
    && string.IsNullOrEmpty(uri.Fragment);
