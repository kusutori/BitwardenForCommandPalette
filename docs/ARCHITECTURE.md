# Bitwarden For Command Palette - 架构文档

## 项目概述

Bitwarden For Command Palette 是一个 Windows PowerToys Command Palette 扩展，用于管理和访问 Bitwarden 密码管理器。该项目通过本地 Bitwarden CLI (`bw`) 与密码库进行交互，提供了一个快速、安全的密码管理界面。

## 项目结构

```
BitwardenForCommandPalette/
├── BitwardenForCommandPalette/              # 主项目目录
│   ├── BitwardenForCommandPalette.cs        # 扩展入口点 (IExtension 实现)
│   ├── BitwardenForCommandPaletteCommandsProvider.cs  # 命令提供者
│   ├── Program.cs                          # 程序入口点
│   │
│   ├── Commands/                           # 命令实现
│   │   └── ItemCommands.cs                 # 各种复制/操作命令
│   │
│   ├── Helpers/                            # 辅助类
│   │   └── ResourceHelper.cs              # 多语言资源管理
│   │
│   ├── Models/                             # 数据模型
│   │   ├── BitwardenItem.cs               # Bitwarden 项目模型
│   │   └── BitwardenStatus.cs             # 状态模型
│   │
│   ├── Pages/                              # UI 页面
│   │   ├── BitwardenForCommandPalettePage.cs  # 主列表页面
│   │   ├── UnlockPage.cs                  # 解锁表单页面
│   │   └── FilterPage.cs                  # 筛选页面
│   │
│   ├── Services/                           # 业务逻辑服务
│   │   ├── BitwardenCliService.cs         # Bitwarden CLI 封装
│   │   ├── IconService.cs                 # 网站图标服务 (带缓存)
│   │   └── SettingsManager.cs             # 设置管理
│   │
│   ├── Strings/                            # 多语言资源
│   │   ├── en-US/
│   │   │   └── Resources.resw             # 英文资源
│   │   └── zh-CN/
│   │       └── Resources.resw             # 简体中文资源
│   │
│   └── Assets/                             # 静态资源
│       └── Square44x44Logo.targetsize-24_altform-unplated.png
│
├── docs/                                   # 文档目录
│   └── ARCHITECTURE.md                     # 本文档
│
└── README.md                               # 项目说明
```

## 核心架构

### 1. 扩展生命周期

扩展遵循 PowerToys Command Palette 的标准生命周期：

```csharp
// 1. 首次加载时，Command Palette 调用 Main() 并传递 -RegisterProcessAsComServer 参数
Program.Main(args)
  ↓
// 2. 创建 COM 服务器并注册扩展
ComServer server = new();
BitwardenForCommandPalette extensionInstance = new(extensionDisposedEvent);
server.RegisterClass<BitwardenForCommandPalette, IExtension>(() => extensionInstance);
server.Start();
  ↓
// 3. 扩展实例被缓存，每次请求时返回同一实例
extensionDisposedEvent.WaitOne();  // 等待扩展被释放
  ↓
// 4. 清理资源
server.Stop();
```

### 2. 扩展入口点 (`BitwardenForCommandPalette.cs`)

```csharp
public sealed partial class BitwardenForCommandPalette : IExtension, IDisposable
{
    private readonly BitwardenForCommandPaletteCommandsProvider _provider = new();

    public object? GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Commands => _provider,  // 仅提供命令提供者
            _ => null,
        };
    }
}
```

**职责**：
- 实现 `IExtension` 接口
- 作为扩展的根对象
- 返回命令提供者给宿主

### 3. 命令提供者 (`BitwardenForCommandPaletteCommandsProvider.cs`)

```csharp
public partial class BitwardenForCommandPaletteCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly Settings _settings = new();

    public BitwardenForCommandPaletteCommandsProvider()
    {
        DisplayName = ResourceHelper.AppDisplayName;
        Icon = IconHelpers.FromRelativePath("Assets\\...");

        InitializeSettings();  // 初始化设置页面

        _commands = [
            new CommandItem(new BitwardenForCommandPalettePage())
            {
                Title = DisplayName,
                Icon = Icon
            },
        ];
    }

    public override ICommandItem[] TopLevelCommands() => _commands;
}
```

