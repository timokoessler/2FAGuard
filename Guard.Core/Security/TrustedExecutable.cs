using Microsoft.Security.Extensions;

namespace Guard.Core.Security
{
    public class TrustedExecutable
    {

        private static readonly string[] AllowedPublisherNameParts =
        [
            "Timo Kössler",
            "Timo Koessler",
            "2FAGuard",
        ];

        public static bool IsFileTrusted(string path, bool strict)
        {

            if (!File.Exists(path))
            {
                return false;
            }

            using FileStream fs = File.OpenRead(path);
            FileSignatureInfo sigInfo = FileSignatureInfo.GetFromFileStream(fs);

            if (sigInfo.State != SignatureState.SignedAndTrusted)
            {
                return false;
            }

            if (!strict) {
                return true;
            }

            string publisherName = sigInfo.SigningCertificate.SubjectName.Name;

            // Check if the publisher name contains any of the allowed parts
            foreach (string allowedPart in AllowedPublisherNameParts)
            {
                if (publisherName.Contains(allowedPart, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;

        }
    }
}
