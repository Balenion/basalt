using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AuthForge.Core.Services
{
    public class JwtService
    {
        /// <summary>
        /// Генерирует массив случайных криптографически стойких байт заданной битности
        /// </summary>
        public byte[] GenerateSecretBytes(int bits = 256)
        {
            if (bits <= 0 || bits % 8 != 0)
            {
                throw new ArgumentException("Количество бит должно быть больше нуля и кратно 8.", nameof(bits));
            }

            var bytes = new byte[bits / 8];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }

        public class JwtDecodeResult
        {
            public string HeaderJson { get; set; } = "{}";
            public string PayloadJson { get; set; } = "{}";
            public bool IsValidStructure { get; set; }
            public DateTime? ExpirationTime { get; set; }
            public string Algorithm { get; set; } = string.Empty;
        }

        /// <summary>
        /// Парсит JWT токен и возвращает отформатированный JSON для Header и Payload
        /// </summary>
        public JwtDecodeResult DecodeToken(string token)
        {
            var result = new JwtDecodeResult();
            if (string.IsNullOrWhiteSpace(token)) return result;

            string[] parts = token.Trim().Split('.');
            if (parts.Length != 3) return result; // У JWT строго 3 части

            try
            {
                result.HeaderJson = FormatJson(DecodeBase64Url(parts[0]));
                result.PayloadJson = FormatJson(DecodeBase64Url(parts[1]));
                result.IsValidStructure = true;

                using var headerDoc = JsonDocument.Parse(DecodeBase64Url(parts[0]));
                if (headerDoc.RootElement.TryGetProperty("alg", out var algProp))
                {
                    result.Algorithm = algProp.GetString() ?? string.Empty;
                }

                using var payloadDoc = JsonDocument.Parse(DecodeBase64Url(parts[1]));
                if (payloadDoc.RootElement.TryGetProperty("exp", out var expProp) && expProp.TryGetInt64(out long expUnix))
                {
                    result.ExpirationTime = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime.ToLocalTime();
                }
            }
            catch
            {
                result.IsValidStructure = false;
            }

            return result;
        }

        /// <summary>
        /// Проверяет подпись JWT токена (поддерживает HS256, HS384, HS512)
        /// </summary>
        public bool VerifySignature(string token, string secret, string algorithm)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrEmpty(secret)) return false;

            string[] parts = token.Trim().Split('.');
            if (parts.Length != 3) return false;

            try
            {
                string message = $"{parts[0]}.{parts[1]}";
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
                byte[] signatureBytes = DecodeBase64UrlBytes(parts[2]);

                byte[] computedHash;
                using (HMAC hmac = algorithm.ToUpper() switch
                {
                    "HS384" => new HMACSHA384(secretBytes),
                    "HS512" => new HMACSHA512(secretBytes),
                    _ => new HMACSHA256(secretBytes)
                })
                {
                    computedHash = hmac.ComputeHash(messageBytes);
                }

                return CryptographicOperations.FixedTimeEquals(computedHash, signatureBytes);
            }
            catch
            {
                return false;
            }
        }

        private string DecodeBase64Url(string base64Url)
        {
            return Encoding.UTF8.GetString(DecodeBase64UrlBytes(base64Url));
        }

        private byte[] DecodeBase64UrlBytes(string base64Url)
        {
            string padded = base64Url.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }
            return Convert.FromBase64String(padded);
        }

        private string FormatJson(string unformattedJson)
        {
            using var doc = JsonDocument.Parse(unformattedJson);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