**职责**：
- 定义扩展的顶层命令（显示在 Command Palette 主界面）
- 管理设置页面的配置项
- 初始化设置管理器

**设置项**：
1. **BwPath**: Bitwarden CLI 路径（默认 "bw"）
2. **ClientId**: Bitwarden API Client ID
3. **ClientSecret**: Bitwarden API Client Secret
4. **CustomEnv**: 自定义环境变量（格式：`KEY1=VALUE1;KEY2=VALUE2`）

### 4. 主页面 (`BitwardenForCommandPalettePage.cs`)

主页面继承自 `DynamicListPage`，实现动态列表：

```csharp
internal sealed partial class BitwardenForCommandPalettePage : DynamicListPage
{
    private BitwardenItem[]? _items;           // 缓存的项目列表
    private bool _isLoading;                   // 加载状态
    private string? _errorMessage;             // 错误信息
    private BitwardenStatus? _lastStatus;      // 最后一次状态
    private VaultFilter _currentFilter;        // 当前筛选器

    public override IListItem[] GetItems()
    {
        // 1. 检查状态（如果未检查）
        if (_lastStatus == null && !_isLoading)
            return [CreateLoadingItem()];

        // 2. 检查错误
        if (!string.IsNullOrEmpty(_errorMessage))
            return [CreateErrorItem(_errorMessage)];

        // 3. 检查锁定/未登录状态
        if (_lastStatus?.IsLoggedOut ?? false)
            return [CreateNotLoggedInItem()];

        if (_lastStatus?.IsLocked ?? true)
            return [CreateUnlockItem()];

        // 4. 显示项目列表
        if (_items == null || _items.Length == 0)
            return [CreateEmptyItem()];

        var filteredItems = FilterItems(_items, SearchText, _currentFilter);
        var listItems = filteredItems.Select(CreateListItem).ToList();

        // 5. 添加实用命令
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            listItems.Insert(0, CreateFilterItem());
            listItems.Add(CreateSyncItem());
            listItems.Add(CreateLockItem());
        }

        return listItems.ToArray();
    }
}
```

**页面状态流**：

```
首次加载
    ↓
检查状态 → 未登录 → 显示"未登录"提示
    ↓
已锁定 → 显示"解锁"按钮 → 导航到 UnlockPage
    ↓
已解锁 → 加载项目列表 → 显示项目
    ↓
用户搜索/筛选 → 过滤显示
```

### 5. 解锁页面 (`UnlockPage.cs`)

使用 Adaptive Cards 实现表单界面：

```csharp
internal sealed partial class UnlockPage : ContentPage
{
    private readonly UnlockForm _unlockForm;

    public override IContent[] GetContent() => [_unlockForm];
}

internal sealed partial class UnlockForm : FormContent
{
    // Adaptive Cards JSON 模板
    TemplateJson = """{
        "type": "AdaptiveCard",
        "body": [
            { "type": "Input.Text", "id": "masterPassword", "style": "password" }
        ],
        "actions": [{ "type": "Action.Submit", "title": "解锁" }]
    }""";

    public override CommandResult SubmitForm(string payload)
    {
        var masterPassword = ParseFormInput(payload);
        var (success, message) = service.UnlockAsync(masterPassword).Result;

        if (success)
        {
            _onUnlocked?.Invoke();  // 通知主页面刷新
            return CommandResult.GoBack();
        }
        return CommandResult.ShowToast(message);
    }
}
```

### 6. 筛选页面 (`FilterPage.cs`)

动态列表页面，提供多种筛选选项：

```csharp
internal sealed partial class FilterPage : DynamicListPage
{
    public override IListItem[] GetItems()
    {
        return [
            // 基础筛选
            new ListItem(new ApplyFilterCommand(new VaultFilter(), ...))
            { Title = "所有项目" },

            new ListItem(new ApplyFilterCommand(new VaultFilter { FavoritesOnly = true }, ...))
            { Title = "仅收藏" },

            // 类型筛选
            new ListItem(new ApplyFilterCommand(new VaultFilter { ItemType = BitwardenItemType.Login }, ...))
            { Title = "登录项目" },

            ...

            // 文件夹筛选（动态加载）
            new SectionHeaderItem("文件夹"),
            ..._folders.Select(f => new ListItem(new ApplyFilterCommand(...)))
        ];
    }
}
```

