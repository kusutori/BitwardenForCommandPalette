# Bitwarden CLI (bw) 使用文档

> 基于 BitwardenForCommandPalette 项目实践总结的 Bitwarden CLI 使用指南

## 目录

- [概述](#概述)
- [安装](#安装)
- [认证与会话管理](#认证与会话管理)
- [核心命令](#核心命令)
- [数据结构](#数据结构)
- [在程序中集成](#在程序中集成)
- [最佳实践](#最佳实践)

---

## 概述

Bitwarden CLI (`bw`) 是 Bitwarden 密码管理器的命令行工具，提供完整的密码库管理功能。它支持 Windows、macOS 和 Linux 平台。

### 主要功能

- 登录/登出账户
- 解锁/锁定密码库
- 列出、搜索、获取密码项
- 创建、编辑、删除密码项
- 同步密码库
- 生成密码
- 导入/导出数据

---

## 安装

### Windows

```powershell
# 使用 winget
winget install Bitwarden.CLI

# 使用 npm
npm install -g @bitwarden/cli

# 使用 Chocolatey
choco install bitwarden-cli
```

### macOS

```bash
# 使用 Homebrew
brew install bitwarden-cli

# 使用 npm
npm install -g @bitwarden/cli
```

### Linux

```bash
# 使用 npm
npm install -g @bitwarden/cli

# 使用 Snap
snap install bw

# 使用 Flatpak
flatpak install flathub com.bitwarden.cli
```

### 验证安装

```bash
bw --version
```

---

## 认证与会话管理

### 登录流程

```bash
# 交互式登录（推荐用于手动操作）
bw login

# 使用邮箱和密码登录
bw login email@example.com password123

# 使用 API Key 登录（推荐用于自动化）
bw login --apikey

# 使用 SSO 登录
bw login --sso
```

### 解锁密码库

登录后，密码库处于锁定状态。需要解锁才能访问数据：

```bash
# 交互式解锁
bw unlock

# 使用密码解锁（返回会话密钥）
bw unlock "master_password"

# 使用 --raw 只返回会话密钥
bw unlock "master_password" --raw

# 使用环境变量中的密码
bw unlock --passwordenv BW_PASSWORD

# 使用文件中的密码
bw unlock --passwordfile /path/to/password.txt
```

### 会话密钥管理

解锁后会返回一个会话密钥（Session Key），后续操作需要使用这个密钥：

```bash
# 方式1：设置环境变量
export BW_SESSION="your_session_key"
bw list items  # 自动使用环境变量

# 方式2：每次命令传递
bw list items --session "your_session_key"
```

### 锁定与登出

```bash
# 锁定密码库（保留登录状态，清除会话密钥）
bw lock

# 登出（完全退出账户）
bw logout
```

### 检查状态

```bash
bw status
```

返回 JSON 格式：

```json
{
  "serverUrl": "https://vault.bitwarden.com",
  "lastSync": "2024-01-15T10:30:00.000Z",
  "userEmail": "user@example.com",
  "userId": "00000000-0000-0000-0000-000000000000",
  "status": "unlocked"  // 可能值: "unlocked", "locked", "unauthenticated"
}
```

---

## 核心命令

### list - 列出对象

```bash
# 列出所有项目
bw list items --session "SESSION_KEY"

# 搜索项目
bw list items --search "google" --session "SESSION_KEY"

# 按文件夹筛选
bw list items --folderid "FOLDER_ID" --session "SESSION_KEY"

# 按集合筛选
bw list items --collectionid "COLLECTION_ID" --session "SESSION_KEY"

# 按 URL 筛选
bw list items --url "https://google.com" --session "SESSION_KEY"

# 列出已删除项目
bw list items --trash --session "SESSION_KEY"

# 列出无文件夹的项目
bw list items --folderid null --session "SESSION_KEY"

# 列出文件夹
bw list folders --session "SESSION_KEY"

# 列出集合
bw list collections --session "SESSION_KEY"

# 列出组织
bw list organizations --session "SESSION_KEY"
```

### get - 获取单个对象

```bash
# 获取项目（通过 ID）
bw get item "ITEM_ID" --session "SESSION_KEY"

# 获取项目（通过搜索词）
bw get item "google" --session "SESSION_KEY"

# 直接获取密码
bw get password "google" --session "SESSION_KEY"

# 直接获取用户名
bw get username "google" --session "SESSION_KEY"

# 获取 URI
bw get uri "google" --session "SESSION_KEY"

# 获取 TOTP 验证码（重要！）
bw get totp "ITEM_ID" --session "SESSION_KEY"

# 获取笔记
bw get notes "ITEM_ID" --session "SESSION_KEY"

# 获取文件夹
bw get folder "FOLDER_ID" --session "SESSION_KEY"

# 获取模板（用于创建）
bw get template item
bw get template item.login
bw get template item.card
bw get template item.identity
bw get template folder
```

### sync - 同步

```bash
# 同步密码库
bw sync --session "SESSION_KEY"

# 获取上次同步时间
bw sync --last --session "SESSION_KEY"
```

### create - 创建对象

```bash
# 创建文件夹
echo '{"name":"My Folder"}' | bw encode | bw create folder --session "SESSION_KEY"

# 创建登录项
bw get template item | jq '.name="My Login" | .login.username="user" | .login.password="pass"' | bw encode | bw create item --session "SESSION_KEY"

# 创建银行卡
bw get template item | jq '.type=3 | .name="My Card" | .card.cardholderName="John Doe"' | bw encode | bw create item --session "SESSION_KEY"
```

### edit - 编辑对象

```bash
# 编辑项目
bw get item "ITEM_ID" --session "SESSION_KEY" | jq '.login.password="newpassword"' | bw encode | bw edit item "ITEM_ID" --session "SESSION_KEY"

# 编辑文件夹
bw get folder "FOLDER_ID" --session "SESSION_KEY" | jq '.name="New Name"' | bw encode | bw edit folder "FOLDER_ID" --session "SESSION_KEY"
```

### delete - 删除对象

```bash
# 删除项目（移到回收站）
bw delete item "ITEM_ID" --session "SESSION_KEY"

# 永久删除
bw delete item "ITEM_ID" --permanent --session "SESSION_KEY"

# 删除文件夹
bw delete folder "FOLDER_ID" --session "SESSION_KEY"
```

### restore - 恢复对象

```bash
# 从回收站恢复
bw restore item "ITEM_ID" --session "SESSION_KEY"
```

### generate - 生成密码

```bash
# 生成默认密码（14位，含大小写和数字）
bw generate

# 生成复杂密码
bw generate -ulns --length 20

# 生成密码短语
bw generate --passphrase --words 4 --separator "-"

# 选项说明：
# -u, --uppercase  包含大写字母
# -l, --lowercase  包含小写字母
# -n, --number     包含数字
# -s, --special    包含特殊字符
# --length         密码长度
```

---

## 数据结构

### 项目类型 (type)

| 类型 | 值 | 说明 |
|------|---|------|
| Login | 1 | 登录凭据 |
| Secure Note | 2 | 安全笔记 |
| Card | 3 | 银行卡 |
| Identity | 4 | 身份信息 |

### 项目 JSON 结构

```json
{
  "object": "item",
  "id": "uuid",
  "organizationId": null,
  "folderId": "uuid or null",
  "type": 1,
  "reprompt": 0,
  "name": "Item Name",
  "notes": "Optional notes",
  "favorite": false,
  "login": {
    "uris": [
      {
        "match": null,
        "uri": "https://example.com"
      }
    ],
    "username": "user@example.com",
    "password": "secret123",
    "totp": "otpauth://totp/...",
    "passwordRevisionDate": "2024-01-01T00:00:00.000Z"
  },
  "collectionIds": [],
  "revisionDate": "2024-01-01T00:00:00.000Z",
  "creationDate": "2024-01-01T00:00:00.000Z",
  "deletedDate": null
}
```

### 银行卡 (Card) 结构

```json
{
  "type": 3,
  "card": {
    "cardholderName": "John Doe",
    "brand": "Visa",
    "number": "4111111111111111",
    "expMonth": "12",
    "expYear": "2025",
    "code": "123"
  }
}
```

### 身份 (Identity) 结构

```json
{
  "type": 4,
  "identity": {
    "title": "Mr",
    "firstName": "John",
    "middleName": null,
    "lastName": "Doe",
    "address1": "123 Main St",
    "address2": null,
    "address3": null,
    "city": "New York",
    "state": "NY",
    "postalCode": "10001",
    "country": "US",
    "company": "Acme Inc",
    "email": "john@example.com",
    "phone": "+1-555-1234",
    "ssn": "123-45-6789",
    "username": "johndoe",
    "passportNumber": "AB1234567",
    "licenseNumber": "DL12345"
  }
}
```

### 自定义字段 (Fields) 结构

```json
{
  "fields": [
    {
      "name": "Field Name",
      "value": "Field Value",
      "type": 0,
      "linkedId": null
    }
  ]
}
```

字段类型：
| 类型 | 值 | 说明 |
|------|---|------|
| Text | 0 | 明文 |
| Hidden | 1 | 隐藏 |
| Boolean | 2 | 布尔值 |
| Linked | 3 | 链接字段 |

### 文件夹结构

```json
{
  "object": "folder",
  "id": "uuid",
  "name": "Folder Name"
}
```

### 状态结构

```json
{
  "serverUrl": "https://vault.bitwarden.com",
  "lastSync": "2024-01-15T10:30:00.000Z",
  "userEmail": "user@example.com",
  "userId": "uuid",
  "status": "unlocked"
}
```

---

## 在程序中集成

### C# 集成示例

```csharp
using System.Diagnostics;
using System.Text;
using System.Text.Json;

public class BitwardenCliService
{
    private string? _sessionKey;
    
    /// <summary>
    /// 执行 bw 命令
    /// </summary>
    private async Task<(string output, string error, int exitCode)> ExecuteCommandAsync(string arguments)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "bw",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = processInfo };
        
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        return (output.ToString().Trim(), error.ToString().Trim(), process.ExitCode);
    }

    /// <summary>
    /// 获取状态
    /// </summary>
    public async Task<BitwardenStatus?> GetStatusAsync()
    {
        var (output, _, exitCode) = await ExecuteCommandAsync("status");
        if (exitCode != 0) return null;
        return JsonSerializer.Deserialize<BitwardenStatus>(output);
    }

    /// <summary>
    /// 解锁密码库
    /// </summary>
    public async Task<bool> UnlockAsync(string masterPassword)
    {
        var escaped = masterPassword.Replace("\"", "\\\"");
        var (output, _, exitCode) = await ExecuteCommandAsync($"unlock \"{escaped}\" --raw");
        
        if (exitCode == 0 && !string.IsNullOrWhiteSpace(output))
        {
            _sessionKey = output.Trim();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取所有项目
    /// </summary>
    public async Task<BitwardenItem[]?> GetItemsAsync()
    {
        if (string.IsNullOrEmpty(_sessionKey)) return null;
        
        var (output, _, exitCode) = await ExecuteCommandAsync($"list items --session \"{_sessionKey}\"");
        if (exitCode != 0) return null;
        
        return JsonSerializer.Deserialize<BitwardenItem[]>(output);
    }

    /// <summary>
    /// 搜索项目
    /// </summary>
    public async Task<BitwardenItem[]?> SearchItemsAsync(string query)
    {
        if (string.IsNullOrEmpty(_sessionKey)) return null;
        
        var escaped = query.Replace("\"", "\\\"");
        var (output, _, exitCode) = await ExecuteCommandAsync(
            $"list items --search \"{escaped}\" --session \"{_sessionKey}\"");
        if (exitCode != 0) return null;
        
        return JsonSerializer.Deserialize<BitwardenItem[]>(output);
    }

    /// <summary>
    /// 获取 TOTP 验证码
    /// </summary>
    public async Task<string?> GetTotpAsync(string itemId)
    {
        if (string.IsNullOrEmpty(_sessionKey)) return null;
        
        var (output, _, exitCode) = await ExecuteCommandAsync(
            $"get totp \"{itemId}\" --session \"{_sessionKey}\"");
        
        return exitCode == 0 ? output.Trim() : null;
    }

    /// <summary>
    /// 同步密码库
    /// </summary>
    public async Task<bool> SyncAsync()
    {
        if (string.IsNullOrEmpty(_sessionKey)) return false;
        var (_, _, exitCode) = await ExecuteCommandAsync($"sync --session \"{_sessionKey}\"");
        return exitCode == 0;
    }

    /// <summary>
    /// 锁定密码库
    /// </summary>
    public async Task<bool> LockAsync()
    {
        var (_, _, exitCode) = await ExecuteCommandAsync("lock");
        if (exitCode == 0) _sessionKey = null;
        return exitCode == 0;
    }
}
```

### Python 集成示例

```python
import subprocess
import json

class BitwardenCli:
    def __init__(self):
        self.session_key = None
    
    def _run_command(self, args: list[str]) -> tuple[str, str, int]:
        """执行 bw 命令"""
        result = subprocess.run(
            ["bw"] + args,
            capture_output=True,
            text=True
        )
        return result.stdout.strip(), result.stderr.strip(), result.returncode
    
    def get_status(self) -> dict | None:
        """获取状态"""
        output, _, code = self._run_command(["status"])
        return json.loads(output) if code == 0 else None
    
    def unlock(self, master_password: str) -> bool:
        """解锁密码库"""
        output, _, code = self._run_command(["unlock", master_password, "--raw"])
        if code == 0 and output:
            self.session_key = output
            return True
        return False
    
    def get_items(self) -> list[dict] | None:
        """获取所有项目"""
        if not self.session_key:
            return None
        output, _, code = self._run_command(["list", "items", "--session", self.session_key])
        return json.loads(output) if code == 0 else None
    
    def get_totp(self, item_id: str) -> str | None:
        """获取 TOTP 验证码"""
        if not self.session_key:
            return None
        output, _, code = self._run_command(["get", "totp", item_id, "--session", self.session_key])
        return output if code == 0 else None
```

---

## 最佳实践

### 安全建议

1. **不要在命令行直接传递密码** - 使用环境变量或文件
2. **会话结束后锁定密码库** - 调用 `bw lock`
3. **保护会话密钥** - 不要记录到日志
4. **使用 API Key 进行自动化** - 而不是主密码

### 性能优化

1. **缓存项目列表** - 避免频繁调用 `bw list items`
2. **使用 `--raw` 选项** - 减少解析开销
3. **批量操作时使用 `bw serve`** - 启动 REST API 服务器

### 错误处理

```csharp
// 检查退出码
if (exitCode != 0)
{
    // 常见错误：
    // - 密码错误
    // - 会话过期
    // - 网络问题
    // - CLI 未安装
}

// 检查状态
var status = await GetStatusAsync();
if (status?.Status == "unauthenticated")
{
    // 需要登录
}
else if (status?.Status == "locked")
{
    // 需要解锁
}
```

### 配置自托管服务器

```bash
# 配置服务器地址
bw config server https://your-bitwarden-server.com

# 配置 EU 服务器
bw config server https://vault.bitwarden.eu
```

---

## 全局选项

| 选项 | 说明 |
|------|------|
| `--pretty` | 格式化 JSON 输出 |
| `--raw` | 只返回原始值 |
| `--response` | 返回标准响应格式 |
| `--quiet` | 静默模式 |
| `--nointeraction` | 禁用交互提示 |
| `--session <key>` | 指定会话密钥 |
| `-v, --version` | 显示版本 |
| `-h, --help` | 显示帮助 |

---

## 参考链接

- [官方文档](https://bitwarden.com/help/cli/)
- [GitHub 仓库](https://github.com/bitwarden/clients)
- [API 文档](https://bitwarden.com/help/vault-management-api/)
