using FreeRP.Net.Client;
using FreeRP.Net.Client.Blazor;
using FreeRP.Net.Client.Blazor.Data;
using FreeRP.Net.Client.Data;
using FreeRP.Net.Client.Translation;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<FrpEnvironment>();
builder.Services.AddFreeRP();
builder.Services.AddScoped<FreeRP.Net.Client.Blazor.Dialog.BusyDialogService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var env = scope.ServiceProvider.GetRequiredService<FrpEnvironment>();
    var i18n = scope.ServiceProvider.GetRequiredService<I18nService>();
    string code = await env.GetUserLanguageAsync();
    await i18n.LoadTextAsync(code);
}

await app.RunAsync();