### 7. Bitwarden CLI 服务 (`BitwardenCliService.cs`)

封装所有与 Bitwarden CLI 的交互，使用单例模式：

```csharp
public partial class BitwardenCliService
{
    private static BitwardenCliService? _instance;
    public static BitwardenCliService Instance { get; }

    public string? SessionKey { get; private set; }  // 会话密钥，解锁后缓存

    // 核心命令执行
    private async Task<(string output, string error, int exitCode)> ExecuteCommandAsync(string arguments)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = SettingsManager.Instance.BwPath,
            Arguments = arguments,
            Environment = SettingsManager.Instance.GetEnvironmentVariables(), // 支持自定义环境变量
            ...
        };

        // 异步执行并捕获输出
        var output = await process.Start();
        return (output, error, exitCode);
    }

    // 公共 API
    public static async Task<BitwardenStatus?> GetStatusAsync();
    public async Task<(bool success, string message)> UnlockAsync(string masterPassword);
    public async Task<bool> LockAsync();
    public async Task<bool> SyncAsync();
    public async Task<BitwardenItem[]?> GetItemsAsync();
    public async Task<BitwardenItem[]?> SearchItemsAsync(string query);
    public async Task<string?> GetTotpAsync(string itemId);
    public async Task<BitwardenItem[]?> GetItemsByFolderAsync(string? folderId);
    public async Task<BitwardenFolder[]?> GetFoldersAsync();
}
```

**认证流程**：

```
1. 检查设置是否有 API Key
   ↓ 是
   设置环境变量 BW_CLIENTID, BW_CLIENTSECRET
   自动执行 bw login --apikey

2. 检查已有会话密钥
   ↓ 有
   使用会话密钥执行命令

3. 需要解锁
   ↓
   用户输入主密码 → bw unlock --raw → 获取会话密钥
   ↓
   缓存会话密钥，后续命令使用 --session
```

### 8. 图标服务 (`IconService.cs`)

使用记忆化缓存优化图标加载性能：

```csharp
public static class IconService
{
    // 内存缓存（最多 200 个域名）
    private static readonly Dictionary<string, IconInfo> _iconCache = new();

    // 已知不可用的域名（避免无效请求）
    private static readonly HashSet<string> _unavailableIconDomains = new()
    {
        "adobe.com", "account.adobe.com"
    };

    public static IconInfo GetItemIcon(BitwardenItem item)
    {
        if (item.ItemType != BitwardenItemType.Login)
            return GetDefaultIcon(item.ItemType);

        var domain = ExtractDomainFromItem(item);
        if (string.IsNullOrEmpty(domain))
            return DefaultWebIcon;

        // 检查缓存
        if (_iconCache.TryGetValue(domain, out var cachedIcon))
            return cachedIcon;

        // 检查不可用列表
        if (_unavailableIconDomains.Contains(domain))
            return DefaultWebIcon;

        // 生成图标 URL
        var iconUrl = $"https://icons.bitwarden.net/{domain}/icon.png";
        var iconInfo = new IconInfo(iconUrl);

        // 管理缓存大小
        if (_iconCache.Count >= MaxCacheSize)
            RemoveOldCacheEntries();

        _iconCache[domain] = iconInfo;
        return iconInfo;
    }

    // 域名提取逻辑（处理各种 URL 格式）
    private static string? ExtractDomainFromHostname(string hostname)
    {
        // 提取注册域名（accounts.google.com → google.com）
        // 支持两字 TLD（co.uk, com.au 等）
    }
}
```

**图标获取流程**：

```
登录项目 → 提取域名 → 检查缓存
    ↓
    已缓存 → 返回图标 URL
    ↓
    未缓存 → 检查不可用列表
        ↓
        不可用 → 返回默认图标
        ↓
        可用 → 生成 URL 并缓存
```

### 9. 设置管理 (`SettingsManager.cs`)

单例模式，管理所有配置：

