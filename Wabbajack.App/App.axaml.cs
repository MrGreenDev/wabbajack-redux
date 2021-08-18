using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wabbajack.App.ViewModels;
using Wabbajack.App.Views;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Networking.NexusApi;

namespace Wabbajack.App
{
    public class App : Application
    {
        public static IServiceProvider Services { get; private set; }
        public static Window MainWindow { get; set; }
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var host = Host.CreateDefaultBuilder(Array.Empty<string>())
                .ConfigureServices((host, services) =>
                {
                    services.AddSingleton(new ApplicationInfo
                    {
                        AppName = "Wabbajack",
                        AppVersion = new Version(1, 0)
                    });
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<InstallConfigurationViewModel>();
                    services.AddDTOConverters();
                    services.AddDTOSerializer();

                }).Build();
            Services = host.Services;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                MainWindow = desktop.MainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}