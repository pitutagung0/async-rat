using Xunit;
using AsyncRat.Client.Network;

namespace AsyncRat.Client.Tests;

public class FramerTests
{
    private readonly PacketFramer _framer = new();

    [Fact]
    public void Frame_PrependsBigEndianLength()
    {
        var data = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var framed = _framer.Frame(data);

        Assert.Equal(8, framed.Length);
        Assert.Equal(0, framed[0]);
        Assert.Equal(0, framed[1]);
        Assert.Equal(0, framed[2]);
        Assert.Equal(4, framed[3]);
        Assert.Equal(0xDE, framed[4]);
    }

    [Fact]
    public void Deframe_ExtractsPayload()
    {
        var framed = new byte[] { 0, 0, 0, 3, 0x41, 0x42, 0x43 };
        var (len, payload) = _framer.Deframe(framed);

        Assert.Equal(3, len);
        Assert.Equal([0x41, 0x42, 0x43], payload);
    }

    [Fact]
    public void Frame_EmptyPayload()
    {
        var framed = _framer.Frame([]);
        Assert.Equal(4, framed.Length);
        var (len, payload) = _framer.Deframe(framed);
        Assert.Equal(0, len);
        Assert.Empty(payload);
    }

    [Fact]
    public void Deframe_ThrowsOnShortBuffer()
    {
        Assert.Throws<ArgumentException>(() => _framer.Deframe(new byte[] { 0, 0 }));
    }

    [Fact]
    public void Deframe_ThrowsOnInvalidLength()
    {
        var bad = new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0x00 };
        Assert.Throws<InvalidDataException>(() => _framer.Deframe(bad));
    }

    [Fact]
    public async Task ReadFrameAsync_Roundtrip()
    {
        var original = new byte[256];
        Random.Shared.NextBytes(original);
        var framed = _framer.Frame(original);

        using var ms = new MemoryStream(framed);
        var result = await _framer.ReadFrameAsync(ms);

        Assert.Equal(original, result);
    }

    [Fact]
    public void Frame_ThrowsOnOversizedPayload()
    {
        var huge = new byte[PacketFramer.MaxPayloadSize + 1];
        Assert.Throws<ArgumentException>(() => _framer.Frame(huge));
    }
}
