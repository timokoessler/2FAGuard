# Security Policy

## Reporting a Vulnerability

I take all security issues seriously. I appreciate your efforts and responsible disclosure and will make every effort to acknowledge your contributions.

To report a vulnerability, please email me at [info@timokoessler.de](mailto:info@timokoessler.de) with the full details, including steps to reproduce the issue. You can use my [PGP key](https://timokoessler.de/pgp-key.txt) or my [S/MIME key](https://timokoessler.de/smime.txt) to encrypt the email. Please write the email in English or German.

I will check the vulnerability as soon as possible and get back to you within 48 hours. If the vulnerability is accepted, I will publish a security advisory and release a patch as soon as possible. Please do not disclose the vulnerability until I have published a security advisory. I will give you credit for your responsible disclosure in the advisory.

## Secure Distribution

To ensure the security and integrity of the software, always download it from official sources such as the project's GitHub releases or the official website. Verify the authenticity of the downloaded files by checking their signatures. Avoid downloading the software from third-party sources, as they may contain modified or malicious versions. If you notice any suspicious downloads or unauthorized distributions, please report them immediately.

The following websites are the official download sources for this project:

- The GitHub Releases page of this repository: [github.com/timokoessler/2FAGuard/releases](https://github.com/timokoessler/2FAGuard/releases)
- The official website: [2faguard.app](https://2faguard.app)
- The Microsoft Store: [https://apps.microsoft.com/detail/9p6hr4gszjrm](https://apps.microsoft.com/detail/9p6hr4gszjrm)
- The Winget package `timokoessler.2FAGuard`

For debugging purposes I sometimes provide test builds via GitHub Issues or via email. These builds are always downloaded from the 2faguard.app domain and are also signed with a code signing certificate.

All releases except for the Microsoft Store app are signed with a code signing certificate. The following certificates are used for signing:

| Subject                             | Certificate Authority | Valid from | Valid to   | Fingerprint                                                 |
| ----------------------------------- | --------------------- | ---------- | ---------- | ----------------------------------------------------------- |
| Open Source Developer, Timo Kössler | Certum                | 2026-02-01 | 2027-02-01 | 43:E9:DD:4D:4A:06:85:3C:BB:52:1E:A3:5E:5F:33:7E:DB:0D:BC:CD |
| Open Source Developer, Timo Kössler | Certum                | 2025-02-22 | 2026-02-22 | 08:39:62:6A:85:8F:4D:2E:44:ED:C9:97:08:36:26:09:E4:32:DA:5A |
| Open Source Developer, Timo Kössler | Certum                | 2024-03-28 | 2025-03-28 | B6:50:88:6E:28:A6:85:FD:84:0E:3D:AD:97:74:63:69:A6:A8:F7:09 |
