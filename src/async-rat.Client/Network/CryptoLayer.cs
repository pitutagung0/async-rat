using System.Security.Cryptography;

namespace AsyncRat.Client.Network;

public sealed class CryptoLayer
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public CryptoLayer(byte[] key)
    {
        if (key.Length != 32)
            throw new ArgumentException("AES-256-GCM requires a 32-byte key");
        _key = (byte[])key.Clone();
    }

    public byte[] Encrypt(byte[] plaintext)
    {
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[NonceSize + ciphertext.Length + TagSize];
        nonce.CopyTo(result, 0);
        ciphertext.CopyTo(result, NonceSize);
        tag.CopyTo(result, NonceSize + ciphertext.Length);
        return result;
    }

    public byte[] Decrypt(byte[] encrypted)
    {
        if (encrypted.Length < NonceSize + TagSize)
            throw new CryptographicException("Ciphertext too short");

        var nonce = encrypted.AsSpan(0, NonceSize);
        int ctLen = encrypted.Length - NonceSize - TagSize;
        var ciphertext = encrypted.AsSpan(NonceSize, ctLen);
        var tag = encrypted.AsSpan(NonceSize + ctLen, TagSize);

        var plaintext = new byte[ctLen];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    public static byte[] DeriveKey(string password, byte[]? salt = null)
    {
        salt ??= RandomNumberGenerator.GetBytes(16);
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations: 100_000,
            HashAlgorithmName.SHA256,
            outputLength: 32
        );
    }

    public static byte[] GenerateKey() => RandomNumberGenerator.GetBytes(32);
}
