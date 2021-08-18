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
    public partial class InstallConfigurationView : ReactiveUserControl<InstallConfigurationViewModel>
    {
        public InstallConfigurationView()
        {
            InitializeComponent();
            DataContext = App.Services.GetService<InstallConfigurationViewModel>()!;

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(x => x.ModListFile.SelectedPath)
                    .Where(x => x != default)
                    .BindTo(ViewModel, vm => vm!.ModListPath)
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

    }
}