# BitwardenForCommandPalette 项目架构文档

> 项目整体架构、模块组织与交互关系（更新于 2025-12-31）

## 目录

- [项目概述](#项目概述)
- [架构图](#架构图)
- [目录结构](#目录结构)
- [核心模块](#核心模块)
- [页面详解](#页面详解)
- [服务详解](#服务详解)
- [数据流](#数据流)
- [扩展指南](#扩展指南)
- [问题排查](#问题排查)

---

## 项目概述

BitwardenForCommandPalette 是一个 PowerToys Command Palette 扩展，通过本地 Bitwarden CLI (`bw`) 与 Bitwarden 密码库交互，提供快速访问和管理密码的能力。

### 技术栈

| 组件 | 技术 |
|------|------|
| 框架 | .NET 9.0 |
| 平台 | Windows 10.0.26100.0 |
| 打包 | MSIX |
| 扩展 SDK | Microsoft.CommandPalette.Extensions.Toolkit |
| 密码管理 | Bitwarden CLI |
| JSON 序列化 | System.Text.Json (Source Generator) |
| UI 框架 | Adaptive Cards 1.6 |
| 本地化 | WinRT ResourceLoader |

### 核心功能

| 功能类别 | 具体功能 |
|---------|---------|
| 密码库管理 | 状态检测、解锁、锁定、同步 |
| 项目浏览 | 列表展示、搜索、筛选（收藏/文件夹/类型/回收站） |
| 项目操作 | 复制凭据、打开 URL、查看 TOTP |
| CRUD 操作 | 创建、编辑、删除、恢复项目 |
| 密码生成 | 随机密码生成、口令短语生成 |
| 文件夹管理 | 创建文件夹、按文件夹筛选 |

---

## 架构图

```
┌─────────────────────────────────────────────────────────────────────┐
│                      PowerToys Command Palette                        │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   │ COM / WinRT
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        Extension Entry Point                          │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │  BitwardenForCommandPalette.cs (IExtension)                    │   │
│  │  └── BitwardenForCommandPaletteCommandsProvider.cs             │   │
│  └───────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                            Pages Layer                                │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ 列表页面 (DynamicListPage)                                       │ │
│  │  ┌──────────────────┐  ┌──────────────────┐                     │ │
│  │  │ MainPage         │  │ FilterPage       │                     │ │
│  │  │ - 项目列表       │  │ - 收藏筛选       │                     │ │
│  │  │ - 搜索           │  │ - 文件夹筛选     │                     │ │
│  │  │ - 详情面板       │  │ - 类型筛选       │                     │ │
│  │  │ - 回收站视图     │  │ - 回收站         │                     │ │
│  │  └──────────────────┘  └──────────────────┘                     │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ 内容页面 (ContentPage + FormContent)                            │ │
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────────┐│ │
│  │  │ UnlockPage     │  │ CreateItemPage │  │ EditItemPage       ││ │
│  │  │ - 密码输入     │  │ - 类型选择     │  │ - 字段编辑         ││ │
│  │  │ - 解锁提交     │  │ - 表单输入     │  │ - 保存/取消        ││ │
│  │  └────────────────┘  │ - 文件夹选择   │  └────────────────────┘│ │
│  │                      └────────────────┘                         │ │
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────────┐│ │
│  │  │ TotpPage       │  │ GeneratorPage  │  │ CreateFolderPage   ││ │
│  │  │ - 实时倒计时   │  │ - 密码生成器   │  │ - 名称输入         ││ │
│  │  │ - 一键复制     │  │ - 口令生成器   │  │ - 创建提交         ││ │
│  │  └────────────────┘  └────────────────┘  └────────────────────┘│ │
│  └─────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                          Commands Layer                               │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │                       ItemCommands.cs                            │ │
│  │  ┌──────────────┐ ┌──────────────┐ ┌────────────────────────┐  │ │
│  │  │ 登录命令     │ │ 银行卡命令   │ │ CRUD 命令              │  │ │
│  │  │ - Password   │ │ - Number     │ │ - CreateItemCommand    │  │ │
│  │  │ - Username   │ │ - CVV        │ │ - EditItemCommand      │  │ │
│  │  │ - URL        │ │ - Expiry     │ │ - DeleteItemCommand    │  │ │
│  │  │ - TOTP       │ │ - Holder     │ │ - RestoreCommand       │  │ │
│  │  └──────────────┘ └──────────────┘ │ - PermanentDeleteCmd   │  │ │
│  │  ┌──────────────┐ ┌──────────────┐ └────────────────────────┘  │ │
│  │  │ 身份命令     │ │ 密码库命令   │                             │ │
│  │  │ - FullName   │ │ - Sync       │                             │ │
│  │  │ - Email      │ │ - Lock       │                             │ │
│  │  │ - Phone      │ └──────────────┘                             │ │
│  │  │ - Address    │ ┌──────────────┐                             │ │
│  │  │ - SSN, etc.  │ │ 通用命令     │                             │ │
│  │  └──────────────┘ │ - Notes      │                             │ │
│  │                   │ - Field      │                             │ │
│  │                   └──────────────┘                             │ │
│  └─────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                          Services Layer                               │
│  ┌─────────────────────────────┐  ┌──────────────────────────────┐  │
│  │   BitwardenCliService.cs    │  │      IconService.cs          │  │
│  │   (Singleton)               │  │      (Static + Cache)        │  │
│  │                             │  │                              │  │
│  │  状态管理:                  │  │  - GetItemIcon()             │  │
│  │  - GetStatusAsync()         │  │  - GetWebsiteIconUrl()       │  │
│  │  - UnlockAsync()            │  │  - 200 条缓存上限            │  │
│  │  - LockAsync()              │  │                              │  │
│  │  - SyncAsync()              │  └──────────────────────────────┘  │
│  │                             │  ┌──────────────────────────────┐  │
│  │  数据获取:                  │  │    SettingsManager.cs        │  │
│  │  - GetItemsAsync()          │  │    (ISettingsManager)        │  │
│  │  - GetFoldersAsync()        │  │                              │  │
│  │  - GetTotpAsync()           │  │  - CLI Path                  │  │
│  │                             │  │  - API Client ID/Secret      │  │
│  │  CRUD 操作:                 │  │  - Custom Environment Vars   │  │
│  │  - CreateItemAsync()        │  │                              │  │
│  │  - EditItemAsync()          │  └──────────────────────────────┘  │
│  │  - DeleteItemAsync()        │  ┌──────────────────────────────┐  │
│  │  - RestoreItemAsync()       │  │    ResourceHelper.cs         │  │
│  │  - PermanentDeleteAsync()   │  │    (Static)                  │  │
│  │  - CreateFolderAsync()      │  │                              │  │
│  │                             │  │  - GetString(key)            │  │
│  │  生成器:                    │  │  - 多语言支持 (en/zh)        │  │
│  │  - GeneratePasswordAsync()  │  │                              │  │
│  │  - GeneratePassphraseAsync()│  └──────────────────────────────┘  │
│  └─────────────────────────────┘                                    │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   │ Process.Start("bw", args)
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       External: Bitwarden CLI                         │
│  bw status | unlock | lock | sync | list | get | create | edit       │
│  bw delete | restore | generate                                       │
└─────────────────────────────────────────────────────────────────────┘
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
│   └── ItemCommands.cs                 # 所有复制/操作命令 (~650 行)
│
├── Pages/                              # 页面模块
│   ├── BitwardenForCommandPalettePage.cs  # 主列表页 (~800 行)
│   ├── UnlockPage.cs                   # 解锁表单页
│   ├── FilterPage.cs                   # 筛选选项页
│   ├── CreateItemPage.cs               # 创建项目页 (~900 行)
│   ├── EditItemPage.cs                 # 编辑项目页 (~650 行)
│   ├── TotpPage.cs                     # TOTP 验证码页
│   ├── GeneratorPage.cs                # 密码/口令生成器页 (~450 行)
│   └── VaultFilters.cs                 # 筛选状态管理类
│
├── Models/                             # 数据模型
│   ├── BitwardenItem.cs                # 密码项模型（含所有子类型）
│   └── BitwardenStatus.cs              # 状态模型
│
├── Services/                           # 服务层
│   ├── BitwardenCliService.cs          # CLI 封装服务 (~720 行)
│   ├── IconService.cs                  # 图标服务
│   └── SettingsManager.cs              # 设置管理服务
│
├── Helpers/                            # 辅助类
│   └── ResourceHelper.cs               # 多语言资源助手 (~340 行)
│
├── Strings/                            # 本地化资源
│   ├── en-US/Resources.resw            # 英文资源 (~900 行)
│   └── zh-CN/Resources.resw            # 中文资源 (~900 行)
│
└── Assets/                             # 资源文件
    ├── app.ico
    └── *.png
```

---

## 核心模块

### 1. 入口模块

| 文件 | 职责 |
|------|------|
| `BitwardenForCommandPalette.cs` | 实现 `IExtension`，作为 COM 服务器入口点 |
| `BitwardenForCommandPaletteCommandsProvider.cs` | 继承 `CommandProvider`，定义扩展顶级命令 |
| `Program.cs` | 启动 COM 服务器，处理 `-RegisterProcessAsComServer` |

### 2. 页面模块

| 文件 | 基类 | 职责 |
|------|------|------|
| `BitwardenForCommandPalettePage.cs` | `DynamicListPage` | 主页面，项目列表、搜索、详情面板 |
| `UnlockPage.cs` | `ContentPage` | 解锁表单，主密码输入 |
| `FilterPage.cs` | `DynamicListPage` | 筛选页面，收藏/文件夹/类型筛选 |
| `CreateItemPage.cs` | `ContentPage` | 创建项目/文件夹，类型选择 + Adaptive Cards 表单 |
| `EditItemPage.cs` | `ContentPage` | 编辑项目，Adaptive Cards 表单 |
| `TotpPage.cs` | `ContentPage` | TOTP 页面，实时倒计时 + 一键复制 |
| `GeneratorPage.cs` | `ContentPage` | 密码/口令生成器 |
| `VaultFilters.cs` | - | 筛选状态管理类 |

### 3. 命令模块

| 命令类别 | 命令列表 |
|---------|---------|
| **登录命令** | CopyPassword, CopyUsername, CopyUrl, OpenUrl, CopyTotp |
| **银行卡命令** | CopyCardNumber, CopyCardCvv, CopyCardExpiration, CopyCardholderName |
| **身份命令** | CopyFullName, CopyEmail, CopyPhone, CopyAddress, CopyCompany, CopySsn, CopyPassport, CopyLicense |
| **通用命令** | CopyNotes, CopyField |
| **密码库命令** | SyncVault, LockVault |
| **CRUD 命令** | CreateItem, EditItem, DeleteItem, RestoreItem, PermanentDelete |

### 4. 服务模块

| 服务 | 模式 | 职责 |
|------|------|------|
| `BitwardenCliService` | 单例 | CLI 封装，会话管理，所有 bw 命令调用 |
| `IconService` | 静态 + 缓存 | 网站图标获取，200 条缓存上限 |
| `SettingsManager` | 接口实现 | 设置管理，CLI 路径/API Key/环境变量 |
| `ResourceHelper` | 静态 | 多语言资源获取 |

### 5. 模型模块

| 类 | 用途 |
|-----|------|
| `BitwardenItem` | 密码项主模型 |
| `BitwardenLogin` | 登录信息 |
| `BitwardenCard` | 银行卡信息 |
| `BitwardenIdentity` | 身份信息 |
| `BitwardenSecureNote` | 安全笔记 |
| `BitwardenField` | 自定义字段 |
| `BitwardenFolder` | 文件夹 |
| `BitwardenUri` | URL 项 |
| `BitwardenStatus` | 密码库状态 |

---

## 页面详解

### MainPage (BitwardenForCommandPalettePage)

主页面，负责展示项目列表和处理用户交互。

**核心功能**：
- 密码库状态检测和解锁导航
- 项目列表展示（支持回收站视图）
- 搜索过滤
- 详情面板（右侧 Markdown）
- 上下文命令菜单

**关键方法**：
```csharp
public override IListItem[] GetItems()
{
    // 1. 检查密码库状态
    // 2. 加载项目列表
    // 3. 应用筛选
    // 4. 构建 ListItem 数组
}

private string BuildDetailsMarkdown(BitwardenItem item)
{
    // 根据项目类型生成详情面板 Markdown
}

private IListItem CreateListItem(BitwardenItem item)
{
    // 创建列表项，设置命令、图标、详情
}
```

### CreateItemPage

创建项目页面，使用 Adaptive Cards 实现表单。

**结构**：
```
CreateItemTypeSelectorPage (ListPage)
├── CreateItemPage (Login)
├── CreateItemPage (Card)
├── CreateItemPage (Identity)
├── CreateItemPage (SecureNote)
├── CreateFolderPage
├── PasswordGeneratorPage
└── PassphraseGeneratorPage
```

**关键实现**：
- 动态加载文件夹列表
- 动态生成 Input.ChoiceSet（文件夹选择）
- 表单提交调用 `BitwardenCliService.CreateItemAsync()`

### EditItemPage

编辑项目页面，加载现有项目数据到表单。

**关键实现**：
- 根据项目类型选择不同模板
- 预填充现有数据
- 表单提交调用 `BitwardenCliService.EditItemAsync()`

### TotpPage

TOTP 验证码页面，实时显示倒计时。

**关键实现**：
```csharp
// 定时器每秒更新
private Timer _timer;

private void UpdateTotp()
{
    var totp = BitwardenCliService.Instance.GetTotpAsync(_itemId).Result;
    var remaining = 30 - (DateTime.UtcNow.Second % 30);
    // 更新显示
}
```

### GeneratorPage

密码/口令生成器页面。

**两个生成器**：
1. **PasswordGeneratorPage** - 随机密码
   - 长度：5-128
   - 选项：大写、小写、数字、特殊字符
   
2. **PassphraseGeneratorPage** - 口令短语
   - 单词数：3-20
   - 分隔符
   - 选项：首字母大写、包含数字

---

## 服务详解

### BitwardenCliService

核心服务，封装所有 Bitwarden CLI 调用。

**单例模式**：
```csharp
private static BitwardenCliService? _instance;
public static BitwardenCliService Instance => _instance ??= new BitwardenCliService();
```

**Session 管理**：
```csharp
public string? SessionKey { get; private set; }

public async Task<bool> UnlockAsync(string password)
{
    var (output, _, exitCode) = await ExecuteCommandAsync($"unlock \"{password}\" --raw");
    if (exitCode == 0)
    {
        SessionKey = output.Trim();
        return true;
    }
    return false;
}
```

**命令执行**：
```csharp
private static async Task<(string output, string error, int exitCode)> ExecuteCommandAsync(string arguments)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = SettingsManager.Instance.BitwardenCliPath ?? "bw",
        Arguments = arguments,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    
    // 注入环境变量
    foreach (var (key, value) in SettingsManager.Instance.GetEnvironmentVariables())
    {
        startInfo.EnvironmentVariables[key] = value;
    }
    
    // 执行并返回结果
}
```

**JSON 序列化（AOT 兼容）**：
```csharp
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(BitwardenStatus))]
[JsonSerializable(typeof(BitwardenItem[]))]
[JsonSerializable(typeof(BitwardenFolder[]))]
internal partial class BitwardenJsonContext : JsonSerializerContext { }
```

### IconService

图标服务，带缓存机制。

**缓存策略**：
```csharp
private static readonly Dictionary<string, string> _iconUrlCache = new();
private const int MaxCacheSize = 200;

public static string GetWebsiteIconUrl(string domain)
{
    if (_iconUrlCache.TryGetValue(domain, out var cached))
        return cached;
    
    // 缓存淘汰
    if (_iconUrlCache.Count >= MaxCacheSize)
    {
        var keysToRemove = _iconUrlCache.Keys.Take(MaxCacheSize / 2).ToList();
        foreach (var key in keysToRemove)
            _iconUrlCache.Remove(key);
    }
    
    var url = $"https://icons.bitwarden.net/{domain}/icon.png";
    _iconUrlCache[domain] = url;
    return url;
}
```

### SettingsManager

设置管理，实现 `ISettingsManager` 接口。

**配置项**：
- `BitwardenCliPath` - CLI 路径
- `BitwardenClientId` - API Client ID
- `BitwardenClientSecret` - API Client Secret
- `CustomEnvironmentVariables` - 自定义环境变量

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

### 2. 项目浏览流程

```
用户选择扩展
        │
        ▼
MainPage.GetItems()
        │
        ├─── 状态未知?
        │         │
        │         ▼ Yes
        │    GetStatusAsync() → bw status
        │         │
        │         ▼
        │    状态判断:
        │    ├── unauthenticated → 显示登录提示
        │    ├── locked → 导航到 UnlockPage
        │    └── unlocked → 继续加载
        │
        ▼
LoadItemsAsync() → bw list items
        │
        ▼
应用筛选 (VaultFilters)
        │
        ▼
构建 ListItem[]，显示列表
```

### 3. 创建项目流程

```
用户点击 "Create Item"
        │
        ▼
导航到 CreateItemTypeSelectorPage
        │
        ▼
GetItems() → 加载文件夹列表
        │
        ▼
显示类型选择列表
        │
        ▼
用户选择类型（如 Login）
        │
        ▼
导航到 CreateItemPage(Login, folders)
        │
        ▼
显示 Adaptive Card 表单
├── 名称输入
├── 文件夹选择（下拉框）
├── 类型特定字段
└── 创建按钮
        │
        ▼
用户填写并提交
        │
        ▼
CreateItemForm.SubmitForm()
        │
        ▼
构建 JsonObject
        │
        ▼
CreateItemAsync() → bw create item <base64>
        │
        ▼
显示成功/失败提示
```

### 4. TOTP 流程

```
用户点击 TOTP 命令
        │
        ▼
导航到 TotpPage(itemId)
        │
        ▼
启动定时器 (1秒间隔)
        │
        ▼
每秒: GetTotpAsync() → bw get totp
        │
        ▼
更新显示:
├── 验证码
├── 倒计时进度条
└── 剩余秒数
        │
        ▼
用户点击复制 → 复制到剪贴板
```

---

## 扩展指南

### 添加新页面

1. 创建新的页面类：

```csharp
internal sealed partial class MyNewPage : DynamicListPage
{
    public MyNewPage()
    {
        Icon = new IconInfo("\uE700");
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

2. 在需要的地方导航：

```csharp
return new ListItem(new MyNewPage()) { Title = "Go to New Page" };
```

### 添加新的 Adaptive Card 表单页面

```csharp
internal sealed class MyFormPage : ContentPage
{
    private readonly MyForm _form;
    
    public MyFormPage()
    {
        _form = new MyForm();
        Name = "Page Name";
    }
    
    public override IContent[] GetContent() => [_form];
}

internal sealed class MyForm : FormContent
{
    public MyForm()
    {
        TemplateJson = GetTemplate();
        DataJson = GetData();
    }
    
    private static string GetTemplate() => """
    {
        "type": "AdaptiveCard",
        "version": "1.6",
        "body": [
            {
                "type": "Input.Text",
                "id": "myField",
                "label": "${fieldLabel}"
            }
        ],
        "actions": [
            {
                "type": "Action.Submit",
                "title": "Submit"
            }
        ]
    }
    """;
    
    private static string GetData()
    {
        return new JsonObject
        {
            ["fieldLabel"] = "My Field"
        }.ToJsonString();
    }
    
    public override ICommandResult SubmitForm(string payload, string data)
    {
        var formData = JsonNode.Parse(payload)?.AsObject();
        var myField = formData?["myField"]?.GetValue<string>();
        
        // 处理提交
        
        return CommandResult.ShowToast("Success");
    }
}
```

### 添加新的 CLI 方法

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

### 添加新的资源字符串

1. 在 `en-US/Resources.resw` 添加：
```xml
<data name="MyNewString" xml:space="preserve">
    <value>English text</value>
</data>
```

2. 在 `zh-CN/Resources.resw` 添加：
```xml
<data name="MyNewString" xml:space="preserve">
    <value>中文文本</value>
</data>
```

3. 在 `ResourceHelper.cs` 添加：
```csharp
public static string MyNewString => GetString("MyNewString");
```

---

## 问题排查

常见问题和解决方案请参考 [Troubleshooting.md](Troubleshooting.md)。

主要包括：
- AOT 编译相关问题
- Adaptive Cards 相关问题
- Command Palette SDK 相关问题
- Bitwarden CLI 相关问题

---

## 参考文档

- [CommandPalette Extensions Guide](CommandPalette-Extensions-Guide.md)
- [Bitwarden CLI Guide](Bitwarden-CLI-Guide.md)
- [Adaptive Cards Guide](Adaptive-Cards-Guide.md)
- [Localization Guide](Localization-Guide.md)
- [问题排查指南](Troubleshooting.md)
- [更新日志](CHANGELOG.md)
