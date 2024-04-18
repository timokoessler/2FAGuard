namespace Guard.Core.Models
{
    public class AegisExport
    {
        class HeaderParams
        {
            public required string Nonce { get; set; }
            public required string Tag { get; set; }
        }

        class HeaderSlot
        {
            public int Type { get; set; }
            public required string Uuid { get; set; }
            public required string Key { get; set; }
            public required HeaderParams Key_Params { get; set; }
            public int? N { get; set; }
            public int? R { get; set; }
            public int? P { get; set; }
            public string? Salt { get; set; }
            public bool? Repaired { get; set; }
            public bool? Is_Backup { get; set; }
        }

        class Header
        {
            public HeaderSlot[]? Slots { get; set; }
            public HeaderParams? Params { get; set; }
        }

        class DatabaseEntryInfo
        {
            public required string Secret { get; set; }
            public required string Algo { get; set; }
            public required int Digits { get; set; }
            public required int Period { get; set; }
        }

        class DatabaseEntry
        {
            public required string Type { get; set; }
            public required string Uuid { get; set; }
            public required string Name { get; set; }
            public required string Issuer { get; set; }
            public required string Note { get; set; }
            public bool Favorite { get; set; }

            // Todo Icon?
            public required DatabaseEntryInfo Info { get; set; }
        }

        class Database
        {
            public int Version { get; set; }
            public required DatabaseEntry[] Entries { get; set; }
        }

        class Encrypted
        {
            public int Version { get; set; }
            public required Header Header { get; set; }
            public required string Db { get; set; }
        }

        class Plain
        {
            public int Version { get; set; }
            public required Header Header { get; set; }
            public required Database Db { get; set; }
        }
    }
}
