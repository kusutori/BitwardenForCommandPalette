# AGENTS.md

## 项目简介
- 项目名：Bitwarden For Command Palette
- 类型：Windows PowerToys Command Palette 扩展（.NET/C#）
- 主要功能：通过 Bitwarden CLI（`bw`）访问和管理密码库

## 仓库结构
- `BitwardenForCommandPalette/`：主项目源码
- `docs/`：架构、开发和排障文档
- `README.md`：功能与使用说明

## 项目 Tree（源码与文档）
> 说明：以下 tree 省略构建产物和 IDE 临时目录（如 `bin/`、`obj/`、`.vs/`、`.idea/`）。

```text
BitwardenForCommandPalette/
├── .gitignore
├── AGENTS.md
├── BitwardenForCommandPalette.slnx
├── CLAUDE.md
├── Directory.Build.props
├── Directory.Packages.props
├── LICENSE
├── LOCALIZATION_SUMMARY.md
├── nuget.config
├── README.md
├── THIRD-PARTY-NOTICES.md
├── docs/
│   ├── Adaptive-Cards-Guide.md
│   ├── ARCHITECTURE.md
│   ├── Bitwarden-CLI-Guide.md
│   ├── CHANGELOG.md
│   ├── CommandPalette-Extensions-Guide.md
│   ├── Localization-Guide.md
│   ├── Project-Architecture.md
│   ├── Troubleshooting.md
│   ├── 动态标题和进度条实现.md
│   └── 实现细节说明.md
└── BitwardenForCommandPalette/
    ├── app.manifest
    ├── BitwardenForCommandPalette.cs
    ├── BitwardenForCommandPalette.csproj
    ├── BitwardenForCommandPaletteCommandsProvider.cs
    ├── Package.appxmanifest
    ├── Program.cs
    ├── Assets/
    │   ├── app.ico
    │   ├── LockScreenLogo.scale-200.png
    │   ├── SplashScreen.scale-200.png
    │   ├── Square150x150Logo.scale-200.png
    │   ├── Square44x44Logo.scale-200.png
    │   ├── Square44x44Logo.targetsize-24_altform-unplated.png
    │   ├── StoreLogo.png
    │   └── Wide310x150Logo.scale-200.png
    ├── Commands/
    │   └── ItemCommands.cs
    ├── Helpers/
    │   ├── ExtensionHostHelper.cs
    │   ├── ResourceHelper.cs
    │   └── StatusMessageExtensions.cs
    ├── Models/
    │   ├── BitwardenItem.cs
    │   └── BitwardenStatus.cs
    ├── Pages/
    │   ├── BitwardenForCommandPalettePage.cs
    │   ├── CreateItemPage.cs
    │   ├── EditItemPage.cs
    │   ├── GeneratorPage.cs
    │   ├── TotpPage.cs
    │   ├── UnlockPage.cs
    │   └── VaultFilters.cs
    ├── Properties/
    │   ├── launchSettings.json
    │   └── PublishProfiles/
    │       ├── win-arm64.pubxml
    │       └── win-x64.pubxml
    ├── Services/
    │   ├── BitwardenCliService.cs
    │   ├── IconService.cs
    │   └── SettingsManager.cs
    ├── Strings/
    │   ├── en-US/Resources.resw
    │   ├── es-ES/Resources.resw
    │   ├── fr-FR/Resources.resw
    │   ├── ja-JP/Resources.resw
    │   ├── ko-KR/Resources.resw
    │   └── zh-CN/Resources.resw
    └── Vault/
        ├── ItemDetailsGenerator.cs
        ├── VaultContextCommands.cs
        ├── VaultItemsLoader.cs
        ├── VaultListBuilder.cs
        ├── VaultPageCommands.cs
        └── VaultPageHelpers.cs
```

## 本地开发
1. 环境要求：Windows + Visual Studio 2026 + PowerToys + Bitwarden CLI
2. 解决方案文件：`BitwardenForCommandPalette.slnx`
3. 主要依赖配置：`Directory.Packages.props`
4. 常用命令：
   - 还原：`dotnet restore`
   - 构建：`dotnet build -p:Platform=x64`

## 代码约定
- 使用现有目录边界组织代码（`Services/`、`Pages/`、`Commands/`、`Models/`、`Helpers/`）。
- 新功能优先复用现有服务，避免在页面层直接堆叠业务逻辑。
- 涉及文本显示的修改，需同步检查 `Strings/*/Resources.resw` 的多语言资源。
- 涉及 Bitwarden CLI 调用时，统一通过 `Services/BitwardenCliService.cs` 相关抽象处理。

## 提交前检查
- 确认 `dotnet build -p:Platform=x64` 通过。
- 若改动用户可见文本，确认至少 `en-US` 与 `zh-CN` 资源完整。
- 若改动命令行为，更新 `README.md` 或 `docs/` 中对应说明。

## 参考文档
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/Project-Architecture.md`
- `docs/Troubleshooting.md`
