# Advanced

## Registry Keys

Administrators can configure some special settings of the application by using the following registry keys. These keys are intended for advanced users and usage in corporate environments. They are not required for normal operation of the application.

All of the registry keys are located in the following path:

```
HKEY_CURRENT_USER\Software\Policies\2FAGuard
```

A group policy ADMX template provided by a user from the community can be downloaded [here](https://2faguard.app/static-content/2FAGuard-ADMX-Template.zip).

### Modify app data path

Create a value named `AppDataPath` in the path above. The value should be of type `REG_SZ` and contain the path to the folder where the application data will be stored. It's possible to use environment variables in the path, e.g. `%APPDATA%` or `%USERNAME%`.

> The path must be an absolute path and accessible by the user running the application. Make sure that the same path is **never** used by two different users! This key is ignored by the portable version of the application and the Microsoft Store app.

The default data paths are:

- Installer version: `%LOCALAPPDATA%\2FAGuard`
- Portable version: `.\2FAGuard-Data`

### Prevent unencrypted exports

Create a value named `PreventUnencryptedExports` in the path above. The value should be of type `DWORD`. If set to `1`, the application will not allow unencrypted exports of the secrets. The default is `0`.

### Password requirements

Create a subkey named `Password` in the path specified above. The following values can be set:

- `RequireLowerAndUpperCase` (DWORD): Requires at least one lowercase and one uppercase letter in the password. The default is `0`.
- `RequireDigits` (DWORD): Requires at least one digit in the password. The default is `0`.
- `RequireSpecialChars` (DWORD): Requires at least one special character in the password. The default is `0`.
- `MinLength` (DWORD): The minimum length of the password. The default is `8`. Tip: Select decimal as the base for the value in the registry editor when entering the value.

Please note that the password requirements are only enforced when creating a new password. If you change the requirements after a password has been set, the new requirements will not be enforced for the existing password.

### Modify setup

Create a subkey named `Setup` in the path specified above. The following values can be set:

- `HideSkip` (DWORD): If set to `1`, the skip button on the welcome screen will be hidden, preventing users from using the insecure mode without authentication. Default is `0`.
- `HideWinHello` (DWORD): If set to `1`, the Windows Hello button on the welcome screen will be hidden. The default is `0`.
- `HidePassword` (DWORD): If set to `1`, the password button on the welcome screen will be hidden. The default is `0`.

If two of the above keys are set to `1`, the welcome screen will not be displayed and the only remaining option will be selected automatically. If all three keys are set to `1`, all options will be shown like default.

### Modify settings page

Create a subkey named `Settings` in the path specified above. These keys can be helpful if these features are not available in a server environment. The following values can be set:

- `HideWinHello` (DWORD): If set to `1`, the settings page will not show the Windows Hello settings. The default is `0`.
- `HidePreventRecording` (DWORD): The settings page will not show the option to prevent screen recording if set to `1`. The default is `0`.
- `HideSecurityKey` (DWORD): If set to `1`, the settings page will not show the security key (WebAuthn / FIDO2) settings. The default is `0`.
