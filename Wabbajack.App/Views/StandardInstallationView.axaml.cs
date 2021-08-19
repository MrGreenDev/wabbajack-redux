using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Wabbajack.App.ViewModels;

namespace Wabbajack.App.Views
{
    public partial class StandardInstallationView : ScreenBase<StandardInstallationViewModel>
    {
        public StandardInstallationView()
        {
            InitializeComponent();
        }

    }
}