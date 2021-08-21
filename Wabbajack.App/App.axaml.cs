using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using Splat;
using Wabbajack.App.Controls;
using Wabbajack.App.Converters;
using Wabbajack.App.Interfaces;
using Wabbajack.App.Models;
using Wabbajack.App.ViewModels;
using Wabbajack.App.Views;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Networking.NexusApi;
using Wabbajack.Services.OSIntegrated;

namespace Wabbajack.App
{
    public class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;
        public static Window? MainWindow { get; set; }
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var host = Host.CreateDefaultBuilder(Array.Empty<string>())
                .ConfigureServices((host, services) =>
                {
                    services.AddAppServices();
                }).Build();
            Services = host.Services;

            SetupConverters();

            // Need to startup the message bus;
            Services.GetService<MessageBus>();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                MainWindow = desktop.MainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void SetupConverters()
        {
            Locator.CurrentMutable.RegisterConstant<IBindingTypeConverter>(new AbsoultePathBindingConverter());
        }
    }
}