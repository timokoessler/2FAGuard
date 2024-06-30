using System.IO;
using Guard.Core.Models;

namespace Guard.WPF.Core.Import.Importer
{
    internal class UriListImporter : IImporter
    {
        public string Name => "UriList";
        public IImporter.ImportType Type => IImporter.ImportType.File;
        public string SupportedFileExtensions => "Uri-List (*.txt) | *.txt";

        public bool RequiresPassword(string? path) => false;

        public (int total, int duplicate, int tokenID) Parse(string? path, byte[]? password)
        {
            ArgumentNullException.ThrowIfNull(path);
            string[] lines = File.ReadAllLines(path);
            int total = 0;
            int duplicate = 0;
            int tokenID = 0;
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                OTPUri otpUri = OTPUriParser.Parse(line);
                DBTOTPToken dbToken = OTPUriParser.ConvertToDBToken(otpUri);
                if (!TokenManager.AddToken(dbToken))
                {
                    duplicate++;
                }
                else
                {
                    tokenID = dbToken.Id;
                }
                total++;
            }
            return (total, duplicate, tokenID);
        }
    }
}
