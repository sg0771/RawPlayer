using Avalonia.Controls;
using Avalonia.Interactivity;
using SDL2;
using System;
using System.IO;

namespace PcmPlayer
{
    public partial class MainWindow : Window
    {
        private SDL.SDL_AudioSpec _spec;
        private uint _deviceId;
        private FileStream? _pcmFileStream;
        private BinaryReader? _pcmReader;
        private bool _paused = false;
        private bool _stopRequested = false;

        public MainWindow()
        {
            InitializeComponent();

            BrowseButton.Click += BrowseButton_Click;
            StartButton.Click += StartButton_Click;
            PauseButton.Click += PauseButton_Click;
            ResumeButton.Click += ResumeButton_Click;
            StopButton.Click += StopButton_Click;

            SDL.SDL_Init(SDL.SDL_INIT_AUDIO);
        }

        private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
        {
            var result = await this.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "选择 PCM 文件",
                FileTypeFilter = new[] { new Avalonia.Platform.Storage.FilePickerFileType("PCM 文件") { Patterns = new[] { "*.pcm" } } },
                AllowMultiple = false
            });

            if (result.Count > 0)
                FilePathTextBox.Text = result[0].Path.LocalPath;
        }

        private void StartButton_Click(object? sender, RoutedEventArgs e)
        {
            if (!File.Exists(FilePathTextBox.Text)) return;

            _pcmFileStream?.Dispose();
            _pcmFileStream = new FileStream(FilePathTextBox.Text, FileMode.Open, FileAccess.Read);
            _pcmReader = new BinaryReader(_pcmFileStream);

            int sampleRate = (int)SampleRateBox.Value!;
            int channels = (int)ChannelsBox.Value!;
            bool loop = LoopCheckBox.IsChecked == true;

            _spec = new SDL.SDL_AudioSpec
            {
                freq = sampleRate,
                format = SDL.AUDIO_S16SYS,
                channels = (byte)channels,
                samples = 4096,
                callback = (userData, stream, len) =>
                {
                    byte[] buffer = new byte[len];
                    int offset = 0;

                    while (offset < len)
                    {
                        if (_paused || _pcmReader == null)
                        {
                            // 暂停时填充零
                            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, stream, len);
                            return;
                        }

                        int bytesToRead = len - offset;
                        byte[] data = _pcmReader.ReadBytes(bytesToRead);

                        if (data.Length == 0) // 文件末尾
                        {
                            if (loop)
                            {
                                _pcmReader.BaseStream.Seek(0, SeekOrigin.Begin);
                                continue;
                            }
                            else
                            {
                                _stopRequested = true;
                                break;
                            }
                        }

                        Array.Copy(data, 0, buffer, offset, data.Length);
                        offset += data.Length;
                    }

                    System.Runtime.InteropServices.Marshal.Copy(buffer, 0, stream, len);

                    if (_stopRequested)
                        SDL.SDL_PauseAudioDevice(_deviceId, 1);
                }
            };

            _deviceId = SDL.SDL_OpenAudioDevice(null, 0, ref _spec, out _, 0);
            SDL.SDL_PauseAudioDevice(_deviceId, 0);
        }

        private void PauseButton_Click(object? sender, RoutedEventArgs e) => _paused = true;
        private void ResumeButton_Click(object? sender, RoutedEventArgs e) => _paused = false;

        private void StopButton_Click(object? sender, RoutedEventArgs e)
        {
            _stopRequested = true;
            _paused = false;
            if (_deviceId != 0)
            {
                SDL.SDL_CloseAudioDevice(_deviceId);
                _deviceId = 0;
            }
            _pcmFileStream?.Dispose();
            _pcmFileStream = null;
        }
    }
}