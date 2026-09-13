using System.Security.Cryptography;

namespace AsyncRat.Client.Modules;

public sealed class ScreenCapture
{
    private long _captureCount;

    public long CaptureCount => Interlocked.Read(ref _captureCount);

    public byte[] Capture()
    {
        Interlocked.Increment(ref _captureCount);
        return GenerateStubBmp(320, 240);
    }

    public byte[] CaptureWithMetadata(out ScreenMeta meta)
    {
        var data = Capture();
        meta = new ScreenMeta(
            Width: 320,
            Height: 240,
            BitsPerPixel: 24,
            CapturedAt: DateTimeOffset.UtcNow,
            SizeBytes: data.Length
        );
        return data;
    }

    private static byte[] GenerateStubBmp(int width, int height)
    {
        int rowSize = ((width * 3 + 3) / 4) * 4;
        int pixelDataSize = rowSize * height;
        int fileSize = 54 + pixelDataSize;

        var bmp = new byte[fileSize];
        bmp[0] = 0x42; // 'B'
        bmp[1] = 0x4D; // 'M'
        BitConverter.TryWriteBytes(bmp.AsSpan(2), fileSize);
        BitConverter.TryWriteBytes(bmp.AsSpan(10), 54);          // pixel data offset
        BitConverter.TryWriteBytes(bmp.AsSpan(14), 40);          // DIB header size
        BitConverter.TryWriteBytes(bmp.AsSpan(18), width);
        BitConverter.TryWriteBytes(bmp.AsSpan(22), height);
        BitConverter.TryWriteBytes(bmp.AsSpan(26), (short)1);    // color planes
        BitConverter.TryWriteBytes(bmp.AsSpan(28), (short)24);   // bits per pixel
        BitConverter.TryWriteBytes(bmp.AsSpan(34), pixelDataSize);

        RandomNumberGenerator.Fill(bmp.AsSpan(54));

        return bmp;
    }
}

public sealed record ScreenMeta(
    int Width,
    int Height,
    int BitsPerPixel,
    DateTimeOffset CapturedAt,
    int SizeBytes
);
