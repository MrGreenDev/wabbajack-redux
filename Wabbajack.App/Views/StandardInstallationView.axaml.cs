using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Wabbajack.App.ViewModels;
using Wabbajack.Interfaces;

namespace Wabbajack.App.Views
{
    public partial class StandardInstallationView : ScreenBase<StandardInstallationViewModel>, ISingletonService
    {
        public StandardInstallationView()
        {
            InitializeComponent();
        }

    }
}