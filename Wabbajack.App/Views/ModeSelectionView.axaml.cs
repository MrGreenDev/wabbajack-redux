using System;
using System.Reactive.Disposables;
using ReactiveUI;
using Wabbajack.App.Messages;
using Wabbajack.App.ViewModels;

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
                    MessageBus.Instance.Send(new NavigateTo(typeof(InstallConfigurationViewModel)));
                }).DisposeWith(disposables);
            });
        }

    }
}