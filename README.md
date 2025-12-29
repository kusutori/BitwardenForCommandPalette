# Bitwarden For Command Palette

一个用于 Windows PowerToys Command Palette 的 Bitwarden 密码管理器扩展。通过本地 Bitwarden CLI (`bw`) 与您的密码库进行交互。

[📖 更新日志](docs/CHANGELOG.md) · [🏗️ 架构文档](docs/ARCHITECTURE.md) · [📘 开发指南](docs/Project-Architecture.md)

## 功能特性

### 已实现 ✅

- [x] **密码库状态检测** - 自动检测 Bitwarden CLI 是否已安装和登录状态
- [x] **密码库解锁** - 支持通过主密码解锁密码库
- [x] **密码库同步** - 手动同步密码库与服务器
- [x] **锁定密码库** - 手动锁定密码库
- [x] **列出所有项目** - 显示密码库中的所有项目（登录、银行卡、身份、安全笔记）
- [x] **网站图标** - 自动获取并显示登录项的网站图标（使用 Bitwarden 官方图标服务）
- [x] **搜索功能** - 支持按名称、用户名、URL 搜索项目
- [x] **收藏夹筛选** - 只显示收藏的项目
- [x] **文件夹筛选** - 按文件夹筛选项目
- [x] **类型筛选** - 按项目类型（登录、银行卡、身份、安全笔记）筛选
- [x] **登录项支持**
  - 复制密码、用户名、URL
  - 打开 URL
  - **TOTP 支持** - 生成并复制 TOTP 验证码
- [x] **银行卡支持** - 复制卡号、CVV、有效期、持卡人姓名
- [x] **身份信息支持** - 复制姓名、邮箱、电话、地址、公司、SSN、护照号、驾照号
- [x] **安全笔记支持** - 复制笔记内容
- [x] **自定义字段支持** - 显示和复制自定义字段
- [x] **解锁后自动返回** - 解锁成功后自动返回密码库列表
- [x] **错误提示** - 操作失败时显示错误消息
- [x] **收藏标记** - 收藏的项目会显示 ⭐ 标记

- [x] **设置页面** - 在 Command Palette 设置中配置扩展
  - 自定义 Bitwarden CLI 路径
  - 配置自定义环境变量（支持自建服务器等场景）

### 待开发 📋

- [ ] **项目详情页** - 查看项目的完整详情
- [ ] **自动锁定** - 一段时间后自动锁定密码库
- [ ] **键盘快捷键** - 支持自定义快捷键操作
- [ ] **深色/浅色主题图标** - 根据系统主题显示不同图标
- [ ] **密码生成器** - 生成安全密码

## 前置要求

1. **Windows 11** 或支持 PowerToys Command Palette 的 Windows 版本
2. **PowerToys** 已安装并启用 Command Palette
3. **Bitwarden CLI** (`bw`) 已安装并在 PATH 中可用
   ```bash
   # 使用 winget 安装
   winget install Bitwarden.CLI
   
   # 或使用 npm 安装
   npm install -g @bitwarden/cli
   ```
4. **已登录 Bitwarden** - 首次使用前需要在终端中登录
   ```bash
   bw login
   ```

## 安装

1. 克隆或下载本项目
2. 使用 Visual Studio 2022 打开 `BitwardenForCommandPalette.sln`
3. 确保选择正确的平台（x64 或 ARM64）
4. 右键项目 → 部署（Deploy）
5. 在 Command Palette 中运行 `Reload` 命令

## 使用方法

### 基本使用

1. 打开 PowerToys Command Palette（默认快捷键：`Alt + Space`）
2. 找到 "Bitwarden For Command Palette" 并按 Enter
3. 如果密码库已锁定，按 Enter 进入解锁页面，输入主密码
4. 解锁后即可浏览所有密码项
5. 输入文字可搜索项目
6. 按 Enter 复制密码，或使用右键菜单查看更多操作

### 配置设置

1. 打开 PowerToys 设置 → Command Palette
2. 在扩展列表中找到 "Bitwarden For Command Palette"
3. 点击设置图标可配置：
   - **Bitwarden CLI 路径**：自定义 `bw.exe` 的路径（默认使用 PATH 中的 `bw`）
   - **Bitwarden API Client ID**：配置 API Key 认证的 Client ID（可选）
   - **Bitwarden API Client Secret**：配置 API Key 认证的 Client Secret（可选）
   - **自定义环境变量**：配置 Bitwarden CLI 的其他环境变量
     - `BW_SERVER`：自建 Bitwarden 服务器地址（如 `https://vault.example.com`）
     - `NODE_EXTRA_CA_CERTS`：自签名证书路径
     - 其他 Bitwarden CLI 支持的环境变量
   - 格式：`KEY1=VALUE1;KEY2=VALUE2`

