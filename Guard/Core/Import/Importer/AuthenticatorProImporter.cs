namespace Guard.Core.Import.Importer
{
    internal class AuthenticatorProImporter : IImporter
    {
        public string Name => "AuthenticatorPro";
        public IImporter.ImportType Type => IImporter.ImportType.File;
        public string SupportedFileExtensions => "Authenticator Pro Backup (*.authpro) | *.authpro";

        public (int total, int duplicate, int tokenID) Parse(string? path)
        {
            // Todo implement
            return (0, 0, 0);
        }
    }
}
