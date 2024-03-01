using Google.Protobuf;
using Guard.Core.Import.GoogleOTPMigration;
using Guard.Core.Models;
using System.IO;

namespace Guard.Core.Import
{
    internal class GoogleAuthenticator
    {
        internal static OTPUri Parse(string uriString)
        {
            MigrationPayload migrationPayload = new();
            //migrationPayload.MergeFrom();
            Uri uri = new(uriString);
            if(uri.Scheme != "otpauth-migration")
            {
                throw new Exception("Invalid URI scheme");
            }
            string[] query = uri.Query[1..].Split('&');
            if(query.Length != 1)
            {
                throw new Exception("Invalid URI: Expected 1 query parameter");
            }

            if (!query[0].StartsWith("data="))
            {
                throw new Exception("Invalid URI: Expected data query parameter");
            }

            string urlDecoded = Uri.UnescapeDataString(query[0][5..]);
            byte[] data = Convert.FromBase64String(urlDecoded);
            Stream stream = new MemoryStream(data);
            migrationPayload.MergeFrom(stream);

            // Todo


        }
    }
}
