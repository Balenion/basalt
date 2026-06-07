using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Text;
using OtpNet;

namespace AuthForge.Core.Services
{
    public class TotpService
    {
        public string GenerateCode(string base32Secret)
        {
            if (string.IsNullOrWhiteSpace(base32Secret))
                return "000000";

            try
            {
                byte[] secretBytes = Base32Encoding.ToBytes(base32Secret.Trim().ToUpper());

                var totp = new Totp(secretBytes);

                return totp.ComputeTotp();
            }
            catch
            {
                return "ERROR";
            }
        }

        public int GetRemainingSeconds()
        {
            long remaining = 30 - (DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 30);
            return (int)remaining;
        }

        public bool IsValidSecret(string base32Secret)
        {
            if (string.IsNullOrWhiteSpace(base32Secret)) return false;
            try
            {
                Base32Encoding.ToBytes(base32Secret.Trim().ToUpper());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
