namespace Guard.Core.Export.Exporter
{
    internal class BackupExporter : IExporter
    {
        public string Name => "Backup";
        public IExporter.ExportType Type => IExporter.ExportType.File;
        public string ExportFileExtensions => "2FAGuard Backup (*.2fabackup) | *.2fabackup";

        public bool RequiresPassword() => true;

        public void Export(string? path, string? password)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(password);
            throw new NotImplementedException();
        }
    }
}
