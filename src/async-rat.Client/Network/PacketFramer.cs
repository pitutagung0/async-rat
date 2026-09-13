using System.Buffers.Binary;

namespace AsyncRat.Client.Network;

public sealed class PacketFramer
{
    public const int HeaderSize = 4;
    public const int MaxPayloadSize = 16 * 1024 * 1024; // 16 MB

    public byte[] Frame(byte[] payload)
    {
        if (payload.Length > MaxPayloadSize)
            throw new ArgumentException($"Payload too large: {payload.Length} > {MaxPayloadSize}");

        var frame = new byte[HeaderSize + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, HeaderSize), payload.Length);
        payload.CopyTo(frame.AsSpan(HeaderSize));
        return frame;
    }

    public async Task<byte[]> ReadFrameAsync(Stream stream, CancellationToken ct = default)
    {
        var header = new byte[HeaderSize];
        await ReadExactAsync(stream, header, ct);

        int length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length < 0 || length > MaxPayloadSize)
            throw new InvalidDataException($"Invalid frame length: {length}");

        if (length == 0) return [];

        var payload = new byte[length];
        await ReadExactAsync(stream, payload, ct);
        return payload;
    }

    public (int Length, byte[] Payload) Deframe(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < HeaderSize)
            throw new ArgumentException("Buffer too short for header");

        int length = BinaryPrimitives.ReadInt32BigEndian(buffer[..HeaderSize]);
        if (length < 0 || length > MaxPayloadSize)
            throw new InvalidDataException($"Invalid frame length: {length}");

        if (buffer.Length < HeaderSize + length)
            throw new ArgumentException("Buffer too short for payload");

        return (length, buffer.Slice(HeaderSize, length).ToArray());
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct);
            if (read == 0)
                throw new IOException($"Connection closed (read {offset}/{buffer.Length} bytes)");
            offset += read;
        }
    }
}
