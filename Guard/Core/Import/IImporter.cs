namespace Guard.Core.Import
{
    internal interface IImporter
    {
        internal enum ImportType
        {
            File,
            Clipboard
        }

        internal abstract string Name { get; }
        internal abstract ImportType Type { get; }
        internal abstract string SupportedFileExtensions { get; }
        internal abstract (int total, int duplicate, int tokenID) Parse(string? path);
    }
}
