using System;
using Microsoft.Extensions.DependencyInjection;
using Wabbajack.App.Controls;
using Wabbajack.App.Interfaces;
using Wabbajack.App.Messages;
using Wabbajack.App.Models;
using Wabbajack.App.ViewModels;
using Wabbajack.App.Views;
using Wabbajack.DTOs;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Networking.NexusApi;
using Wabbajack.Services.OSIntegrated;

namespace Wabbajack.App
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddSingleton(new ApplicationInfo
            {
                AppName = "Wabbajack",
                AppVersion = new Version(1, 0)
            });


            services.AddSingleton<MessageBus>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<InstallConfigurationViewModel>();
            services.AddDTOConverters();
            services.AddDTOSerializer();
            services.AddSingleton<ModeSelectionViewModel>();
            services.AddTransient<FileSelectionBoxViewModel>();
            services.AddSingleton<IScreenView, ModeSelectionView>();
            services.AddSingleton<IScreenView, InstallConfigurationView>();
            services.AddSingleton<IScreenView, StandardInstallationView>();
            services.AddSingleton<InstallationStateManager>();

            services.AddAllSingleton<IReceiverMarker, StandardInstallationViewModel>();
            services.AddAllSingleton<IReceiverMarker, MainWindowViewModel>();
            
            services.AddOSIntegrated();
            return services;
        }
    }
}