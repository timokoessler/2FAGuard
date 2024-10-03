# 2FAGuard - TOTP Authenticator

A modern and secure Windows app for managing your 2FA authentification codes.

![2FAGuard Windows App](https://2faguard.app/img/github-readme.png)

## Features

- 🛡️ **Secure by Design**: Your data is encrypted using the latest security standards and stored securely on your device.
- ↕️ **Import & Export**: Your data, your rules. Import existing tokens from other apps or export your data to use elsewhere.
- 🔒 **Supports Windows Hello**: Security made simple. Use your fingerprint or face to access your tokens quickly and securely.
- 💡 **Dark Mode**: A modern and sleek design that looks great in both light and dark mode.
- 📦 **Portable Version**: You can use 2FAGuard as a portable application without installation, e.g. on a USB stick
- 🌍 **Multilingual**: 2FAGuard is currently available in English, German, French, Italian and Chinese.
- ⚙️ **Customizable**: Auto-lock, autostart or minimize to the background - the app adapts to your needs.

## Download

You can download the latest version of 2FAGuard [from the official website](https://2faguard.app#download) or directly from the Releases section of this repository. The app is also available on the [Microsoft Store](https://apps.microsoft.com/detail/9p6hr4gszjrm) and supports Windows 10 (1903+) and 11.

## FAQ

**How can I add a token?**
There are many ways to add a token. You can import a QR code, enter the token details manually or import multiple tokens from other 2FA apps. All tokens following the TOTP standard are supported.

**How is my data secured?**
The secret keys, the account names and your notes are encrypted using the AEGIS-256 algorithm. The encryption key is derived via Argon2id.

**Can I export my tokens?**
Yes, you can export your tokens at any time. Currently you have three options: encrypted 2FAGuard backup, generic TOTP Uri list or Authenticator Pro backup. But it is also possible to export single tokens as QR codes or view the secret key.

**Which devices are supported?**
This app is only available for Windows 10 1903 and higher. For Android I recommend [Authenticator Pro](https://github.com/jamie-mh/AuthenticatorPro) that is fully compatible with 2FAGuard. It also requires the C++ Redistributable Runtimes by Microsoft to be installed, but they are usually already installed on most systems. If not, the installer will take care of it.

**What can I do if I lost my login data?**
If you lost your password and cannot use an additional login method like Windows Hello, you will no longer have access to your tokens. If you want to reset the app and start over, you can delete the app data. Note that this will delete all your tokens and settings. For the Windows Store version, you can delete the app data in the Windows settings under Apps -> 2FAGuard -> Advanced Options -> Reset. If you are using the classic installation, delete the directory C:\Users\%username%\AppData\Local\2FAGuard.

## Contact

If a public GitHub issue or discussion is not the right choice for your concern, you can contact me directly via email: [info@2faguard.app](mailto:info@2faguard.app). If you want to encrypt your message, take a look at the [imprint](https://2faguard.app/imprint) for my PGP and S/MIME keys.

## License

© [Timo Kössler](https://timokoessler.de) 2024  
Released under the [MIT license](https://github.com/timokoessler/totp-token-guard/blob/main/LICENSE)
