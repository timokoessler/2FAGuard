using System.Text.Json.Serialization;

namespace Guard.WPF.Core.Models
{
    class ProtonAuthenticatorExport
    {
        public class UnencryptedExport
        {
            public required int Version { get; set; }
            public required ExportEntry[] Entries { get; set; }
        }

        public class EncryptedExport
        {
            public required int Version { get; set; }
            public required string Salt { get; set; }
            public required string Content { get; set; }
        }

        public class ExportEntry
        {
            public string? Id { get; set; }
            public required ExportEntryContent Content { get; set; }
            public string? Note { get; set; }
        }

        public class ExportEntryContent
        {
            public required string Uri { get; set; }

            [JsonPropertyName("entry_type")]
            public required string EntryType { get; set; }
            public required string Name { get; set; }
        }
    }
}
