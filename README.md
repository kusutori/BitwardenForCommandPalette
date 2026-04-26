# Bitwarden For Command Palette

[简体中文文档 (Chinese README)](./README.zh-CN.md)

A Bitwarden extension for **Windows PowerToys Command Palette**. It interacts with your vault through the local Bitwarden CLI (`bw`).

[📖 Changelog](docs/CHANGELOG.md) · [🏗️ Architecture](docs/ARCHITECTURE.md) · [📘 Dev Guide](docs/Project-Architecture.md) · [🔧 Troubleshooting](docs/Troubleshooting.md)

## Features

### Implemented ✅

- Vault status detection (CLI installed/login state)
- Vault unlock, manual sync, and lock
- List/search items (by name, username, URL)
- Favorites filter + favorites pinned first
- Folder/type filters and Trash support
- Item types: Login, Card, Identity, Secure Note
- TOTP generation/copy with live countdown page
- Custom fields display/copy
- CRUD support:
  - Create item (Login/Card/Identity/Secure Note)
  - Create folder
  - Edit existing item
  - Delete to Trash / Restore / Permanently delete
- Password generator:
  - Random password generator (length + charset options)
  - Passphrase generator (word count/separator/capitalization/number options)
- Two-pane details panel with rich item details
- Multi-language UI support (including English and Simplified Chinese)
- Settings page in Command Palette:
  - Custom Bitwarden CLI path
  - API Key auth (`Client ID` / `Client Secret`)
  - Custom environment variables (e.g. self-hosted server)

### Planned 📋

- Auto lock after inactivity
- Custom keyboard shortcut configuration
- Theme-aware icons (dark/light)
- Password health report
- Attachment support

## Prerequisites

1. **Windows 11** (or a Windows version that supports PowerToys Command Palette)
2. **PowerToys** installed with Command Palette enabled
3. **Bitwarden CLI** (`bw`) installed and available in `PATH`
   ```bash
   # Install with winget
   winget install Bitwarden.CLI

   # Or install with npm
   npm install -g @bitwarden/cli
   ```
4. Logged in to Bitwarden at least once
   ```bash
   bw login
   ```

## Installation

1. Clone or download this repository.
2. Open `BitwardenForCommandPalette.sln` in Visual Studio 2022.
3. Select the target platform (`x64` or `ARM64`).
4. Right click the project and choose **Deploy**.
5. Run `Reload` in Command Palette.

## Usage

### Basic flow

1. Open PowerToys Command Palette (default: `Alt + Space`).
2. Open **Bitwarden For Command Palette**.
3. If the vault is locked, open the unlock page and enter your master password.
4. Browse items, search by typing, and press `Enter` to run primary actions.

### Configure extension settings

In **PowerToys Settings → Command Palette → Bitwarden For Command Palette**:

- **Bitwarden CLI Path**: custom `bw.exe` path (default uses `bw` from `PATH`)
- **Bitwarden API Client ID**: optional
- **Bitwarden API Client Secret**: optional
- **Custom Environment Variables**:
  - `BW_SERVER` for self-hosted Bitwarden
  - `NODE_EXTRA_CA_CERTS` for custom certificates
  - any other env var supported by Bitwarden CLI
  - format: `KEY1=VALUE1;KEY2=VALUE2`

### API Key authentication (recommended)

To avoid signing in again after restarts:

1. Go to Bitwarden Web Vault → **Settings → Security → Keys**.
2. Create/view your API key.
3. Copy `client_id` and `client_secret`.
4. Paste them into extension settings:
   - `Bitwarden API Client ID`
   - `Bitwarden API Client Secret`
5. The extension maps them to `BW_CLIENTID` and `BW_CLIENTSECRET`.

Notes:
- If both API Key and `bw login` are present, API Key takes priority.
- API Key authenticates login only; vault unlock still requires master password.

## Keyboard Shortcuts

### Global/common

| Shortcut | Action |
|---|---|
| `Enter` | Primary action |
| `Ctrl+Enter` | Secondary action |
| `Ctrl+E` | Edit item |
| `Ctrl+Delete` | Move item to Trash |
| `Ctrl+R` | Sync vault |
| `Ctrl+L` | Lock vault |
| `Ctrl+Shift+A` | Create item |

### Login item

| Shortcut | Action |
|---|---|
| `Enter` / `Ctrl+P` | Copy password |
| `Ctrl+U` | Copy username |
| `Ctrl+T` | Copy TOTP code |
| `Ctrl+O` | Open URL |
| `Ctrl+Shift+C` | Copy URL |

### Card item

| Shortcut | Action |
|---|---|
| `Enter` / `Ctrl+Shift+N` | Copy card number |
| `Ctrl+V` | Copy CVV |
| `Ctrl+X` | Copy expiration |
| `Ctrl+N` | Copy cardholder name |

### Identity item

| Shortcut | Action |
|---|---|
| `Enter` / `Ctrl+N` | Copy full name |
| `Ctrl+M` | Copy email |
| `Ctrl+P` | Copy phone |
| `Ctrl+A` | Copy address |
| `Ctrl+U` | Copy username |
| `Ctrl+S` | Copy SSN |

### Trash

| Shortcut | Action |
|---|---|
| `Ctrl+Shift+Delete` | Permanently delete |

## Contributing

Issues and PRs are welcome.

## Acknowledgements

- [Microsoft PowerToys](https://github.com/microsoft/PowerToys) - Command Palette extension framework
- [Bitwarden](https://bitwarden.com/) - Open source password manager
- [bitwarden-cli-bio](https://github.com/jeanregisser/bitwarden-cli-bio) - Bitwarden CLI with biometric unlock

**License notice**: This project references the third-party projects above. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for details.

## License

MIT License. See [LICENSE](LICENSE).
