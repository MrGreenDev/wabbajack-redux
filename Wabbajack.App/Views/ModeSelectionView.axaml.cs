using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Wabbajack.App.Interfaces;
using Wabbajack.App.Messages;
using Wabbajack.App.ViewModels;
using Wabbajack.Installer;
using Wabbajack.Interfaces;

namespace Wabbajack.App.Views
{
    public partial class ModeSelectionView : ScreenBase<ModeSelectionViewModel>, ISingletonService
    {
        public ModeSelectionView(IServiceProvider provider)
        {

            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                Install.Button.Command = ReactiveCommand.Create(() =>
                {
                    MessageBus.Instance.Send(new NavigateTo(typeof(InstallConfigurationViewModel)));
                }).DisposeWith(disposables);
            });
        }

    }
}