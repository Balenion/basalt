using AuthForge.Core.Models;
using ClosedXML.Excel;
using OtpNet;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AuthForge.Core.Services
{
    public class ExcelService
    {
        public void CreateTemplate(string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Users");
            worksheet.Cell(1, 1).Value = "Login";
            worksheet.Cell(1, 2).Value = "Password";
            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public List<UserInput> ReadEmployees(string filePath)
        {
            var users = new List<UserInput>();
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                users.Add(new UserInput(
                    row.Cell(1).GetString(),
                    row.Cell(2).GetString()
                ));
            }
            return users;
        }

        public void SaveResults(string filePath, List<UserOutput> results)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Hashed Data");

            worksheet.Cell(1, 1).Value = "Login";
            worksheet.Cell(1, 2).Value = "Password Hash";
            worksheet.Cell(1, 3).Value = "Salt";
            worksheet.Cell(1, 4).Value = "Algorithm";

            for (int i = 0; i < results.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = results[i].Login;
                worksheet.Cell(i + 2, 2).Value = results[i].PasswordHash;
                worksheet.Cell(i + 2, 3).Value = results[i].Salt;
                worksheet.Cell(i + 2, 4).Value = results[i].Algorithm;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public bool IsTemplateValid(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || new FileInfo(filePath).Length < 100)
            {
                return false;
            }

            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1);
                    if (worksheet == null) return false;

                    string col1 = worksheet.Cell(1, 1).GetString().Trim();
                    string col2 = worksheet.Cell(1, 2).GetString().Trim();

                    return col1.Equals("Login", StringComparison.OrdinalIgnoreCase) &&
                           col2.Equals("Password", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void GenerateAdvancedSeed(string filePath, SeedOptions options)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Users Seed");
            var rand = new Random();

            var totpIndices = new HashSet<int>();
            if (options.Include2FA && options.TotpPercentage > 0)
            {
                int target2FaCount = (int)Math.Ceiling((options.TotpPercentage / 100.0) * options.Count);
                target2FaCount = Math.Min(target2FaCount, options.Count);

                var shuffledIndices = Enumerable.Range(0, options.Count).OrderBy(x => rand.Next()).Take(target2FaCount);
                foreach (int idx in shuffledIndices)
                {
                    totpIndices.Add(idx);
                }
            }

            int currentColumn = 1;
            worksheet.Cell(1, currentColumn++).Value = "Login";
            worksheet.Cell(1, currentColumn++).Value = "Password";

            int nameCol = 0, lastNameCol = 0, firstNameCol = 0, midNameCol = 0;
            if (options.IncludeName)
            {
                if (options.SplitName)
                {
                    lastNameCol = currentColumn++; worksheet.Cell(1, lastNameCol).Value = "LastName";
                    firstNameCol = currentColumn++; worksheet.Cell(1, firstNameCol).Value = "FirstName";
                    midNameCol = currentColumn++; worksheet.Cell(1, midNameCol).Value = "MiddleName";
                }
                else
                {
                    nameCol = currentColumn++; worksheet.Cell(1, nameCol).Value = "FullName";
                }
            }

            int emailCol = options.IncludeEmail ? currentColumn++ : 0;
            if (emailCol > 0) worksheet.Cell(1, emailCol).Value = "Email";

            int phoneCol = options.IncludePhone ? currentColumn++ : 0;
            if (phoneCol > 0) worksheet.Cell(1, phoneCol).Value = "Phone";

            int dateCol = options.IncludeBirthDate ? currentColumn++ : 0;
            if (dateCol > 0) worksheet.Cell(1, dateCol).Value = "BirthDate";

            int roleCol = options.IncludeRole ? currentColumn++ : 0;
            if (roleCol > 0) worksheet.Cell(1, roleCol).Value = "Role";

            int statusCol = options.IncludeStatus ? currentColumn++ : 0;
            if (statusCol > 0) worksheet.Cell(1, statusCol).Value = "Status";

            int is2faEnabledCol = options.Include2FA ? currentColumn++ : 0;
            if (is2faEnabledCol > 0) worksheet.Cell(1, is2faEnabledCol).Value = "Is2FAEnabled";

            int totpSecretCol = options.Include2FA ? currentColumn++ : 0;
            if (totpSecretCol > 0) worksheet.Cell(1, totpSecretCol).Value = "TOTP_Secret";

            string[] lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Garcia", "Rodriguez", "Wilson", "Thomas", "Taylor", "Anderson" };
            string[] firstNames = { "John", "James", "Robert", "Michael", "William", "David", "Richard", "Joseph", "Thomas", "Charles", "Mary", "Patricia", "Jennifer", "Linda" };
            string[] midNames = { "Alexander", "Edward", "Christopher", "James", "Lee", "Michael", "William", "Robert", "Grace", "Elizabeth", "Marie", "Ann" };
            string[] domains = { "gmail.com", "yahoo.com", "outlook.com", "hotmail.com", "proton.me", "company.local" };
            string[] roles = { "User", "User", "User", "Manager", "Admin" };
            string[] statuses = { "Active", "Active", "Active", "Pending", "Suspended" };

            char[] passwordChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*".ToCharArray();

            for (int i = 0; i < options.Count; i++)
            {
                int row = i + 2;

                string login = $"user_{rand.Next(100, 999)}{i}";
                if (i == 0) login = "admin";

                worksheet.Cell(row, 1).Value = login;
                worksheet.Cell(row, 2).Value = GenerateSecurePassword(options.PasswordLength, passwordChars);

                string ln = lastNames[rand.Next(lastNames.Length)];
                string fn = firstNames[rand.Next(firstNames.Length)];
                string mn = midNames[rand.Next(midNames.Length)];

                if (options.IncludeName)
                {
                    if (options.SplitName)
                    {
                        worksheet.Cell(row, lastNameCol).Value = ln;
                        worksheet.Cell(row, firstNameCol).Value = fn;
                        worksheet.Cell(row, midNameCol).Value = mn;
                    }
                    else
                    {
                        worksheet.Cell(row, nameCol).Value = $"{fn} {mn} {ln}";
                    }
                }

                if (options.IncludeEmail)
                    worksheet.Cell(row, emailCol).Value = $"{login}@{domains[rand.Next(domains.Length)]}".ToLower();

                if (options.IncludePhone)
                    worksheet.Cell(row, phoneCol).Value = $"+1202555{rand.Next(1000, 9999)}";

                if (options.IncludeBirthDate)
                    worksheet.Cell(row, dateCol).Value = DateTime.Today.AddDays(-rand.Next(7300, 16000)).ToString("yyyy-MM-dd");

                if (options.IncludeRole)
                    worksheet.Cell(row, roleCol).Value = roles[rand.Next(roles.Length)];
                if (options.IncludeStatus)
                    worksheet.Cell(row, statusCol).Value = statuses[rand.Next(statuses.Length)];

                if (options.Include2FA)
                {
                    if (totpIndices.Contains(i))
                    {
                        worksheet.Cell(row, is2faEnabledCol).Value = "True";

                        byte[] secretBytes = new byte[20]; // 160 бит
                        using var rng = RandomNumberGenerator.Create();
                        rng.GetBytes(secretBytes);
                        worksheet.Cell(row, totpSecretCol).Value = Base32Encoding.ToString(secretBytes);
                    }
                    else
                    {
                        worksheet.Cell(row, is2faEnabledCol).Value = "False";
                        worksheet.Cell(row, totpSecretCol).Value = string.Empty;
                    }
                }
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        private string GenerateSecurePassword(int length, char[] charPool)
        {
            var password = new StringBuilder();
            byte[] randomBytes = new byte[4];
            using var rng = RandomNumberGenerator.Create();

            while (password.Length < length)
            {
                rng.GetBytes(randomBytes);
                uint randNum = BitConverter.ToUInt32(randomBytes, 0);
                password.Append(charPool[randNum % charPool.Length]);
            }
            return password.ToString();
        }

        public class SeedOptions
        {
            public int Count { get; set; }
            public int PasswordLength { get; set; }
            public bool IncludeName { get; set; }
            public bool SplitName { get; set; }
            public bool IncludeEmail { get; set; }
            public bool IncludePhone { get; set; }
            public bool IncludeBirthDate { get; set; }
            public bool Include2FA { get; set; }
            public int TotpPercentage { get; set; }
            public bool IncludeRole { get; set; }
            public bool IncludeStatus { get; set; }
        }
    }
}