```csharp
public sealed class SettingsManager
{
    public static SettingsManager Instance { get; } = new();

    public string BwPath { get; set; } = "bw";
    public string CustomEnvironment { get; set; } = string.Empty;
    public string BwClientId { get; set; } = string.Empty;
    public string BwClientSecret { get; set; } = string.Empty;

    // 解析环境变量（支持自定义格式）
    public Dictionary<string, string> GetEnvironmentVariables()
    {
        var result = new Dictionary<string, string>();

        // 添加 API Key
        if (!string.IsNullOrWhiteSpace(BwClientId))
            result["BW_CLIENTID"] = BwClientId;
        if (!string.IsNullOrWhiteSpace(BwClientSecret))
            result["BW_CLIENTSECRET"] = BwClientSecret;

        // 解析自定义环境变量（KEY1=VALUE1;KEY2=VALUE2）
        var pairs = CustomEnvironment.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
                result[parts[0].Trim()] = parts[1].Trim();
        }

        return result;
    }
}
```

### 10. 数据模型

#### BitwardenItem.cs

```csharp
public class BitwardenItem
{
    public string? Id { get; set; }
    public int Type { get; set; }  // 1=Login, 2=SecureNote, 3=Card, 4=Identity
    public string? Name { get; set; }
    public string? Notes { get; set; }
    public bool Favorite { get; set; }
    public string? FolderId { get; set; }

    public BitwardenLogin? Login { get; set; }
    public BitwardenCard? Card { get; set; }
    public BitwardenIdentity? Identity { get; set; }
    public BitwardenField[]? Fields { get; set; }

    [JsonIgnore]
    public BitwardenItemType ItemType => (BitwardenItemType)Type;
}

// Login, Card, Identity, Field 等子模型...
```

#### BitwardenStatus.cs

```csharp
public class BitwardenStatus
{
    public string? Status { get; set; }  // "unlocked", "locked", "unauthenticated"
    public string? UserEmail { get; set; }
    public DateTime? LastSync { get; set; }

    [JsonIgnore]
    public bool IsUnlocked => Status?.ToLower() == "unlocked";

    [JsonIgnore]
    public bool IsLocked => Status?.ToLower() == "locked";

    [JsonIgnore]
    public bool IsLoggedOut => Status?.ToLower() == "unauthenticated";
}
```

### 11. 命令系统 (`ItemCommands.cs`)

所有操作都继承自 `InvokableCommand`：

```csharp
// 基础命令模式
internal sealed partial class CopyPasswordCommand : InvokableCommand
{
    private readonly BitwardenItem _item;

    public CopyPasswordCommand(BitwardenItem item)
    {
        _item = item;
        Name = ResourceHelper.CommandCopyPassword;
        Icon = new IconInfo("\uE8C8"); // Fluent UI 图标
    }

    public override CommandResult Invoke()
    {
        ClipboardHelper.SetText(_item.Login.Password);
        return CommandResult.Dismiss();  // 关闭并返回
    }
}

// TOTP 命令（需要调用 CLI）
internal sealed partial class CopyTotpCommand : InvokableCommand
{
    public override CommandResult Invoke()
    {
        var totp = BitwardenCliService.Instance.GetTotpAsync(_item.Id).Result;
        if (!string.IsNullOrEmpty(totp))
        {
            ClipboardHelper.SetText(totp);
            return CommandResult.ShowToast(ResourceHelper.ToastTotpCopied(totp));
        }
        return CommandResult.ShowToast(ResourceHelper.ToastTotpFailed);
    }
}

// OFF 命令（直接在页面内执行）
internal sealed partial class InlineCommand : InvokableCommand
{
    private readonly Func<CommandResult> _action;

    public override CommandResult Invoke() => _action();
}
```

**命令分类**：

1. **登录项目命令**：
   - 复制密码、用户名、URL
   - 打开 URL
   - 复制 TOTP

2. **银行卡命令**：
   - 复制卡号、CVV、有效期、持卡人姓名

3. **身份信息命令**：
   - 复制姓名、邮箱、电话、地址、公司
   - 复制 SSN、护照号、驾照号

4. **安全笔记命令**：
   - 复制笔记内容

5. **自定义字段命令**：
   - 动态生成，复制字段值

6. **操作命令**：
   - 同步、锁定、刷新、应用筛选

## 工作流程

### 1. 首次使用流程

