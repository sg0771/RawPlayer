using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.IO;

namespace YuvSkiaPlayerApp
{
    public partial class MainWindow : Window
    {
        private SkiaVideoPlayer? _player;

        public MainWindow()
        {
            InitializeComponent();
            _player = this.FindControl<SkiaVideoPlayer>("Player");

            var browse = this.FindControl<Button>("BrowseBtn");
            var fileBox = this.FindControl<TextBox>("FilePathBox");
            var play = this.FindControl<Button>("PlayBtn");
            var pause = this.FindControl<Button>("PauseBtn");
            var stop = this.FindControl<Button>("StopBtn");
            var widthBox = this.FindControl<TextBox>("WidthBox");
            var heightBox = this.FindControl<TextBox>("HeightBox");
            var fpsBox = this.FindControl<TextBox>("FpsBox");
            var loopBox = this.FindControl<CheckBox>("LoopCheck");

            browse.Click += async (_, __) =>
            {
                var dlg = new OpenFileDialog();
                dlg.AllowMultiple = false;
                dlg.Filters.Add(new FileDialogFilter() { Name = "YUV files", Extensions = { "yuv" } });
                var res = await dlg.ShowAsync(this);
                if (res != null && res.Length > 0)
                    fileBox.Text = res[0];
            };

            play.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(fileBox.Text) || !File.Exists(fileBox.Text))
                {
                    _ = MessageAsync("请选择存在的 YUV 文件路径。");
                    return;
                }

                if(!int.TryParse(widthBox.Text, out int w) || !int.TryParse(heightBox.Text, out int h) || w<=0 || h<=0)
                {
                    _ = MessageAsync("请填写合法的宽度和高度。");
                    return;
                }

                if(!int.TryParse(fpsBox.Text, out int fps) || fps<=0) fps = 25;
                var loop = loopBox.IsChecked ?? true;

                _player?.Start(fileBox.Text, w, h, fps, loop);
            };

            pause.Click += (_, __) =>
            {
                _player?.TogglePause();
            };

            stop.Click += (_, __) =>
            {
                _player?.Stop();
            };
        }

        private async System.Threading.Tasks.Task MessageAsync(string text)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dlg = new Window
                {
                    Width = 420,
                    Height = 160,
                    Content = new TextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
                };
                await dlg.ShowDialog(this);
            });
        }
    }
}
