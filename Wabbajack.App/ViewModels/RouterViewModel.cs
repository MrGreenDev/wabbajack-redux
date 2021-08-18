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

        private Stack<Type> _breadCrumbs = new();
        private readonly Dictionary<Control,Type> _screensReversed;

        [Reactive]
        public Control CurrentScreen { get; set; }
        
        [Reactive]
        public Type CurrentType { get; set; }
        
        public RouterViewModel(IEnumerable<IScreenView> screens)
        {
            _screens = screens.ToDictionary(s => s.ViewModelType, s => (Control)s);
            _screensReversed = screens.ToDictionary(s => (Control)s, s => s.ViewModelType);
            var (key, value) = _screens.First();
            CurrentScreen = value;
            CurrentType = key;
        }

        public void NavigateTo<T>()
        {
            _breadCrumbs.Push(CurrentType);
            CurrentScreen = _screens[typeof(T)];
            CurrentType = typeof(T);
        }

        public void Back()
        {
            var prev = _breadCrumbs.Pop();
            CurrentScreen = _screens[prev];
            CurrentType = prev;
        }
    }
}