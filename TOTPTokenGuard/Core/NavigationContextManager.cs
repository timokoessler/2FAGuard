namespace TOTPTokenGuard.Core
{
    internal class NavigationContext : Dictionary<string, object>
    {
        internal NavigationContext() { }

        internal NavigationContext(IDictionary<string, object> dictionary)
            : base(dictionary) { }
    }

    internal class NavigationContextManager
    {
        internal static NavigationContext CurrentContext { get; set; } = [];

        internal static void ClearContext()
        {
            CurrentContext = new NavigationContext();
        }
    }
}
