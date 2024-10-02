# 2FAGuard - TOTP Authenticator

A modern and secure Windows app for managing your 2FA authentification codes.

![2FAGuard Windows App](https://2faguard.app/img/github-readme.png)

## Features

- ↕️ **Import & Export**: You have full control over your data. Import your existing tokens from other apps or export your data to use elsewhere.
- 🛡️ **Secure by Design**: Your data is encrypted using the latest security standards and stored securely on your device.
- 🔒 **Supports Windows Hello**: Use your fingerprint, face or hardware security key to access your tokens quickly and securely.
- 💡 **Dark Mode**: A modern and sleek design that looks great in both light and dark mode.
- 📦 **Portable Version**: You can use 2FAGuard as a portable application without installation, e.g. on a USB stick
- 🌍 **Multilingual**: 2FAGuard is currently available in English, German, French, Italian and Chinese.

## Download

You can download the latest version of 2FAGuard [from the official website](https://2faguard.app#download) or directly from the Releases section of this repository. The app is also available on the Microsoft Store and supports Windows 10 (1903+) and 11.

## FAQ

**How can I add a token?**
You can import QR codes or enter the token details manually. Furthermore 2FAGuard supports importing multiple tokens at once from other 2FA apps. All tokens that follow the TOTP standard are supported.

**How is my data secured?**
The secret keys, the account names and your notes are encrypted using the AEGIS-256 algorithm. The encryption key is derived via Argon2id.

**From which 2FA apps can I import tokens?**
Tokens can be imported from generic TOTP Uri lists. In addition Google Authenticator, Bitwarden, Authenticator Pro, Aegis Authenticator and 2FAS are currently explicitly supported.

**Which devices are supported?**
This app is only available for Windows 10 18362.0 and higher. For Android I recommend [Authenticator Pro](https://github.com/jamie-mh/AuthenticatorPro) that is fully compatible with 2FAGuard.

## Contact

If a public GitHub issue or discussion is not the right choice for your concern, you can contact me directly:

- E-Mail: [info@2faguard.app](mailto:info@2faguard.app)

## License

© [Timo Kössler](https://timokoessler.de) 2024  
Released under the [MIT license](https://github.com/timokoessler/totp-token-guard/blob/main/LICENSE)
