# Advanced

## Registry Keys

Administrators can configure some special settings of the application by using the following registry keys. These keys are intended for advanced users and usage in corporate environments. They are not required for normal operation of the application.

All of the registry keys are located in the following path:

```
HKEY_CURRENT_USER\Software\Policies\2FAGuard
```

A group policy ADMX template provided by a user from the community can be downloaded [here](https://2faguard.app/static-content/2FAGuard-ADMX-Template.zip).
Please note that the ADMX template is currently out of date, but you can find all options on this page.

### Modify app data path

Create a value named `AppDataPath` in the path above. The value should be of type `REG_SZ` and contain the path to the folder where the application data will be stored. It's possible to use environment variables in the path, e.g. `%APPDATA%` or `%USERNAME%`.

> The path must be an absolute path and accessible by the user running the application. Make sure that the same path is **never** used by two different users! This key is ignored by the portable version of the application and the Microsoft Store app.

The default data paths are:

- Installer version: `%LOCALAPPDATA%\2FAGuard`
- Portable version: `.\2FAGuard-Data`

### Password requirements

Create a subkey named `Password` in the path specified above. The following values can be set:

- `RequireLowerAndUpperCase` (DWORD): Requires at least one lowercase and one uppercase letter in the password. The default is `0`.
- `RequireDigits` (DWORD): Requires at least one digit in the password. The default is `0`.
- `RequireSpecialChars` (DWORD): Requires at least one special character in the password. The default is `0`.
- `MinLength` (DWORD): The minimum length of the password. The default is `8`. Tip: Select decimal as the base for the value in the registry editor when entering the value.

Please note that the password requirements are only enforced when creating a new password. If you change the requirements after a password has been set, the new requirements will not be enforced for the existing password.

### Prevent unencrypted exports

Create a value named `PreventUnencryptedExports` in the path above. The value should be of type `DWORD`. If set to `1`, the application will not allow unencrypted exports of the secrets. The default is `0`.

### Disable automatic updates

Create a value named `DisableAutoUpdate` in the path above. The value should be of type `DWORD`. If set to `1`, the application will not check for updates automatically. The default is `0`.
Only disable automatic updates if you have an alternative update mechanism in place.

### Disable Screen Recording Protection (v1.6.2+)

Create a value named `DisableScreenRecordingProtection` in the path above. The value should be of type `DWORD`. If set to `1`, the screen recording protection feature is disabled and the user will not be able to modify this setting. The default is `0`. This setting might be helpful for usage in RDP environments.

### Modify setup UI

Create a subkey named `Setup` in the path specified above. The following values can be set:

- `HideSkip` (DWORD): If set to `1`, the skip button on the welcome screen will be hidden, preventing users from using the insecure mode without authentication. Default is `0`.
- `HideWinHello` (DWORD): If set to `1`, the Windows Hello button on the welcome screen will be hidden. The default is `0`.
- `HidePassword` (DWORD): If set to `1`, the password button on the welcome screen will be hidden. The default is `0`.

If two of the above keys are set to `1`, the welcome screen will not be displayed and the only remaining option will be selected automatically. If all three keys are set to `1`, all options will be shown like default.

### Modify settings

Create a subkey named `Settings` in the path specified above. These keys can be helpful if these features are not available in a server environment. The following values can be set:

- `HideWinHello` (DWORD): If set to `1`, the settings page will not show the Windows Hello settings. The default is `0`.
- `HidePreventRecording` (DWORD): The settings page will not show the option to prevent screen recording if set to `1`. The default is `0`.
- `HideSecurityKey` (DWORD): If set to `1`, the settings page will not show the security key (WebAuthn / FIDO2) settings. The default is `0`.
- `ForceLockOnScreenLock` (DWORD): If set to `1`, the application will always lock when the screen is locked. The default is `0`. The setting allows enforcing the automatic locking feature and prevents users from changing this setting. Available since v1.6.3.
- `ForceLockTimeSetting` (DWORD): Enforce locking of the application after a specific time of inactivity. This setting overrides the user's setting. The value should be set to the number of the option available in the dropdown menu on the settings page starting with `0` for "Never". Invalid values will be ignored. Available since v1.6.3.

## Installer Command Line Options

The installer supports the following command line options. These can **not** be used with the Portable version or the Microsoft Store app.

- `/NOSTART` - Prevents the application from starting automatically after installation.
- `/SP- /VERYSILENT /SUPPRESSMSGBOXES /NORESTART` - Silent installation without progress bar. Suppresses all message boxes.
- `/SP- /SILENT /SUPPRESSMSGBOXES /NORESTART` - Silent installation with progress bar. Suppresses all message boxes.

All options except `/NOSTART` are standard Inno Setup options. More information about Inno Setup options can be found [here](https://jrsoftware.org/ishelp/index.php?topic=setupcmdline).
