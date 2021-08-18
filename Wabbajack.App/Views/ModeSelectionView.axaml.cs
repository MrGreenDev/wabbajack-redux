using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Wabbajack.App.Views
{
    public partial class ModeSelectionView : UserControl
    {
        public ModeSelectionView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}