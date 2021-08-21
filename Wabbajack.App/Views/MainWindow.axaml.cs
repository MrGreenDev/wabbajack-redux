using System;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Wabbajack.App.ViewModels;

namespace Wabbajack.App.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {

        public MainWindow()
        {

            InitializeComponent();
            DataContext = App.Services.GetService<MainWindowViewModel>()!;
            
            this.WhenActivated(dispose =>
            {
                CloseButton.Command = ReactiveCommand.Create(() => Environment.Exit(0))
                    .DisposeWith(dispose);
                MinimizeButton.Command = ReactiveCommand.Create(() => WindowState = WindowState.Minimized)
                    .DisposeWith(dispose);
                
                this.BindCommand(ViewModel, vm => vm.BackButton, view => view.BackButton)
                    .DisposeWith(dispose);
                
                this.Bind(ViewModel, vm => vm.CurrentScreen, view => view.Contents.Content)
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