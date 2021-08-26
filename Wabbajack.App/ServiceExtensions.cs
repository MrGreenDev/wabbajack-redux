using System;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CefNet;
using Microsoft.Extensions.DependencyInjection;
using Wabbajack.App.Controls;
using Wabbajack.App.Interfaces;
using Wabbajack.App.Messages;
using Wabbajack.App.Models;
using Wabbajack.App.Utilities;
using Wabbajack.App.ViewModels;
using Wabbajack.App.Views;
using Wabbajack.DTOs;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Networking.NexusApi;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Wabbajack.Services.OSIntegrated;

namespace Wabbajack.App
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
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
            services.AddSingleton<IScreenView, SettingsView>();
            services.AddSingleton<InstallationStateManager>();
            services.AddSingleton<HttpClient>();
            
            services.AddAllSingleton<IReceiverMarker, StandardInstallationViewModel>();
            services.AddAllSingleton<IReceiverMarker, MainWindowViewModel>();
            services.AddAllSingleton<IReceiverMarker, SettingsViewModel>();
            services.AddAllSingleton<IReceiverMarker, NexusLoginViewModel>();
            services.AddAllSingleton<IReceiverMarker, LoversLabOAuthLoginViewModel>();

            var resources = KnownFolders.EntryPoint.Combine("Library/libcef/windows-x64");
            services.AddSingleton(s => new CefSettings()
            {
                NoSandbox = true,
                PersistSessionCookies = true,
                MultiThreadedMessageLoop = false,
                WindowlessRenderingEnabled = true,
                ExternalMessagePump = true,
                LocalesDirPath = resources.Combine("locales").ToString(),
                ResourcesDirPath = resources.ToString(),
                UserAgent = "",
                CachePath = KnownFolders.WabbajackAppLocal.Combine("cef_cache").ToString()
            });
            
            

            services.AddSingleton(s =>
            {
                App.FrameworkInitialized += App_FrameworkInitialized;
                
                var app = new CefAppImpl();
                app.ScheduleMessagePumpWorkCallback = OnScheduleMessagePumpWork;

                app.CefProcessMessageReceived += App_CefProcessMessageReceived;
                app.Initialize(resources.ToString(), s.GetService<CefSettings>());


                return app;
            });

            services.AddOSIntegrated();
            return services;
        }

        private static async void OnScheduleMessagePumpWork(long delayMs)
        {
            await Task.Delay((int)delayMs);
            Dispatcher.UIThread.Post(CefApi.DoMessageLoopWork);
        }

        private static void App_CefProcessMessageReceived(object? sender, CefProcessMessageReceivedEventArgs e)
        {
            var msg = e.Name;

        }


        private static CefAppImpl app;
        private static Timer messagePump;
        private const int messagePumpDelay = 10;
        private static void App_FrameworkInitialized(object? sender, EventArgs e)
        {
            if (CefNetApplication.Instance.UsesExternalMessageLoop)
            {
                messagePump = new Timer(_ => Dispatcher.UIThread.Post(CefApi.DoMessageLoopWork), null, messagePumpDelay, messagePumpDelay);
            }
        }
    }
}