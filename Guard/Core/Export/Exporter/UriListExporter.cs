using System.IO;

namespace Guard.Core.Export.Exporter
{
    internal class UriListExporter : IExporter
    {
        public string Name => "UriList";
        public IExporter.ExportType Type => IExporter.ExportType.File;
        public string ExportFileExtensions => "Uri-List (*.txt) | *.txt";
        public bool RequiresPassword() => false;
        public string GetDefaultFileName()
        {
            return $"2FATokens-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";
        }

        public async Task Export(string? path, string? password)
        {
            ArgumentNullException.ThrowIfNull(path);

            var tokens = await TokenManager.GetAllTokens();

            if (tokens.Count == 0)
            {
                throw new Exception("No tokens to export");
            }

            string uriList = string.Join(Environment.NewLine, tokens.Select(token => OTPUriCreator.GetUri(token)));

            await File.WriteAllTextAsync(path, uriList);
        }
    }
}
