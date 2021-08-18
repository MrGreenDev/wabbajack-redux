using System;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using Wabbajack.App.ViewModels;

namespace Wabbajack.App.Views
{
    public partial class MainWindow : Window, IActivatableView
    {

        public MainWindow()
        {

            InitializeComponent();
            
            this.WhenActivated(dispose =>
            {
                CloseButton.Command = ReactiveCommand.Create(() => Environment.Exit(0)).DisposeWith(dispose);
                MinimizeButton.Command = ReactiveCommand.Create(() => WindowState = WindowState.Minimized).DisposeWith(dispose);

            });
            

            Width = 1125;
            Height = 900;

#if DEBUG
            this.AttachDevTools();
#endif
        }

    }
}