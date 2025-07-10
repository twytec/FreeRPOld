using FreeRP.Net.Client.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add common services required by the FreeRP for Blazor library
    /// </summary>
    /// <param name="services">Service collection</param>
    public static IServiceCollection AddFreeRP(this IServiceCollection services)
    {
        services.AddScoped<Core.Translation.I18nService>();
        services.AddScoped<GrpcServices.ConnectService>();
        services.AddScoped<GrpcServices.PdfService>();
        services.AddScoped<GrpcServices.DatabaseService>();
        services.AddScoped<GrpcServices.AdminService>();
        services.AddScoped<GrpcServices.UserService>();
        services.AddFluentUIComponents();
        services.AddScoped<Dialog.IBusyDialogService, Dialog.BusyDialogService>();

        return services;
    }
}
