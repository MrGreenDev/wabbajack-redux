using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Wabbajack.App.Interfaces;
using Wabbajack.Common;

namespace Wabbajack.App.ViewModels
{
    public class RouterViewModel : ReactiveObject
    {
        public readonly Dictionary<Type, Control> _screens = new();

        private readonly Stack<Type> _breadCrumbs = new();

        [Reactive]
        public Control CurrentScreen { get; set; }
        
        [Reactive]
        public Type CurrentType { get; set; }
        
        public RouterViewModel(IEnumerable<IScreenView> screens)
        {
            _screens = screens.ToDictionary(s => s.ViewModelType, s => (Control)s);
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

        public void NavigateTo<TVM, TParam>(TParam param)
        {
            var vm = _screens[typeof(TVM)];
            NavigateTo<TVM>();
            ((INavigationParameter<TParam>)vm).NavigatedTo(param).FireAndForget();
        }

        public void Back()
        {
            var prev = _breadCrumbs.Pop();
            CurrentScreen = _screens[prev];
            CurrentType = prev;
        }
    }
}