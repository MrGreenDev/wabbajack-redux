using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Wabbajack.App.ViewModels;

namespace Wabbajack.App.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>, IActivatableView
    {

        public MainWindow()
        {

            InitializeComponent();
            DataContext = App.Services.GetService<MainWindowViewModel>()!;
            
            this.WhenActivated(dispose =>
            {
                CloseButton.Command = ReactiveCommand.Create(() => Environment.Exit(0)).DisposeWith(dispose);
                MinimizeButton.Command = ReactiveCommand.Create(() => WindowState = WindowState.Minimized).DisposeWith(dispose);


                ViewModel.WhenAnyValue(vm => vm.CurrentScreen)
                    .Where(s => s != default)
                    .BindTo(Content, c => c.Content)
                    .DisposeWith(dispose);

            });
            

            Width = 1125;
            Height = 900;

#if DEBUG
            this.AttachDevTools();
#endif
        }

    }
}