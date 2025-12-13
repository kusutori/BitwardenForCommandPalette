# Bitwarden For Command Palette

一个用于 Windows PowerToys Command Palette 的 Bitwarden 密码管理器扩展。通过本地 Bitwarden CLI (`bw`) 与您的密码库进行交互。

## 功能特性

### 已实现 ✅

- [x] **密码库状态检测** - 自动检测 Bitwarden CLI 是否已安装和登录状态
- [x] **密码库解锁** - 支持通过主密码解锁密码库
- [x] **列出所有项目** - 显示密码库中的所有项目（登录、银行卡、身份、安全笔记）
- [x] **网站图标** - 自动获取并显示登录项的网站图标（使用 Bitwarden 官方图标服务）
- [x] **搜索功能** - 支持按名称、用户名、URL 搜索项目
- [x] **复制密码** - 一键复制密码到剪贴板
- [x] **复制用户名** - 一键复制用户名到剪贴板
- [x] **复制 URL** - 复制登录项的 URL
- [x] **打开 URL** - 在默认浏览器中打开登录项的 URL
- [x] **解锁后自动返回** - 解锁成功后自动返回密码库列表
- [x] **错误提示** - 解锁失败时显示错误消息

### 待开发 📋

- [ ] **TOTP 支持** - 生成并复制 TOTP 验证码（目前仅复制 TOTP 密钥）
- [ ] **密码库同步** - 手动同步密码库与服务器
- [ ] **锁定密码库** - 手动锁定密码库
- [ ] **项目详情页** - 查看项目的完整详情
- [ ] **银行卡支持** - 复制银行卡号、CVV 等
- [ ] **身份信息支持** - 复制身份信息字段
- [ ] **安全笔记支持** - 查看和复制安全笔记内容
- [ ] **自定义字段支持** - 显示和复制自定义字段
- [ ] **收藏夹筛选** - 只显示收藏的项目
- [ ] **文件夹筛选** - 按文件夹筛选项目
- [ ] **Bitwarden CLI 路径配置** - 自定义 `bw` 命令路径
- [ ] **自动锁定** - 一段时间后自动锁定密码库
- [ ] **键盘快捷键** - 支持自定义快捷键操作
- [ ] **深色/浅色主题图标** - 根据系统主题显示不同图标

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

1. 打开 PowerToys Command Palette（默认快捷键：`Alt + Space`）
2. 找到 "Bitwarden For Command Palette" 并按 Enter
3. 如果密码库已锁定，按 Enter 进入解锁页面，输入主密码
4. 解锁后即可浏览所有密码项
5. 输入文字可搜索项目
6. 按 Enter 复制密码，或使用右键菜单查看更多操作

## 项目结构

```
BitwardenForCommandPalette/
├── BitwardenForCommandPalette.cs      # 扩展入口点
├── BitwardenForCommandPaletteCommandsProvider.cs  # 命令提供者
├── Program.cs                          # 程序入口
├── Commands/
│   └── ItemCommands.cs                # 复制密码、用户名等命令
├── Models/
│   ├── BitwardenItem.cs               # 密码项数据模型
│   └── BitwardenStatus.cs             # 状态数据模型
├── Pages/
│   ├── BitwardenForCommandPalettePage.cs  # 主列表页面
│   └── UnlockPage.cs                  # 解锁表单页面
└── Services/
    └── BitwardenCliService.cs         # Bitwarden CLI 封装服务
```

## 技术栈

- **.NET 9.0** - Windows 10.0.26100.0
- **Microsoft.CommandPalette.Extensions** - PowerToys Command Palette 扩展 SDK
- **Bitwarden CLI** - 本地密码库交互

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
