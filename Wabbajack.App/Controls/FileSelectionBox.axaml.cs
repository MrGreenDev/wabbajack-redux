using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using Wabbajack.Paths;

namespace Wabbajack.App.Controls
{
    public partial class FileSelectionBox : UserControl, IActivatableView
    {
        public FileSelectionBox()
        {
            InitializeComponent();
            
            this.WhenActivated(dispose =>
            {
                this.WhenAnyValue(x => x.SelectedPath)
                    .Where(x => x != default)
                    .Select(t => t.ToString())
                    .BindTo(TextBox, t => t.Text)
                    .DisposeWith(dispose);
                

                SelectButton.Command = ReactiveCommand.Create(async () =>
                {
                    if (SelectFolder)
                    {
                        var dialog = new OpenFolderDialog()
                        {
                            Title = "Select a folder",
                        };
                        var result = await dialog.ShowAsync(App.MainWindow);
                        if (result != null)
                            SelectedPath = result.ToAbsolutePath();
                    }
                    else 
                    {
                        var dialog = new OpenFileDialog
                        {
                            AllowMultiple = false,
                            Title = "Select a file",
                            Filters = new()
                            {
                                new FileDialogFilter { Extensions = AllowedExtensions.Split("|").ToList(), Name = "*" }
                            }
                        };
                        var results = await dialog.ShowAsync(App.MainWindow);
                        if (results != null)
                            SelectedPath = results!.First().ToAbsolutePath();
                    }
                }).DisposeWith(dispose);
            });
        }

        public static readonly DirectProperty<FileSelectionBox, AbsolutePath> SelectedPathProperty =
            AvaloniaProperty.RegisterDirect<FileSelectionBox, AbsolutePath>(nameof(SelectedPath), o => o.SelectedPath);

        private AbsolutePath _selectedPath;
        public AbsolutePath SelectedPath
        {
            get => _selectedPath;
            set => SetAndRaise(SelectedPathProperty, ref _selectedPath, value);
        }

        public static readonly StyledProperty<string> AllowedExtensionsProperty =
            AvaloniaProperty.Register<FileSelectionBox, string>(nameof(AllowedExtensions));
        public string AllowedExtensions
        {
            get => GetValue(AllowedExtensionsProperty);
            set => SetValue(AllowedExtensionsProperty, value);
        }

        public AbsolutePath[] AllowedFileNames { get; set; }
        
        public static readonly StyledProperty<bool> SelectFolderProperty =
            AvaloniaProperty.Register<FileSelectionBox, bool>(nameof(SelectFolder));

        public bool SelectFolder
        {
            get => GetValue(SelectFolderProperty);
            set => SetValue(SelectFolderProperty, value);
        }
    }
}