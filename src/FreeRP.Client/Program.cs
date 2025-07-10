using FreeRP.Client;
using FreeRP.ClientCore;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var hc = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

#region FrpClientSettings

FrpClientSettings? clientSettings = null;
var res = await hc.GetAsync("frpSettings.json");
if (res.StatusCode == System.Net.HttpStatusCode.OK)
{
    var json = await res.Content.ReadAsStringAsync();
    if (FreeRP.Helpers.Json.GetModel<FrpClientSettings>(json) is FrpClientSettings s)
        clientSettings = s;
}

clientSettings ??= new();
builder.Services.AddSingleton(clientSettings);

#endregion

builder.Services.AddScoped(sp => hc);

builder.Services.AddScoped<FreeRP.Client.Data.LocalStorage>();
builder.Services.AddScoped<FreeRP.Localization.FrpLocalizationService>();
builder.Services.AddScoped<FreeRP.FrpServices.IFrpDataService, FrpDataService>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
