using FreeRP.FrpServices;
using FreeRP.Server.Components;
using FreeRP.ServerCore;
using FreeRP.ServerCore.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;
using MudBlazor.Translations;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region FrpSettings

IFrpSettingsService settingsService = new FreeRP.ServerCore.Settings.FrpSettingsService();
FreeRP.Settings.FrpSettings frpSettings = await settingsService.GetSettingsAsync(builder.Environment.ContentRootPath);

builder.Services.AddSingleton(settingsService);
builder.Services.AddSingleton(frpSettings);

#endregion

#region ServiceCollection

builder.Services.AddGrpc(o =>
{
    o.MaxReceiveMessageSize = frpSettings.GrpcSettings.GrpcMessageSizeInByte;
});

builder.Services.AddSingleton<IFrpDataService, FrpDataService>();
builder.Services.AddScoped<FreeRP.Localization.FrpLocalizationService>();
builder.Services.AddScoped<IFrpAuthService, FrpAuthService>();
builder.Services.AddScoped<FreeRP.ServerCore.Mail.FrpMailService>();

//For admin site
builder.Services.AddScoped<FreeRP.Server.Data.AuthService>();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddMudTranslations();

#endregion

#region Auth

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthorization();
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(cfg =>
    {
        cfg.RequireHttpsMetadata = true;
        cfg.SaveToken = true;
        cfg.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(frpSettings.LoginSettings.TokenSigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

#endregion

#region Cors

builder.Services.AddCors(o => o.AddPolicy("AllowAll", builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader()
           .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding", "X-Grpc-Web", "User-Agent");
}));

#endregion

var app = builder.Build();

var ds = app.Services.GetRequiredService<IFrpDataService>();
if (ds is FrpDataService d)
    await d.InitAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();
//app.UseStaticFiles(new StaticFileOptions
//{
//    ServeUnknownFileTypes = true,
//    DefaultContentType = "application/octet-stream",
//    FileProvider = new PhysicalFileProvider(frpSettings.PublicRootPath),
//    RequestPath = "/public"
//});
app.UseAntiforgery();

app.UseGrpcWeb();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuthMiddleware>();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapGrpcService<FreeRP.ServerCore.Auth.GrpcAuthService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<FreeRP.ServerCore.Content.GrpcContentService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<FreeRP.ServerCore.Database.GrpcDatabaseAccessService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<FreeRP.ServerCore.Database.GrpcDatabaseService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<FreeRP.ServerCore.Log.GrpcLogService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<FreeRP.ServerCore.Permission.GrpcPermissionService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<FreeRP.ServerCore.Role.GrpcRoleService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<FreeRP.ServerCore.Settings.GrpcSettingsService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<FreeRP.ServerCore.User.GrpcUserService>().EnableGrpcWeb().RequireCors("AllowAll");
//app.MapFallbackToFile("/", "index.html");

app.Run();
