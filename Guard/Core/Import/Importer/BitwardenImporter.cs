using System.IO;
using System.Text.Json;
using Guard.Core.Icons;
using Guard.Core.Models;
using Guard.Core.Security;
using OtpNet;

namespace Guard.Core.Import.Importer
{
    internal class BitwardenImporter : IImporter
    {
        public string Name => "Bitwarden";
        public IImporter.ImportType Type => IImporter.ImportType.File;
        public string SupportedFileExtensions => "Bitwarden Export (*.json) | *.json";

        public bool RequiresPassword(string? path) => false;

        private readonly JsonSerializerOptions jsonSerializerOptions =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

        private enum BitwardenTOTPType
        {
            Secret,
            Uri
        }

        public (int total, int duplicate, int tokenID) Parse(string? path, string? password)
        {
            ArgumentNullException.ThrowIfNull(path);
            using FileStream stream = File.OpenRead(path);
            BitwardenExportFile? exportFile =
                JsonSerializer.Deserialize<BitwardenExportFile>(stream, jsonSerializerOptions)
                ?? throw new Exception("Could not parse Bitwarden export file");
            if (exportFile.Encrypted == true)
            {
                throw new Exception("Encrypted Bitwarden export files are not supported yet");
            }

            if (exportFile.Items == null)
            {
                throw new Exception("Invalid Bitwarden export file: No items found");
            }

            int total = 0,
                duplicate = 0,
                tokenID = 0;

            foreach (BitwardenExportFile.Item item in exportFile.Items)
            {
                if (item.Login == null || item.Login.Totp == null)
                {
                    continue;
                }
                string totp = item.Login.Totp;

                BitwardenTOTPType exportTotpType = totp.StartsWith("otpauth://")
                    ? BitwardenTOTPType.Uri
                    : BitwardenTOTPType.Secret;

                DBTOTPToken dbToken;
                if (exportTotpType == BitwardenTOTPType.Uri)
                {
                    OTPUri otpUri = OTPUriParser.Parse(totp);
                    dbToken = OTPUriParser.ConvertToDBToken(otpUri);
                    total += 1;
                    if (!TokenManager.AddToken(dbToken))
                    {
                        duplicate += 1;
                    }
                    else
                    {
                        tokenID = dbToken.Id;
                    }
                }
                else
                {
                    if (item.Name == null)
                    {
                        throw new Exception("Invalid Bitwarden export file: No item name found");
                    }

                    IconManager.TotpIcon icon = IconManager.GetIcon(
                        item.Name,
                        IconManager.IconColor.Colored,
                        IconManager.IconType.Any
                    );

                    try
                    {
                        Base32Encoding.ToBytes(item.Login.Totp);
                    }
                    catch
                    {
                        throw new Exception($"{I18n.GetString("td.invalidsecret")} ({item.Name})");
                    }

                    dbToken = new()
                    {
                        Id = TokenManager.GetNextId(),
                        Issuer = item.Name,
                        EncryptedSecret = Auth.GetMainEncryptionHelper()
                            .EncryptString(item.Login.Totp),
                        Username = item.Login.Username,
                        CreationTime = DateTime.Now
                    };

                    if (icon != null && icon.Type != IconManager.IconType.Default)
                    {
                        dbToken.Icon = icon.Name;
                        dbToken.IconType = icon.Type;
                    }

                    total += 1;
                    if (!TokenManager.AddToken(dbToken))
                    {
                        duplicate += 1;
                    }
                    else
                    {
                        tokenID = dbToken.Id;
                    }
                }
            }

            return (total, duplicate, tokenID);
        }
    }
}
