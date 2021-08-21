using System;
using Wabbajack.App.Interfaces;
using Wabbajack.App.ViewModels;

namespace Wabbajack.App.Views
{
    public abstract class ScreenBase<T> : ViewBase<T>, IScreenView
    where T : ViewModelBase
    {
        public Type ViewModelType => typeof(T);
    }
}