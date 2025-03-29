using Guard.Core;

namespace Guard.WPF.Core.Security
{
    internal static class PasswordComplexity
    {
        public static (bool ok, string? error) CheckPassword(string password)
        {
            (
                bool requireLowerAndUpperCase,
                bool requireDigit,
                bool requireSpecialChar,
                int minLength
            ) = RegistrySettings.GetPasswordOptions();

            if (password.Length < minLength)
            {
                return (
                    false,
                    I18n.GetString("i.pass.invalid.minlength")
                        .Replace("@Length", minLength.ToString())
                );
            }

            if (password.Length > 128)
            {
                return (false, I18n.GetString("i.pass.invalid.maxlength"));
            }

            if (requireLowerAndUpperCase)
            {
                if (!password.Any(c => char.IsLower(c)) || !password.Any(c => char.IsUpper(c)))
                {
                    return (false, I18n.GetString("i.pass.invalid.case"));
                }
            }

            if (requireDigit)
            {
                if (!password.Any(c => char.IsDigit(c)))
                {
                    return (false, I18n.GetString("i.pass.invalid.digit"));
                }
            }

            if (requireSpecialChar)
            {
                if (!password.Any(c => !char.IsLetterOrDigit(c)))
                {
                    return (false, I18n.GetString("i.pass.invalid.special"));
                }
            }

            return (true, null);
        }
    }
}
