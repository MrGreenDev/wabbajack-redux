using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using Wabbajack.App.Interfaces;
using Wabbajack.App.Messages;
using Wabbajack.App.Views;

namespace Wabbajack.App.ViewModels
{
    public class MainWindowViewModel : ReactiveValidationObject, IActivatableViewModel, IReceiver<NavigateTo>
    {
        private readonly IEnumerable<IScreenView> _screens;
        private readonly IServiceProvider _provider;

        [Reactive]
        public Control CurrentScreen { get; set; }
        
        [Reactive]
        private ImmutableStack<Control> BreadCrumbs { get; set; } = ImmutableStack<Control>.Empty; 

        [Reactive]
        public ReactiveCommand<Unit, Unit> BackButton { get; set; }
        
        public MainWindowViewModel(IEnumerable<IScreenView> screens, IServiceProvider provider)
        {
            _provider = provider;
            _screens = screens;
            
            Activator = new ViewModelActivator();
            this.WhenActivated(disposables =>
            {
                BackButton = ReactiveCommand.Create(() =>
                        {
                            CurrentScreen = BreadCrumbs.Peek();
                            BreadCrumbs = BreadCrumbs.Pop();
                        },
                        this.ObservableForProperty(vm => vm.BreadCrumbs)
                            .Select(bc => bc.Value.Count() > 1))
                    .DisposeWith(disposables);
            });
            
            Receive(new NavigateTo(typeof(ModeSelectionViewModel)));

        }
        public ViewModelActivator Activator { get; }
        public void Receive(NavigateTo val)
        {
            BreadCrumbs = BreadCrumbs.Push(CurrentScreen);

            if (val.ViewModel.IsAssignableTo(typeof(GuidedWebViewModel)))
            {
                CurrentScreen = new GuidedWebView() { ViewModel = (GuidedWebViewModel)_provider.GetService(val.ViewModel)! };
            }
            else
            {
                CurrentScreen = (Control)_screens.First(s => s.ViewModelType == val.ViewModel);
            }
        }
    }
}
