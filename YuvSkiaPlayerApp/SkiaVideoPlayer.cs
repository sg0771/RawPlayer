using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace YuvSkiaPlayerApp
{
    // 使用 unsafe + pinned 高性能实现：直接在 WriteableBitmap 的本机内存写入 BGRA 数据，避免中间 large managed arrays 拷贝。
    public class SkiaVideoPlayer : UserControl
    {
        private Image _img = new Image();
        private CancellationTokenSource? _cts;
        private bool _paused;

        public SkiaVideoPlayer()
        {
            this.Content = _img;
        }

        public void Start(string path, int width, int height, int fps, bool loop)
        {
            Stop();
            _cts = new CancellationTokenSource();
            _paused = false;
            Task.Run(() => LoopPlay(path, width, height, fps, loop, _cts.Token));
        }

        public void TogglePause() => _paused = !_paused;

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }
        }

        private async Task LoopPlay(string path, int width, int height, int fps, bool loop, CancellationToken ct)
        {
            var frameSize = width * height + 2 * (width / 2) * (height / 2);
            try
            {
                using var fs = File.OpenRead(path);
                var buffer = new byte[frameSize];
                do
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    while (true)
                    {
                        if (ct.IsCancellationRequested) return;
                        if (_paused)
                        {
                            await Task.Delay(30, ct);
                            continue;
                        }

                        var read = 0;
                        while (read < frameSize)
                        {
                            var r = await fs.ReadAsync(buffer, read, frameSize - read, ct);
                            if (r == 0) break;
                            read += r;
                        }

                        if (read < frameSize) break; // 文件结束

                        // 在 UI 线程中创建/锁定 WriteableBitmap 并直接写入其内存（unsafe）
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            var bmp = CreateWriteableBitmapFromYuvUnsafe(buffer, width, height);
                            _img.Source = bmp;
                        });

                        await Task.Delay(1000 / Math.Max(1, fps), ct);
                    }
                } while (loop);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine("Play error: " + ex);
            }
        }

        // 在 UI 线程调用；直接写入 WriteableBitmap 内存，使用 unsafe 块和指针
        private static WriteableBitmap CreateWriteableBitmapFromYuvUnsafe(byte[] yuv, int w, int h)
        {
            var bmp = new WriteableBitmap(new Avalonia.PixelSize(w, h), new Avalonia.Vector(96, 96),
                                         Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Opaque);
            using (var fb = bmp.Lock())
            {
                unsafe
                {
                    byte* dest = (byte*)fb.Address.ToPointer();
                    int rowBytes = fb.RowBytes;

                    int ySize = w * h;
                    int uvW = w / 2, uvH = h / 2;
                    int uOff = ySize;
                    int vOff = ySize + uvW * uvH;

                    // 写入每一行，每个像素写 BGRA
                    for (int j = 0; j < h; j++)
                    {
                        byte* rowPtr = dest + j * rowBytes;
                        int yRow = j * w;
                        int uvRow = (j / 2) * uvW;
                        for (int i = 0; i < w; i++)
                        {
                            int yIndex = yRow + i;
                            int uvIndex = uvRow + (i / 2);

                            int Y = yuv[yIndex];
                            int U = yuv[uOff + uvIndex];
                            int V = yuv[vOff + uvIndex];

                            int C = Y - 16;
                            int D = U - 128;
                            int E = V - 128;

                            int R = (298 * C + 409 * E + 128) >> 8;
                            int G = (298 * C - 100 * D - 208 * E + 128) >> 8;
                            int B = (298 * C + 516 * D + 128) >> 8;

                            if (R < 0) R = 0; else if (R > 255) R = 255;
                            if (G < 0) G = 0; else if (G > 255) G = 255;
                            if (B < 0) B = 0; else if (B > 255) B = 255;

                            int pos = i * 4;
                            rowPtr[pos + 0] = (byte)B;
                            rowPtr[pos + 1] = (byte)G;
                            rowPtr[pos + 2] = (byte)R;
                            rowPtr[pos + 3] = 255;
                        }
                    }
                }
            }
            return bmp;
        }
    }
}
