namespace Guard.Core.Import.Importer
{
    public interface IImporter
    {
        public enum ImportType
        {
            File,
            Clipboard,
            ScreenCapture,
        }

        string Name { get; }
        ImportType Type { get; }
        string SupportedFileExtensions { get; }
        (int total, int duplicate, int tokenID) Parse(
            string? path,
            byte[]? password
        );
        bool RequiresPassword(string? path);
    }
}