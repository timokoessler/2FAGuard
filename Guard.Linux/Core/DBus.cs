using Tmds.DBus.Protocol;

namespace Guard.Linux.Core
{
    internal static class DBus
    {
        private static readonly Lazy<Task<Connection>> session = new(() =>
            Connect(Address.Session, "session")
        );
        private static readonly Lazy<Task<Connection>> system = new(() =>
            Connect(Address.System, "system")
        );

        internal static Task<Connection> GetSession() => session.Value;

        internal static Task<Connection> GetSystem() => system.Value;

        private static async Task<Connection> Connect(string? address, string name)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new InvalidOperationException($"D-Bus {name} bus address not found");
            }
            Connection connection = new(address);
            await connection.ConnectAsync();
            return connection;
        }
    }
}
