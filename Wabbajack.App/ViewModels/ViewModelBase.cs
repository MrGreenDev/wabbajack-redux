using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;
using ReactiveUI.Validation.Helpers;
using Wabbajack.Interfaces;

namespace Wabbajack.App.ViewModels
{
    public class ViewModelBase : ReactiveValidationObject, IActivatableViewModel, ISingletonService
    {
        public ViewModelActivator Activator { get; }

        public ViewModelBase()
        {
            Activator = new ViewModelActivator();
        }
    }
}
