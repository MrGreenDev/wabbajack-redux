using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Wabbajack.App.Interfaces;
using Wabbajack.App.ViewModels;
using Wabbajack.Installer;

namespace Wabbajack.App.Views
{
    public partial class ModeSelectionView : ScreenBase<ModeSelectionViewModel>
    {


        public ModeSelectionView(IServiceProvider provider)
        {

            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                
                Install.Button.Command = ReactiveCommand.Create(() =>
                {
                    App.Services.GetService<RouterViewModel>()!.NavigateTo<InstallConfigurationViewModel>();
                });

            });
        }

    }
}