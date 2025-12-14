# BitwardenForCommandPalette 项目架构文档

> 项目整体架构、模块组织与交互关系

## 目录

- [项目概述](#项目概述)
- [架构图](#架构图)
- [目录结构](#目录结构)
- [核心模块](#核心模块)
- [数据流](#数据流)
- [模块详解](#模块详解)
- [扩展指南](#扩展指南)

---

## 项目概述

BitwardenForCommandPalette 是一个 PowerToys Command Palette 扩展，通过本地 Bitwarden CLI (`bw`) 与 Bitwarden 密码库交互，提供快速访问和管理密码的能力。

### 技术栈

| 组件 | 技术 |
|------|------|
| 框架 | .NET 9.0 |
| 平台 | Windows 10.0.26100.0 |
| 打包 | MSIX |
| 扩展 SDK | Microsoft.CommandPalette.Extensions |
| 密码管理 | Bitwarden CLI |
| JSON 序列化 | System.Text.Json (Source Generator) |

### 核心功能

- 密码库状态检测与解锁
- 项目列表展示与搜索
- 多类型项目支持（登录、银行卡、身份、安全笔记）
- 复制凭据到剪贴板
- TOTP 验证码生成
- 筛选功能（收藏、文件夹、类型）
- 密码库同步与锁定

---

## 架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                     PowerToys Command Palette                     │
└─────────────────────────────────────────────────────────────────┘
                                │
                                │ COM / WinRT
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Extension Entry Point                        │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  BitwardenForCommandPalette.cs (IExtension)                 │  │
│  │  └── BitwardenForCommandPaletteCommandsProvider.cs          │  │
│  └─────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                           Pages Layer                             │
│  ┌──────────────────┐  ┌──────────────────┐  ┌────────────────┐  │
│  │ BitwardenFor...  │  │   UnlockPage     │  │  FilterPage    │  │
│  │ CommandPalette   │  │   (ContentPage)  │  │ (DynamicList)  │  │
│  │ Page             │  │                  │  │                │  │
│  │ (DynamicListPage)│  │  - Form UI       │  │  - Favorites   │  │
│  │                  │  │  - Password      │  │  - Folders     │  │
│  │  - Item List     │  │    Input         │  │  - Types       │  │
│  │  - Search        │  │  - Submit        │  │                │  │
│  │  - Filter        │  │                  │  │                │  │
│  └──────────────────┘  └──────────────────┘  └────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Commands Layer                            │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │                      ItemCommands.cs                          │ │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────────────┐ │ │
│  │  │ Login Cmds  │ │ Card Cmds   │ │ Identity Cmds           │ │ │
│  │  │ - Password  │ │ - Number    │ │ - FullName, Email       │ │ │
│  │  │ - Username  │ │ - CVV       │ │ - Phone, Address        │ │ │
│  │  │ - URL       │ │ - Expiry    │ │ - SSN, Passport, etc.   │ │ │
│  │  │ - TOTP      │ │ - Holder    │ │                         │ │ │
│  │  └─────────────┘ └─────────────┘ └─────────────────────────┘ │ │
│  │  ┌─────────────────────────┐ ┌─────────────────────────────┐ │ │
│  │  │ Vault Commands          │ │ Utility Commands            │ │ │
│  │  │ - SyncVaultCommand      │ │ - CopyNotesCommand          │ │ │
│  │  │ - LockVaultCommand      │ │ - CopyFieldCommand          │ │ │
│  │  └─────────────────────────┘ └─────────────────────────────┘ │ │
│  └──────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Services Layer                            │
│  ┌──────────────────────────────┐  ┌───────────────────────────┐  │
│  │    BitwardenCliService.cs    │  │     IconService.cs        │  │
│  │    (Singleton)               │  │     (Static)              │  │
│  │                              │  │                           │  │
│  │  - GetStatusAsync()          │  │  - GetItemIcon()          │  │
│  │  - UnlockAsync()             │  │  - GetWebsiteIconUrl()    │  │
│  │  - LockAsync()               │  │  - ExtractHostname()      │  │
│  │  - SyncAsync()               │  │  - ExtractDomain()        │  │
│  │  - GetItemsAsync()           │  │                           │  │
│  │  - SearchItemsAsync()        │  └───────────────────────────┘  │
│  │  - GetTotpAsync()            │                                 │
│  │  - GetFoldersAsync()         │                                 │
│  └──────────────────────────────┘                                 │
└─────────────────────────────────────────────────────────────────┘
                                │
                                │ Process.Start("bw", args)
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      External: Bitwarden CLI                      │
│                                                                   │
│  bw status | bw unlock | bw lock | bw sync | bw list | bw get    │
└─────────────────────────────────────────────────────────────────┘
                                │
                                │ HTTPS
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Bitwarden Server                             │
│                  (vault.bitwarden.com / self-hosted)              │
└─────────────────────────────────────────────────────────────────┘
```

---

## 目录结构

```
BitwardenForCommandPalette/
├── BitwardenForCommandPalette.csproj   # 项目配置
├── BitwardenForCommandPalette.cs       # 扩展入口点 (IExtension)
├── BitwardenForCommandPaletteCommandsProvider.cs  # 命令提供者
├── Program.cs                          # COM 服务器启动
├── Package.appxmanifest                # MSIX 清单
├── app.manifest                        # 应用清单
│
├── Commands/                           # 命令模块
│   └── ItemCommands.cs                 # 所有复制/操作命令
│
├── Pages/                              # 页面模块
│   ├── BitwardenForCommandPalettePage.cs  # 主列表页
│   ├── UnlockPage.cs                   # 解锁表单页
│   └── FilterPage.cs                   # 筛选选项页
│
├── Models/                             # 数据模型
│   ├── BitwardenItem.cs                # 密码项模型
│   └── BitwardenStatus.cs              # 状态模型
│
├── Services/                           # 服务层
│   ├── BitwardenCliService.cs          # CLI 封装服务
│   └── IconService.cs                  # 图标服务
│
└── Assets/                             # 资源文件
    ├── app.ico                         # 应用图标
    ├── StoreLogo.png
    ├── Square44x44Logo.scale-200.png
    └── ...
```

---

## 核心模块

### 1. 入口模块

| 文件 | 职责 |
|------|------|
| `BitwardenForCommandPalette.cs` | 实现 `IExtension` 接口，作为 COM 服务器的入口点 |
| `BitwardenForCommandPaletteCommandsProvider.cs` | 继承 `CommandProvider`，定义扩展的顶级命令 |
| `Program.cs` | 启动 COM 服务器，处理 `-RegisterProcessAsComServer` 参数 |

### 2. 页面模块

| 文件 | 类型 | 职责 |
|------|------|------|
| `BitwardenForCommandPalettePage.cs` | `DynamicListPage` | 主页面，显示项目列表、处理搜索、管理筛选 |
| `UnlockPage.cs` | `ContentPage` | 解锁表单，接收主密码输入 |
| `FilterPage.cs` | `DynamicListPage` | 筛选选项页面，选择收藏/文件夹/类型筛选 |

### 3. 命令模块

| 命令类别 | 命令 | 功能 |
|---------|------|------|
| **登录命令** | `CopyPasswordCommand` | 复制密码 |
| | `CopyUsernameCommand` | 复制用户名 |
| | `CopyUrlCommand` | 复制 URL |
| | `OpenUrlCommand` | 打开 URL |
| | `CopyTotpCommand` | 复制 TOTP 验证码 |
| **银行卡命令** | `CopyCardNumberCommand` | 复制卡号 |
| | `CopyCardCvvCommand` | 复制 CVV |
| | `CopyCardExpirationCommand` | 复制有效期 |
| | `CopyCardholderNameCommand` | 复制持卡人姓名 |
| **身份命令** | `CopyFullNameCommand` | 复制姓名 |
| | `CopyEmailCommand` | 复制邮箱 |
| | `CopyPhoneCommand` | 复制电话 |
| | `CopyAddressCommand` | 复制地址 |
| | `CopyCompanyCommand` | 复制公司 |
| | `CopySsnCommand` | 复制 SSN |
| | `CopyPassportCommand` | 复制护照号 |
| | `CopyLicenseCommand` | 复制驾照号 |
| **通用命令** | `CopyNotesCommand` | 复制笔记 |
| | `CopyFieldCommand` | 复制自定义字段 |
| **密码库命令** | `SyncVaultCommand` | 同步密码库 |
| | `LockVaultCommand` | 锁定密码库 |

### 4. 服务模块

| 文件 | 模式 | 职责 |
|------|------|------|
| `BitwardenCliService.cs` | 单例 | 封装 Bitwarden CLI 调用，管理会话状态 |
| `IconService.cs` | 静态 | 生成项目图标，处理网站 favicon |

### 5. 模型模块

| 文件 | 类 | 用途 |
|------|-----|------|
| `BitwardenItem.cs` | `BitwardenItem` | 密码项数据模型 |
| | `BitwardenLogin` | 登录信息 |
| | `BitwardenCard` | 银行卡信息 |
| | `BitwardenIdentity` | 身份信息 |
| | `BitwardenSecureNote` | 安全笔记 |
| | `BitwardenField` | 自定义字段 |
| | `BitwardenFolder` | 文件夹 |
| `BitwardenStatus.cs` | `BitwardenStatus` | 密码库状态 |

---

## 数据流

### 1. 启动流程

```
Command Palette 启动
        │
        ▼
加载扩展 (COM)
        │
        ▼
BitwardenForCommandPalette.GetProvider(ProviderType.Commands)
        │
        ▼
返回 BitwardenForCommandPaletteCommandsProvider
        │
        ▼
显示 "Bitwarden For Command Palette" 入口
```

### 2. 打开扩展流程

```
用户选择扩展
        │
        ▼
BitwardenForCommandPalettePage.GetItems()
        │
        ├─── _lastStatus == null?
        │           │
        │           ▼ Yes
        │    CheckStatusAndLoadAsync()
        │           │
        │           ▼
        │    BitwardenCliService.GetStatusAsync()
        │           │
        │           ▼
        │    "bw status" → 解析 JSON
        │           │
        │           ▼
        │    判断状态:
        │    ├── unauthenticated → 显示 "Not Logged In"
        │    ├── locked → 显示 "Unlock Vault" (UnlockPage)
        │    └── unlocked → LoadItemsAsync()
        │
        ▼
显示项目列表
```

### 3. 解锁流程

```
用户点击 "Unlock Vault"
        │
        ▼
导航到 UnlockPage
        │
        ▼
显示 Adaptive Card 表单
        │
        ▼
用户输入主密码并提交
        │
        ▼
UnlockForm.SubmitForm(payload)
        │
        ▼
BitwardenCliService.UnlockAsync(masterPassword)
        │
        ▼
"bw unlock {password} --raw"
        │
        ├─── 成功: 保存 SessionKey, 返回 GoBack()
        │           │
        │           ▼
        │    BitwardenForCommandPalettePage.OnUnlocked()
        │           │
        │           ▼
        │    LoadItemsAsync() → 显示项目列表
        │
        └─── 失败: 显示错误 Toast
```

### 4. 搜索流程

```
用户输入搜索文本
        │
        ▼
BitwardenForCommandPalettePage.UpdateSearchText()
        │
        ▼
RaiseItemsChanged()
        │
        ▼
GetItems() 重新调用
        │
        ▼
FilterItems(_items, SearchText, _currentFilter)
        │
        ▼
应用筛选条件:
├── 收藏筛选 (filter.FavoritesOnly)
├── 类型筛选 (filter.ItemType)
├── 文件夹筛选 (filter.FolderId)
└── 搜索文本匹配 (Name, Username, URL)
        │
        ▼
返回过滤后的 ListItem[]
```

### 5. 复制命令流程

```
用户点击项目或选择右键菜单命令
        │
        ▼
Command.Invoke()
        │
        ├─── CopyPasswordCommand
        │           │
        │           ▼
        │    ClipboardHelper.SetText(item.Login.Password)
        │
        ├─── CopyTotpCommand
        │           │
        │           ▼
        │    BitwardenCliService.GetTotpAsync(itemId)
        │           │
        │           ▼
        │    "bw get totp {itemId} --session {key}"
        │           │
        │           ▼
        │    ClipboardHelper.SetText(totpCode)
        │
        └─── 其他复制命令...
        │
        ▼
CommandResult.Dismiss() 或 ShowToast()
```

---

## 模块详解

### BitwardenCliService

**位置**: `Services/BitwardenCliService.cs`

**模式**: 单例模式 (`BitwardenCliService.Instance`)

**职责**:
- 封装所有 Bitwarden CLI 调用
- 管理会话密钥 (`SessionKey`)
- 提供异步 API

**关键方法**:

```csharp
// 状态检查
Task<BitwardenStatus?> GetStatusAsync()

// 认证
Task<(bool success, string message)> UnlockAsync(string masterPassword)
Task<bool> LockAsync()

// 数据访问
Task<BitwardenItem[]?> GetItemsAsync()
Task<BitwardenItem[]?> SearchItemsAsync(string query)
Task<BitwardenItem[]?> GetItemsByFolderAsync(string? folderId)
Task<BitwardenFolder[]?> GetFoldersAsync()
Task<string?> GetTotpAsync(string itemId)

// 同步
Task<bool> SyncAsync()
```

**JSON 序列化**: 使用 Source Generator 实现 AOT 兼容

```csharp
[JsonSerializable(typeof(BitwardenStatus))]
[JsonSerializable(typeof(BitwardenItem[]))]
[JsonSerializable(typeof(BitwardenFolder[]))]
internal sealed partial class BitwardenJsonContext : JsonSerializerContext { }
```

### IconService

**位置**: `Services/IconService.cs`

**模式**: 静态类

**职责**:
- 根据项目类型返回适当的图标
- 生成网站 favicon URL

**关键方法**:

```csharp
// 获取项目图标
static IIconInfo GetItemIcon(BitwardenItem item)

// 生成网站图标 URL
static IIconInfo? GetWebsiteIconUrl(string uri)

// 提取主机名
static string? ExtractHostname(string uri)

// 提取主域名 (google.com from accounts.google.com)
static string ExtractDomainFromHostname(string hostname)
```

**图标 URL 格式**: `https://icons.bitwarden.net/{domain}/icon.png`

### BitwardenForCommandPalettePage

**位置**: `Pages/BitwardenForCommandPalettePage.cs`

**基类**: `DynamicListPage`

**职责**:
- 主页面，显示密码项列表
- 处理搜索和筛选
- 管理页面状态（加载、错误、空状态）

**状态管理**:

```csharp
private BitwardenItem[]? _items;      // 缓存的项目列表
private bool _isLoading;               // 加载状态
private string? _errorMessage;         // 错误消息
private BitwardenStatus? _lastStatus;  // 最后的状态
private VaultFilter _currentFilter;    // 当前筛选条件
```

**页面状态流转**:

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Loading    │────▶│    Error     │     │   Locked     │
└──────────────┘     └──────────────┘     └──────────────┘
       │                                         │
       │                                         │
       ▼                                         ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  Not Logged  │     │    Empty     │◀────│   Items      │
│     In       │     │              │     │    List      │
└──────────────┘     └──────────────┘     └──────────────┘
```

### FilterPage

**位置**: `Pages/FilterPage.cs`

**基类**: `DynamicListPage`

**职责**:
- 提供筛选选项界面
- 支持收藏、类型、文件夹筛选

**筛选类型**:

```csharp
internal sealed class VaultFilter
{
    public bool FavoritesOnly { get; set; }      // 仅收藏
    public string? FolderId { get; set; }         // 文件夹 ID
    public string? FolderName { get; set; }       // 文件夹名称（显示用）
    public BitwardenItemType? ItemType { get; set; }  // 项目类型
}
```

---

## 扩展指南

### 添加新命令

1. 在 `Commands/ItemCommands.cs` 中创建新命令类：

```csharp
internal sealed partial class MyNewCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public MyNewCommand(BitwardenItem item)
    {
        _item = item;
        Name = "My New Command";
        Icon = new IconInfo("\uE8C8");
    }

    public override CommandResult Invoke()
    {
        // 实现逻辑
        return CommandResult.Dismiss();
    }
}
```

2. 在 `BitwardenForCommandPalettePage.cs` 的对应 `Add*Commands` 方法中添加：

```csharp
commands.Add(new CommandContextItem(new MyNewCommand(item)));
```

### 添加新页面

1. 创建新页面类：

```csharp
// Pages/MyNewPage.cs
internal sealed partial class MyNewPage : DynamicListPage
{
    public MyNewPage()
    {
        Title = "My New Page";
        Name = "Open";
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems()
    {
        // 返回列表项
    }
}
```

2. 在需要的地方导航到新页面：

```csharp
var page = new MyNewPage();
return new ListItem(page) { Title = "Go to New Page" };
```

### 添加新服务方法

1. 在 `BitwardenCliService.cs` 中添加新方法：

```csharp
public async Task<string?> MyNewMethodAsync(string param)
{
    if (string.IsNullOrEmpty(SessionKey))
        return null;

    var (output, error, exitCode) = await ExecuteCommandAsync(
        $"my-command \"{param}\" --session \"{SessionKey}\"");

    return exitCode == 0 ? output.Trim() : null;
}
```

### 添加新数据模型

1. 在 `Models/BitwardenItem.cs` 中添加新类：

```csharp
public class MyNewModel
{
    [JsonPropertyName("field1")]
    public string? Field1 { get; set; }
}
```

2. 如果需要序列化，在 `BitwardenJsonContext` 中注册：

```csharp
[JsonSerializable(typeof(MyNewModel))]
[JsonSerializable(typeof(MyNewModel[]))]
internal sealed partial class BitwardenJsonContext : JsonSerializerContext { }
```

---

## 配置与构建

### 构建命令

```bash
# Debug 构建
dotnet build -p:Platform=x64
dotnet build -p:Platform=ARM64

# Release 构建
dotnet build -c Release -p:Platform=x64
```

### 部署

1. Visual Studio: 右键项目 → 部署
2. 命令行: `dotnet publish`

### 调试

1. 部署扩展
2. 在 Command Palette 中运行 `Reload`
3. 附加到 `BitwardenForCommandPalette.exe` 进程

---

## 参考

- [CommandPalette Extensions Guide](CommandPalette-Extensions-Guide.md)
- [Bitwarden CLI Guide](Bitwarden-CLI-Guide.md)
- [README](../README.md)
