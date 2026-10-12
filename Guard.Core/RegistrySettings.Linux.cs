using System.Text.Json;

namespace Guard.Core
{
    /// <summary>
    /// On Linux the policies that Windows reads from HKCU\Software\Policies\2FAGuard are read from
    /// a JSON file that only administrators can write. Sub keys become nested objects, e.g.
    /// { "DisableAutoUpdate": true, "Setup": { "HideSkip": true }, "Password": { "MinLength": 12 } }
    /// </summary>
    public partial class RegistrySettings
    {
        public const string PolicyFilePath = "/etc/2fa-guard/policies.json";
        private const string RootKey = @"HKEY_CURRENT_USER\Software\Policies\2FAGuard";

        private static readonly Lazy<JsonElement?> policies = new(LoadPolicies);

        private static JsonElement? LoadPolicies()
        {
            try
            {
                if (!File.Exists(PolicyFilePath))
                {
                    return null;
                }
                using JsonDocument doc = JsonDocument.Parse(
                    File.ReadAllText(PolicyFilePath),
                    new JsonDocumentOptions
                    {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                    }
                );
                return doc.RootElement.Clone();
            }
            catch
            {
                return null;
            }
        }

        private static JsonElement? Find(string keyPath, string value)
        {
            if (policies.Value is not JsonElement root)
            {
                return null;
            }
            if (!keyPath.StartsWith(RootKey, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            JsonElement current = root;
            string[] segments = keyPath[RootKey.Length..]
                .Split('\\', StringSplitOptions.RemoveEmptyEntries)
                .Append(value)
                .ToArray();
            foreach (string segment in segments)
            {
                if (current.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }
                JsonElement? next = null;
                foreach (JsonProperty property in current.EnumerateObject())
                {
                    if (property.Name.Equals(segment, StringComparison.OrdinalIgnoreCase))
                    {
                        next = property.Value;
                        break;
                    }
                }
                if (next == null)
                {
                    return null;
                }
                current = next.Value;
            }
            return current;
        }

        private static T? GetValue<T>(string keyPath, string value, T? defaultValue)
        {
            try
            {
                JsonElement? element = Find(keyPath, value);
                if (element == null)
                {
                    return defaultValue;
                }
                if (typeof(T) == typeof(string) && element.Value.ValueKind == JsonValueKind.String)
                {
                    return (T?)(object?)element.Value.GetString();
                }
                if (typeof(T) == typeof(int) && element.Value.ValueKind == JsonValueKind.Number)
                {
                    return (T)(object)element.Value.GetInt32();
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private static bool GetValue(string keyPath, string value, bool defaultValue)
        {
            try
            {
                JsonElement? element = Find(keyPath, value);
                return element?.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number => element.Value.GetInt32() != 0,
                    _ => defaultValue,
                };
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
