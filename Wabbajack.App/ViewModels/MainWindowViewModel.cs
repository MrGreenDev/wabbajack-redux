using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using Wabbajack.App.Interfaces;
using Wabbajack.App.Messages;

namespace Wabbajack.App.ViewModels
{
    public class MainWindowViewModel : ReactiveValidationObject, IActivatableViewModel, IReceiver<NavigateTo>
    {
        private readonly IEnumerable<IScreenView> _screens;

        [Reactive]
        public Control CurrentScreen { get; set; }
        
        [Reactive]
        public ReactiveCommand<Unit, Unit> BackButton { get; set; }
        
        public MainWindowViewModel(IEnumerable<IScreenView> screens)
        {
            _screens = screens;
            Activator = new ViewModelActivator();
            this.WhenActivated(disposables =>
            {
                BackButton = ReactiveCommand.Create(() => {}).DisposeWith(disposables);
            });
            
            Receive(new NavigateTo(typeof(ModeSelectionViewModel)));

        }
        public ViewModelActivator Activator { get; }
        public void Receive(NavigateTo val)
        {
            CurrentScreen = (Control)_screens.First(s => s.ViewModelType == val.ViewModel);
        }
    }
}