```
用户打开 Command Palette
    ↓
找到 Bitwarden For Command Palette
    ↓
首次加载扩展
    ├─ 检查设置（BwPath, API Keys）
    ├─ 执行 bw status
    ├─ 状态判断：
    │   ├─ 未登录 → 显示"未登录"
    │   ├─ 已锁定 → 显示"解锁"按钮
    │   └─ 已解锁 → 加载并显示项目
    └─ 渲染 UI
```

### 2. 解锁流程

```
点击"解锁" → 导航到 UnlockPage
    ↓
输入主密码 → 点击"解锁"按钮
    ↓
UnlockPage.SubmitForm()
    ↓
BitwardenCliService.UnlockAsync(masterPassword)
    ├─ 执行: bw unlock "password" --raw
    ├─ 成功 → 获取会话密钥并缓存
    └─ 失败 → 显示错误 Toast
    ↓
成功 → Callback 通知主页面
    ↓
主页面调用 LoadItemsAsync()
    ↓
BitwardenCliService.GetItemsAsync()
    ├─ 执行: bw list items --session "key"
    └─ 返回项目列表
    ↓
显示项目列表
```

### 3. 搜索流程

```
用户在搜索框输入文字
    ↓
主页面 UpdateSearchText() 被调用
    ↓
GetItems() 触发重新渲染
    ↓
FilterItems() 过滤项目
    ├─ 名称匹配（忽略大小写）
    ├─ 用户名匹配
    └─ URL 匹配
    ↓
显示过滤后的列表
```

### 4. 筛选流程

```
点击"筛选"按钮 → 导航到 FilterPage
    ↓
FilterPage 加载文件夹列表
    ├─ 使用当前会话
    └─ 执行: bw list folders --session
    ↓
显示筛选选项列表
    ├─ 所有项目
    ├─ 仅收藏
    ├─ 项目类型（登录、银行卡等）
    └─ 文件夹（动态）
    ↓
用户点击筛选项 → ApplyFilterCommand
    ↓
回调主页面 → 更新 _currentFilter
    ↓
主页面重新渲染（GetItems()）
    ↓
FilterItems() 应用所有筛选条件
    ↓
显示筛选结果
```

### 5. 复制操作流程

```
用户右键项目或选择操作
    ↓
执行对应 CopyXXXCommand.Invoke()
    ↓
从模型获取值 → ClipboardHelper.SetText()
    ↓
返回 CommandResult.Dismiss() 或 ShowToast()
    ↓
显示成功提示并关闭
```

### 6. 同步/锁定流程

```
点击"同步"按钮 → SyncVaultCommand
    ↓
BitwardenCliService.SyncAsync()
    ├─ 执行: bw sync --session "key"
    ↓
成功 → 显示 Toast
    ↓
刷新项目列表（LoadItemsAsync()）

点击"锁定"按钮 → LockVaultCommand
    ↓
BitwardenCliService.LockAsync()
    ├─ 执行: bw lock
    ├─ 清除本地 SessionKey
    ↓
显示 Toast
    ↓
主页面刷新 → 显示"解锁"按钮
```

## 性能优化

### 1. 图标缓存

- **内存缓存**：最多缓存 200 个域名的图标 URL
- **不可用列表**：已知无图标的域名直接返回默认图标
- **域名提取**：智能提取注册域名，避免重复请求

### 2. 异步执行

- 使用 `async/await` 避免 UI 阻塞
- 异步加载项目和文件夹
- 流式处理 CLI 输出

### 3. 会话管理

- 缓存 `SessionKey` 避免重复解锁
- 自动附加会话参数

## 多语言支持

使用 Windows ResourceLoader 实现本地化：

```csharp
public static class ResourceHelper
{
    private static readonly ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse();

    public static string AppDisplayName => GetString("AppDisplayName");
    public static string ActionUnlock => GetString("ActionUnlock");
    public static string ToastTotpCopied(string code) => string.Format(GetString("ToastTotpCopied"), code);

    public static string GetString(string key, params object[] args)
    {
        var value = _loader.GetString(key);
        return args.Length > 0 ? string.Format(value, args) : value;
    }
}
```

**资源文件结构**（`.resw`）：

```xml
<data name="AppDisplayName" xml:space="preserve">
  <value>Bitwarden For Command Palette</value>
</data>
<data name="CommandCopyPassword" xml:space="preserve">
  <value>复制密码</value>
</data>
```

## 安全考虑

### 1. 凭据处理

