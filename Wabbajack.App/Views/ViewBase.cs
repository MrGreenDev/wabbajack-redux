using System;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Wabbajack.App.ViewModels;

namespace Wabbajack.App.Views
{
    public abstract class ViewBase<T> : ReactiveUserControl<T>
    where T : ViewModelBase
    {
        public ViewBase()
        {
            ViewModel = App.Services.GetService<T>();
            if (ViewModel == null)
                throw new Exception($"View model {typeof(T)} not found, did you forget to add it to DI?");
        }
    }
}