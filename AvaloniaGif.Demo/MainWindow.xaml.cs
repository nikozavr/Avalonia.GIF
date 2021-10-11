using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.ReactiveUI;
using ReactiveUI;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using Image = Avalonia.Controls.Image;

namespace AvaloniaGif.Demo
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private GifInstance _gifInstance;
        private readonly ListBox _gifs;
        private readonly Image _image;

        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            this.WhenActivated(disposables => { });
            _gifs = this.FindControl<ListBox>("Gifs");
            _image = this.FindControl<Image>("image");
        }

        private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            _gifInstance?.Dispose();

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream((string) _gifs.SelectedItem);

            _gifInstance = new GifInstance(_image,
                                           stream,
                                           IterationCount.Infinite);
            _gifInstance.Process();
        }
    }
}