### API Key 认证（推荐）

为了避免每次重启后重新登录，建议使用 API Key 认证：

1. 登录 Bitwarden Web 控制台
2. 进入 **设置** → **安全** → **密钥** (Settings → Security → Keys)
3. 在 "API 密钥" 部分查看或创建新的 API Key
4. 复制 **client_id** 和 **client_secret**
5. 在扩展设置中填入这两项：
   - 将 **client_id** 粘贴到 "Bitwarden API Client ID"
   - 将 **client_secret** 粘贴到 "Bitwarden API Client Secret"
6. 保存设置后，扩展会自动将这些凭据设置为环境变量 `BW_CLIENTID` 和 `BW_CLIENTSECRET`
7. Bitwarden CLI 会自动使用这些环境变量进行认证，无需手动 `bw login`

**注意**：
- 如果同时设置了 API Key 和使用 `bw login`，API Key 优先级更高
- API Key 仅用于认证（登录），解锁密码库仍需要主密码
- 首次使用 API Key 时，CLI 会自动执行类似 `bw login --apikey` 的操作

## 项目结构

```
BitwardenForCommandPalette/
├── BitwardenForCommandPalette.cs      # 扩展入口点
├── BitwardenForCommandPaletteCommandsProvider.cs  # 命令提供者
├── Program.cs                          # 程序入口
├── Commands/
│   └── ItemCommands.cs                # 复制密码、用户名等命令
├── Helpers/
│   └── ResourceHelper.cs              # 多语言资源助手
├── Models/
│   ├── BitwardenItem.cs               # 密码项数据模型
│   └── BitwardenStatus.cs             # 状态数据模型
├── Pages/
│   ├── BitwardenForCommandPalettePage.cs  # 主列表页面
│   ├── UnlockPage.cs                  # 解锁表单页面
│   └── FilterPage.cs                  # 筛选页面
├── Services/
│   ├── BitwardenCliService.cs         # Bitwarden CLI 封装服务
│   ├── IconService.cs                 # 网站图标服务（带缓存）
│   └── SettingsManager.cs             # 设置管理服务
└── Strings/
    ├── en-US/
    │   └── Resources.resw             # 英文资源
    └── zh-CN/
        └── Resources.resw             # 简体中文资源
```

## 技术栈

- **.NET 9.0** - Windows 10.0.26100.0
- **Microsoft.CommandPalette.Extensions** - PowerToys Command Palette 扩展 SDK
- **Bitwarden CLI** - 本地密码库交互
- **WinRT ResourceLoader** - 多语言本地化支持

## 多语言支持

本项目支持多语言界面，目前已实现：
- **英文 (en-US)** - 默认语言
- **简体中文 (zh-CN)** - 部分翻译

### 添加新语言

1. 在 `Strings/` 目录下创建新的语言文件夹（如 `ja-JP`）
2. 复制 `en-US/Resources.resw` 到新文件夹
3. 翻译 `<value>` 标签中的内容
4. 重新构建项目，资源会自动包含

### 资源结构

所有 UI 文本都存储在 `.resw` 文件中，通过 `ResourceHelper` 类访问。关键资源包括：
- `AppDisplayName` - 应用名称
- `Action*` - 操作按钮（复制、打开等）
- `Command*` - 命令名称
- `Toast*` - 提示消息
- `UnlockPage*`, `FilterPage*`, `MainPage*` - 页面相关文本
- `Status*` - 状态消息
- `Item*` - 项目类型和字段名称

## 性能优化

### 图标缓存
`IconService` 使用内存缓存来存储网站图标 URL，避免重复请求：
- 最多缓存 200 个图标
- 超出限制时自动清理旧条目
- 以域名为键进行缓存

## 开发说明

### 构建

```bash
dotnet build -p:Platform=x64
# 或
dotnet build -p:Platform=ARM64
```

### 调试

1. 在 Visual Studio 中设置断点
2. 部署应用
3. 在 Command Palette 中执行 Reload
4. 附加到 `BitwardenForCommandPalette.exe` 进程

## 许可证

MIT License - 详见 [LICENSE](LICENSE) 文件

## 贡献

欢迎提交 Issue 和 Pull Request！

## 致谢

- [Microsoft PowerToys](https://github.com/microsoft/PowerToys) - Command Palette 扩展框架
- [Bitwarden](https://bitwarden.com/) - 开源密码管理器
