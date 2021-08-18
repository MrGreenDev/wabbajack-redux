using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Wabbajack.App.Interfaces;

namespace Wabbajack.App.ViewModels
{
    public class RouterViewModel : ReactiveObject
    {
        public Dictionary<Type, Control> _screens = new();
        
        [Reactive]
        public Control CurrentScreen { get; set; }
        
        public RouterViewModel(IEnumerable<IScreenView> screens)
        {
            _screens = screens.ToDictionary(s => s.ViewModelType, s => (Control)s);
            CurrentScreen = _screens.Values.First();
        }

        public void NavigateTo<T>()
        {
            CurrentScreen = _screens[typeof(T)];
        }
    }
}