namespace Guard.WPF.Core.Import.Importer
{
    internal interface IImporter
    {
        internal enum ImportType
        {
            File,
            Clipboard,
            ScreenCapture,
        }

        internal abstract string Name { get; }
        internal abstract ImportType Type { get; }
        internal abstract string SupportedFileExtensions { get; }
        internal abstract (int total, int duplicate, int tokenID) Parse(
            string? path,
            byte[]? password
        );
        internal abstract bool RequiresPassword(string? path);
    }
}
