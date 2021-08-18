using System;
using System.IO;
using System.IO.Compression;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using Wabbajack.App.Extensions;
using Wabbajack.App.Interfaces;
using Wabbajack.Common;
using Wabbajack.DTOs;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Installer;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;

namespace Wabbajack.App.ViewModels
{
    public class InstallConfigurationViewModel : ReactiveValidationObject, IActivatableViewModel
    {
        private readonly DTOSerializer _dtos;
        
        [Reactive]
        public AbsolutePath ModListPath { get; set; }
        
        [Reactive]
        public AbsolutePath Install { get; set; }
        
        [Reactive]
        public AbsolutePath Download { get; set; }
        
        [Reactive]
        public ModList? ModList { get; set; }
        
        [Reactive]
        public IBitmap? ModListImage { get; set; }
        
        [Reactive]
        public bool IsReady { get; set; }
        
        [Reactive]
        public ReactiveCommand<Unit, Unit> BeginCommand { get; set; }
        
        

        public InstallConfigurationViewModel(DTOSerializer dtos)
        {
            _dtos = dtos;
            Activator = new ViewModelActivator();
            this.WhenActivated(disposables =>
            {

                this.ValidationRule(x => x.ModListPath, p => p.FileExists(), "Wabbajack file must exist");
                this.ValidationRule(x => x.Install, p => p.DirectoryExists(), "Install folder file must exist");
                this.ValidationRule(x => x.Download, p => p != default, "Download folder must be set");
                
                BeginCommand = ReactiveCommand.Create(() => StartInstall(), this.IsValid());
                

                this.WhenAnyValue(t => t.ModListPath)
                    .Where(t => t != default)
                    .SelectAsync(disposables, async x => await LoadModList(x))
                    .Select(x => x)
                    .ObserveOn(AvaloniaScheduler.Instance)
                    .BindTo(this, t => t.ModList)
                    .DisposeWith(disposables);

                this.WhenAnyValue(t => t.ModListPath)
                    .Where(t => t != default)
                    .SelectAsync(disposables, async x => await LoadModListImage(x))
                    .ObserveOn(AvaloniaScheduler.Instance)
                    .BindTo(this, t => t.ModListImage)
                    .DisposeWith(disposables);                    
            });
        }

        private void StartInstall()
        {
            throw new System.NotImplementedException();
        }

        private async Task<IBitmap> LoadModListImage(AbsolutePath path)
        {
            await using var fs = path.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            using var ar = new ZipArchive(fs, ZipArchiveMode.Read);
            var entry = ar.GetEntry("modlist-image.png");
            await using var stream = entry.Open();
            return new Bitmap(new MemoryStream(await stream.ReadAllAsync()));
        }

        private async Task<ModList> LoadModList(AbsolutePath modlist)
        { 
            var definition= await StandardInstaller.LoadFromFile(_dtos, modlist);
            return definition;
        }

        public ViewModelActivator Activator { get; }
    }
}