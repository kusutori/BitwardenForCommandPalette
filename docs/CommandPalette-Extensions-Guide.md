# Microsoft.CommandPalette.Extensions 开发指南

> 基于 BitwardenForCommandPalette 项目实践总结的 PowerToys Command Palette 扩展开发指南

## 目录

- [概述](#概述)
- [项目结构](#项目结构)
- [核心概念](#核心概念)
- [快速开始](#快速开始)
- [详细 API 参考](#详细-api-参考)
- [实践示例](#实践示例)
- [调试与部署](#调试与部署)

---

## 概述

### 什么是 Command Palette Extensions？

Command Palette 是 PowerToys 提供的一个快速启动器，类似于 macOS 的 Spotlight 或 Alfred。Extensions SDK 允许开发者创建自定义扩展，为 Command Palette 添加新功能。

### 两个核心命名空间

| 命名空间 | 说明 |
|---------|------|
| `Microsoft.CommandPalette.Extensions` | 原始 WinRT 接口，定义了扩展与 Command Palette 通信的契约 |
| `Microsoft.CommandPalette.Extensions.Toolkit` | C# 帮助类库，简化扩展开发 |

### 技术要求

- **.NET 10.0** 或更高版本
- **Windows 10.0.19041.0** 或更高版本
- **MSIX 打包** - 扩展必须打包为 MSIX
- **COM 服务器** - 扩展通过 COM 与 Command Palette 通信

---

## 项目结构

一个典型的 Command Palette 扩展项目结构如下：

```
MyExtension/
├── MyExtension.csproj          # 项目文件
├── MyExtension.cs              # 扩展入口点 (IExtension)
├── MyExtensionCommandsProvider.cs  # 命令提供者 (CommandProvider)
├── Program.cs                  # COM 服务器启动
├── Package.appxmanifest        # MSIX 清单
├── app.manifest                # 应用程序清单
├── Commands/                   # 命令类
│   └── MyCommands.cs
├── Pages/                      # 页面类
│   ├── MainPage.cs
│   └── DetailPage.cs
├── Models/                     # 数据模型
│   └── MyModel.cs
├── Services/                   # 业务服务
│   └── MyService.cs
└── Assets/                     # 图标资源
    ├── StoreLogo.png
    └── Square44x44Logo.png
```

---

## 核心概念

### 1. IExtension - 扩展入口点

每个扩展必须实现 `IExtension` 接口，这是 Command Palette 加载扩展的入口：

```csharp
[Guid("YOUR-GUID-HERE")]
public sealed partial class MyExtension : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;
    private readonly MyCommandsProvider _provider = new();

    public MyExtension(ManualResetEvent extensionDisposedEvent)
    {
        _extensionDisposedEvent = extensionDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Commands => _provider,  // 返回命令提供者
            _ => null,
        };
    }

    public void Dispose() => _extensionDisposedEvent.Set();
}
```

### 2. CommandProvider - 命令提供者

命令提供者定义了扩展在 Command Palette 中显示的入口：

```csharp
public partial class MyCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public MyCommandsProvider()
    {
        DisplayName = "My Extension";  // 显示名称
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");  // 图标
        _commands = [
            new CommandItem(new MainPage()) { Title = DisplayName },
        ];
    }

    public override ICommandItem[] TopLevelCommands() => _commands;
}
```

### 3. Page - 页面类型

Command Palette 支持多种页面类型：

| 页面类型 | 基类 | 用途 |
|---------|------|------|
| **ListPage** | `ListPage` | 静态列表页面 |
| **DynamicListPage** | `DynamicListPage` | 动态列表页面（支持搜索、刷新） |
| **ContentPage** | `ContentPage` | 内容页面（表单、Markdown 等） |
| **MarkdownPage** | - | Markdown 内容页面 |

#### DynamicListPage 示例

```csharp
internal sealed partial class MainPage : DynamicListPage
{
    public MainPage()
    {
        Icon = new IconInfo("\uE8A1");
        Title = "My Page";
        Name = "Open";
        PlaceholderText = "Search...";
    }

    // 必须实现：响应搜索文本变化
    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        RaiseItemsChanged();  // 通知列表需要刷新
    }

    // 必须实现：返回列表项
    public override IListItem[] GetItems()
    {
        // 可以根据 SearchText 属性过滤结果
        return [
            new ListItem(new MyCommand()) { Title = "Item 1" },
            new ListItem(new MyCommand()) { Title = "Item 2" },
        ];
    }
}
```

#### ContentPage 示例（表单）

```csharp
internal sealed partial class FormPage : ContentPage
{
    public FormPage()
    {
        Title = "Enter Data";
        Name = "Form";
    }

    public override IContent[] GetContent() => [new MyForm()];
}

internal sealed partial class MyForm : FormContent
{
    public MyForm()
    {
        TemplateJson = """
        {
            "type": "AdaptiveCard",
            "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
            "version": "1.5",
            "body": [
                {
                    "type": "Input.Text",
                    "id": "inputField",
                    "label": "Enter value",
                    "isRequired": true
                }
            ],
            "actions": [
                {
                    "type": "Action.Submit",
                    "title": "Submit",
                    "style": "positive"
                }
            ]
        }
        """;
    }

    public override CommandResult SubmitForm(string payload)
    {
        var formData = JsonNode.Parse(payload)?.AsObject();
        var value = formData?["inputField"]?.GetValue<string>();
        
        // 处理表单数据...
        
        return CommandResult.GoBack();  // 返回上一页
    }
}
```

### 4. Command - 命令类型

命令是用户可以执行的操作：

#### InvokableCommand - 可调用命令

```csharp
internal sealed partial class CopyCommand : InvokableCommand
{
    private readonly string _text;

    public CopyCommand(string text)
    {
        _text = text;
        Name = "Copy";
        Icon = new IconInfo("\uE8C8");  // Segoe MDL2 图标
    }

    public override CommandResult Invoke()
    {
        ClipboardHelper.SetText(_text);
        return CommandResult.Dismiss();  // 关闭 Command Palette
    }
}
```

#### CommandResult 类型

| 方法 | 效果 |
|------|------|
| `CommandResult.Dismiss()` | 关闭 Command Palette |
| `CommandResult.KeepOpen()` | 保持打开状态 |
| `CommandResult.GoBack()` | 返回上一页 |
| `CommandResult.GoHome()` | 返回首页 |
| `CommandResult.ShowToast(message)` | 显示 Toast 通知 |
| `CommandResult.GoToPage(page)` | 导航到指定页面 |
| `CommandResult.Confirm(args)` | 显示确认对话框 |

### 5. ListItem - 列表项

```csharp
var item = new ListItem(new MyCommand())
{
    Title = "Item Title",           // 标题
    Subtitle = "Item description",  // 副标题
    Icon = new IconInfo("\uE8A1"),  // 图标
    Tags = [new Tag { Text = "Tag1" }],  // 标签
    MoreCommands = [                // 右键菜单
        new CommandContextItem(new CopyCommand()),
        new CommandContextItem(new DeleteCommand()),
    ]
};
```

### 6. IconInfo - 图标

支持多种图标类型：

```csharp
// Segoe MDL2 字体图标
new IconInfo("\uE8A1")

// 相对路径图片
IconHelpers.FromRelativePath("Assets\\icon.png")

// URL 图片
new IconInfo(new Uri("https://example.com/icon.png"))

// 带深浅主题的图标
new IconInfo {
    Light = new IconData { Path = "Assets\\icon-light.png" },
    Dark = new IconData { Path = "Assets\\icon-dark.png" }
}
```

常用 Segoe MDL2 图标代码：

| 图标 | 代码 | 说明 |
|------|------|------|
| 🔒 | `\uE72E` | 锁定 |
| 🔓 | `\uE785` | 解锁 |
| 📋 | `\uE8C8` | 复制 |
| 🔄 | `\uE895` | 同步 |
| ⭐ | `\uE734` | 收藏 |
| 🔍 | `\uE71C` | 筛选 |
| 👤 | `\uE77B` | 用户 |
| 💳 | `\uE8C7` | 信用卡 |
| 📝 | `\uE8A0` | 笔记 |
| 🔗 | `\uE71B` | 链接 |
| 🌐 | `\uE774` | 地球 |
| ⚙️ | `\uE713` | 设置 |
| ❌ | `\uE711` | 关闭 |
| ✓ | `\uE73E` | 勾选 |

---

## 快速开始

### 1. 创建项目

```xml
<!-- MyExtension.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.19041.0</TargetPlatformMinVersion>
    <RuntimeIdentifiers>win-x64;win-arm64</RuntimeIdentifiers>
    <EnableMsixTooling>true</EnableMsixTooling>
    <Nullable>enable</Nullable>
    <ApplicationManifest>app.manifest</ApplicationManifest>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CommandPalette.Extensions" />
    <PackageReference Include="Microsoft.Windows.CsWinRT" />
    <PackageReference Include="Shmuelie.WinRTServer" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools.MSIX">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <Content Include="Assets\*.png" />
  </ItemGroup>
</Project>
```

### 2. 配置 Package.appxmanifest

```xml
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3"
  xmlns:com="http://schemas.microsoft.com/appx/manifest/com/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap uap3 rescap">

  <Identity Name="MyExtension" Publisher="CN=Publisher" Version="1.0.0.0" />

  <Properties>
    <DisplayName>My Extension</DisplayName>
    <PublisherDisplayName>Publisher</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.19041.0" />
  </Dependencies>

  <Resources>
    <Resource Language="x-generate"/>
  </Resources>

  <Applications>
    <Application Id="App" Executable="$targetnametoken$.exe" EntryPoint="$targetentrypoint$">
      <uap:VisualElements
        DisplayName="My Extension"
        Description="My Extension Description"
        BackgroundColor="transparent"
        Square150x150Logo="Assets\Square150x150Logo.png"
        Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
      
      <Extensions>
        <!-- COM 服务器注册 -->
        <com:Extension Category="windows.comServer">
          <com:ComServer>
            <com:ExeServer Executable="MyExtension.exe" Arguments="-RegisterProcessAsComServer" DisplayName="My Extension">
              <com:Class Id="YOUR-GUID-HERE" DisplayName="My Extension" />
            </com:ExeServer>
          </com:ComServer>
        </com:Extension>
        
        <!-- Command Palette 扩展注册 -->
        <uap3:Extension Category="windows.appExtension">
          <uap3:AppExtension Name="com.microsoft.commandpalette" Id="ID" PublicFolder="Public"
            DisplayName="My Extension" Description="My Extension Description">
            <uap3:Properties>
              <CmdPalProvider>
                <Activation>
                  <CreateInstance ClassId="YOUR-GUID-HERE" />
                </Activation>
                <SupportedInterfaces>
                  <Commands/>
                </SupportedInterfaces>
              </CmdPalProvider>
            </uap3:Properties>
          </uap3:AppExtension>
        </uap3:Extension>
      </Extensions>
    </Application>
  </Applications>

  <Capabilities>
    <Capability Name="internetClient" />
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
```

### 3. 创建 Program.cs

```csharp
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.Hosting;

namespace MyExtension;

public sealed class Program
{
    [MTAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseComServer(options =>
                {
                    options.Assemblies = [typeof(MyExtension).Assembly];
                })
                .Build();
            host.Run();
        }
    }
}
```

---

## 详细 API 参考

### Toolkit 辅助类

| 类 | 用途 |
|---|------|
| `ClipboardHelper` | 剪贴板操作 |
| `IconHelpers` | 图标加载辅助 |
| `ShellHelpers` | Shell 操作（打开文件、URL 等） |
| `ColorHelpers` | 颜色操作 |
| `StringMatcher` | 字符串匹配 |

### 常用操作示例

```csharp
// 复制到剪贴板
ClipboardHelper.SetText("text to copy");

// 打开 URL
Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

// 加载图标
var icon = IconHelpers.FromRelativePath("Assets\\icon.png");

// 显示 Toast
return CommandResult.ShowToast("Operation completed!");
```

---

## 实践示例

### 完整的动态列表页面

```csharp
internal sealed partial class VaultPage : DynamicListPage
{
    private Item[]? _items;
    private bool _isLoading;

    public VaultPage()
    {
        Icon = new IconInfo("\uE8A1");
        Title = "My Vault";
        PlaceholderText = "Search items...";
        _ = LoadItemsAsync();
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems()
    {
        if (_isLoading)
            return [new ListItem(new NoOpCommand()) { Title = "Loading..." }];

        if (_items == null || _items.Length == 0)
            return [new ListItem(new NoOpCommand()) { Title = "No items found" }];

        return _items
            .Where(item => string.IsNullOrEmpty(SearchText) || 
                           item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .Select(item => new ListItem(new CopyCommand(item.Value))
            {
                Title = item.Name,
                Subtitle = item.Description,
                Icon = new IconInfo("\uE8A1"),
                MoreCommands = [
                    new CommandContextItem(new CopyCommand(item.Value)),
                    new CommandContextItem(new DeleteCommand(item.Id)),
                ]
            })
            .ToArray();
    }

    private async Task LoadItemsAsync()
    {
        _isLoading = true;
        RaiseItemsChanged();

        try
        {
            _items = await MyService.GetItemsAsync();
        }
        finally
        {
            _isLoading = false;
            RaiseItemsChanged();
        }
    }
}
```

---

## 调试与部署

### 构建

```bash
# x64 架构
dotnet build -p:Platform=x64

# ARM64 架构
dotnet build -p:Platform=ARM64
```

### 部署

1. 在 Visual Studio 中右键项目 → **部署（Deploy）**
2. 或使用命令行：`dotnet publish`

### 调试

1. 部署扩展
2. 在 Command Palette 中运行 `Reload` 命令
3. 在 Visual Studio 中附加到 `YourExtension.exe` 进程

### 常见问题

1. **扩展不显示**：检查 `Package.appxmanifest` 中的 GUID 是否与代码中一致
2. **图标不显示**：确保图标文件包含在项目中并设置为 `Content`
3. **COM 错误**：确保 `Program.cs` 正确处理 `-RegisterProcessAsComServer` 参数

---

## 参考链接

- [官方文档 - Microsoft.CommandPalette.Extensions](https://learn.microsoft.com/zh-cn/windows/powertoys/command-palette/microsoft-commandpalette-extensions/microsoft-commandpalette-extensions)
- [官方文档 - Microsoft.CommandPalette.Extensions.Toolkit](https://learn.microsoft.com/zh-cn/windows/powertoys/command-palette/microsoft-commandpalette-extensions-toolkit/microsoft-commandpalette-extensions-toolkit)
- [PowerToys GitHub](https://github.com/microsoft/PowerToys)
- [Segoe MDL2 图标列表](https://learn.microsoft.com/zh-cn/windows/apps/design/style/segoe-ui-symbol-font)
