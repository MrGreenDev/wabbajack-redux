using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Wabbajack.App.Interfaces;
using Wabbajack.App.ViewModels;

namespace Wabbajack.App.Views
{
    public partial class ModeSelectionView : ReactiveUserControl<ModeSelectionViewModel>, IScreenView
    {
        public ModeSelectionView()
        {
            ViewModel = App.Services.GetService<ModeSelectionViewModel>()!;
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                
                Install.Button.Command = ReactiveCommand.Create(() =>
                {
                    App.Services.GetService<RouterViewModel>()!.NavigateTo<InstallConfigurationViewModel>();
                });

            });


        }
        

        public Type ViewModelType => typeof(ModeSelectionView);
    }
}