- **API Key**：通过环境变量传递，不存储在文件中
- **主密码**：仅在解锁时短暂使用，不持久化
- **会话密钥**：存储在内存中，程序退出即清除

### 2. 命令行安全

- 密码参数使用引号转义
- 不记录敏感信息到调试输出
- 创建无窗口进程 (CreateNoWindow)

### 3. 离线支持

- 所有操作基于本地 CLI
- 无需网络连接查看/复制已有数据
- 同步操作需要网络

## 错误处理

### 1. CLI 执行错误

```csharp
var (output, error, exitCode) = await ExecuteCommandAsync("...");

if (exitCode != 0)
{
    Debug.WriteLine($"bw failed: {error}");
    return null;
}
```

### 2. JSON 解析错误

```csharp
try
{
    return JsonSerializer.Deserialize(output, ...);
}
catch (JsonException ex)
{
    Debug.WriteLine($"Failed to parse: {ex.Message}");
    return null;
}
```

### 3. UI 错误状态

- 显示友好的错误消息
- 提供刷新操作
- 保持界面可交互

## 扩展性

### 1. 添加新命令类型

```csharp
// 1. 在 Commands/ 创建新命令类
internal sealed partial class MyNewCommand : InvokableCommand
{
    public override CommandResult Invoke() { ... }
}

// 2. 在主页面的 GetContextCommands() 中添加
private static ICommandContextItem[] GetContextCommands(BitwardenItem item)
{
    var commands = new List<ICommandContextItem>();

    if (/* 某些条件 */)
    {
        commands.Add(new CommandContextItem(new MyNewCommand(item)));
    }

    return commands.ToArray();
}
```

### 2. 添加新筛选选项

```csharp
// 在 FilterPage.GetItems() 添加选项
items.Add(new ListItem(new ApplyFilterCommand(
    new VaultFilter { MyFilterProperty = value },
    _onFilterApplied))
{
    Title = "我的筛选",
    ...
});

// 在主页面 FilterItems() 应用筛选
if (filter.MyFilterProperty != null)
    result = result.Where(item => /* 应用筛选逻辑 */);
```

### 3. 添加新语言

1. 在 `Strings/` 创建新文件夹（如 `ja-JP`）
2. 复制 `en-US/Resources.resw`
3. 翻译所有 `<value>` 内容
4. 重新构建

## 开发调试

### 1. 构建

```powershell
# x64
dotnet build -p:Platform=x64

# ARM64
dotnet build -p:Platform=ARM64
```

### 2. 部署

```powershell
# Visual Studio: 右键项目 → 部署
# 或使用命令
dotnet deploy
```

### 3. 调试

1. 部署扩展
2. 在 Command Palette 中执行 `Reload`
3. 设置断点
4. 附加到 `BitwardenForCommandPalette.exe` 进程
5. 触发扩展操作

### 4. 日志

使用 `Debug.WriteLine()` 输出到调试窗口：

```csharp
Debug.WriteLine($"状态: {status}");
Debug.WriteLine($"错误: {error}");
```

## 技术栈总结

| 组件 | 技术 | 说明 |
|------|------|------|
| 语言 | C# 12 | .NET 9.0 |
| 框架 | .NET 9.0 | Windows 10.0.26100.0 |
| UI | Microsoft.CommandPalette.Extensions.Toolkit | PowerToys 扩展 SDK |
| 认证 | Bitwarden CLI Session | 会话密钥机制 |
| JSON | System.Text.Json | AOT 兼容 |
| 本地化 | WinRT ResourceLoader | .resw 文件 |
| 图标 | Fluent UI 图标 | Unicode 码点 |
| 容器 | WinRT COM Server | 进程外服务 |

## 性能指标

- **首次启动**：~1-2 秒（加载 CLI + 状态检查）
- **解锁时间**：~500ms（取决于密码强度）
- **项目加载**：~200-500ms（取决于项目数量）
- **搜索响应**：即时（内存操作）
- **图标加载**：缓存后即时，首次 ~100ms

## 许可证

MIT License - 允许自由使用、修改和分发。

## 贡献指南

1. Fork 项目
2. 创建特性分支
3. 提交更改
4. 创建 Pull Request

---

**文档维护者**：基于项目代码分析生成
**最后更新**：2025-12-17
