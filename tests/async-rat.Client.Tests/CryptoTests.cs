using System.Security.Cryptography;
using Xunit;
using AsyncRat.Client.Network;

namespace AsyncRat.Client.Tests;

public class CryptoTests
{
    private static CryptoLayer MakeCrypto()
    {
        var key = CryptoLayer.GenerateKey();
        return new CryptoLayer(key);
    }

    [Fact]
    public void EncryptDecrypt_Roundtrip()
    {
        var crypto = MakeCrypto();
        var plain = "Hello, encrypted world!"u8.ToArray();

        var encrypted = crypto.Encrypt(plain);
        var decrypted = crypto.Decrypt(encrypted);

        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_EmptyPayload()
    {
        var crypto = MakeCrypto();
        var encrypted = crypto.Encrypt([]);
        var decrypted = crypto.Decrypt(encrypted);
        Assert.Empty(decrypted);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertexts()
    {
        var crypto = MakeCrypto();
        var plain = new byte[] { 1, 2, 3 };

        var a = crypto.Encrypt(plain);
        var b = crypto.Encrypt(plain);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Decrypt_ThrowsOnTamperedData()
    {
        var crypto = MakeCrypto();
        var encrypted = crypto.Encrypt(new byte[] { 0xAA, 0xBB });

        encrypted[^1] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() => crypto.Decrypt(encrypted));
    }

    [Fact]
    public void Decrypt_ThrowsOnShortInput()
    {
        var crypto = MakeCrypto();
        Assert.Throws<CryptographicException>(() => crypto.Decrypt(new byte[10]));
    }

    [Fact]
    public void Constructor_ThrowsOnWrongKeySize()
    {
        Assert.Throws<ArgumentException>(() => new CryptoLayer(new byte[16]));
    }

    [Fact]
    public void GenerateKey_Returns32Bytes()
    {
        var key = CryptoLayer.GenerateKey();
        Assert.Equal(32, key.Length);
    }

    [Fact]
    public void DeriveKey_Returns32Bytes()
    {
        var key = CryptoLayer.DeriveKey("password123");
        Assert.Equal(32, key.Length);
    }

    [Fact]
    public void EncryptDecrypt_LargePayload()
    {
        var crypto = MakeCrypto();
        var plain = new byte[64 * 1024];
        Random.Shared.NextBytes(plain);

        var decrypted = crypto.Decrypt(crypto.Encrypt(plain));
        Assert.Equal(plain, decrypted);
    }
}
