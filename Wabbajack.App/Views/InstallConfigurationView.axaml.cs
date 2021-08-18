using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
    public partial class InstallConfigurationView : ReactiveUserControl<InstallConfigurationViewModel>, IScreenView
    {
        public InstallConfigurationView()
        {
            InitializeComponent();
            DataContext = App.Services.GetService<InstallConfigurationViewModel>()!;

            this.WhenActivated(disposables =>
            {

                this.WhenAnyValue(x => x.ModListFile.SelectedPath)
                    .BindTo(ViewModel, vm => vm!.ModListPath)
                    .DisposeWith(disposables);
                
                this.WhenAnyValue(x => x.DownloadPath.SelectedPath)
                    .BindTo(ViewModel, vm => vm!.Download)
                    .DisposeWith(disposables);
                
                this.WhenAnyValue(x => x.InstallPath.SelectedPath)
                    .BindTo(ViewModel, vm => vm!.Install)
                    .DisposeWith(disposables);

                ViewModel.WhenAnyValue(x => x.BeginCommand)
                    .Where(x => x != default)
                    .BindTo(BeginInstall, x => x.Button.Command)
                    .DisposeWith(disposables);
                
                ViewModel.WhenAnyValue(x => x.ModList)
                    .Where(x => x != default)
                    .Select(x => x.Name)
                    .BindTo(ModListName, x => x.Text)
                    .DisposeWith(disposables);
                
                ViewModel.WhenAnyValue(x => x.ModListImage)
                    .Where(x => x != default)
                    .BindTo(ModListImage, x => x.Source)
                    .DisposeWith(disposables);
            });
        }

        public Type ViewModelType => typeof(InstallConfigurationViewModel);
    }
}