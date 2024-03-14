namespace Guard.Core.Export.Exporter
{
    internal interface IExporter
    {
        internal enum ExportType
        {
            File
        }

        internal abstract string Name { get; }
        internal abstract ExportType Type { get; }
        internal abstract string ExportFileExtensions { get; }
        internal abstract async void Export(string? path, string? password);
        internal abstract bool RequiresPassword();
    }
}
