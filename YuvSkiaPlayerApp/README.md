# YuvSkiaPlayerApp (unsafe/pinned)

说明：
- 该示例使用 .NET 8 + Avalonia + Skia 后端，且在 csproj 中启用了 AllowUnsafeBlocks。
- SkiaVideoPlayer 使用 unsafe/pinned 方式直接写入 WriteableBitmap 的本机内存（BGRA8888），减少中间拷贝，提升性能，适合较高分辨率/帧率场景。
- 输入 YUV 格式假定为 YUV420p（Y plane，全分辨率；U/V 为 w/2 x h/2），帧按 Y..U..V 顺序排列。

运行：
1. 确认已安装 .NET 8 SDK 与 Visual Studio 2022。
2. 运行脚本后，若本机有 dotnet，脚本会尝试创建 solution 并 restore；否则在生成后手动运行：
   dotnet restore
3. 在 VS2022 打开生成的 .sln 并运行，或用命令行：
   dotnet run --project YuvSkiaPlayerApp/YuvSkiaPlayerApp.csproj
