using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using Wabbajack.App.Interfaces;

namespace Wabbajack.App.ViewModels
{
    public class MainWindowViewModel : ReactiveValidationObject, IActivatableViewModel
    {
        private readonly RouterViewModel _router;

        [Reactive]
        public Control CurrentScreen { get; set; }
        public MainWindowViewModel(RouterViewModel router)
        {
            _router = router;
            Activator = new ViewModelActivator();
            this.WhenActivated(disposables =>
            {
                _router.WhenAnyValue(r => r.CurrentScreen)
                    .Where(c => c != default)
                    .BindTo(this, t => t.CurrentScreen)
                    .DisposeWith(disposables);
            });

        }

        public ViewModelActivator Activator { get; }
    }
}
