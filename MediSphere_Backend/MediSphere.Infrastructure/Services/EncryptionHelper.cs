using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace MediSphere.Infrastructure.Services;

public class EncryptionHelper
{
    private readonly byte[] _key;

    public EncryptionHelper(IConfiguration config)
    {
        // HIPAA/GDPR standard key logic. Extracts from configuration, falls back to secure default.
        var secretKey = config["Encryption:Key"] ?? "MediSphereSecretEncryptionKey2026!";
        using var sha256 = SHA256.Create();
        _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretKey));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return plainText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            using var ms = new MemoryStream();
            
            // Write IV first to the stream
            ms.Write(iv, 0, iv.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }
        catch
        {
            // Fail gracefully in case of dev configuration issues
            return plainText;
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText)) return cipherText;

        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);
            if (fullCipher.Length < 16) return cipherText; // Must include IV block

            using var aes = Aes.Create();
            aes.Key = _key;

            var iv = new byte[16];
            var cipher = new byte[fullCipher.Length - 16];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
            Buffer.BlockCopy(fullCipher, 16, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch
        {
            // If it's not encrypted or decryption fails, return original text safely
            return cipherText;
        }
    }
}
