# Bitwarden For Command Palette - 更新日志

本文件记录 Bitwarden For Command Palette 所有版本的详细更新内容、新功能、修复和改进。

---

## [Unreleased] - 待发布

### 🔧 优化
- 优化代码结构和性能

---

## [v1.2.0] - 2025-12-31

### ✨ 新增功能

#### 🔐 密码/口令生成器
- **密码生成器** (`PasswordGeneratorPage`)
  - 可配置长度（5-128 字符）
  - 可选大写字母、小写字母、数字、特殊字符
  - 一键生成并复制到剪贴板
- **口令生成器** (`PassphraseGeneratorPage`)
  - 可配置单词数量（3-20 个）
  - 可配置分隔符
  - 可选首字母大写、包含数字
  - 一键生成并复制到剪贴板

#### 📁 创建项目时选择文件夹
- 创建登录、银行卡、身份、安全笔记时可选择目标文件夹
- 使用 `Input.ChoiceSet` 下拉框动态加载文件夹列表
- 默认选项"无文件夹"

#### 📁 创建文件夹功能
- 在创建项目页面添加"创建文件夹"选项
- 简单的表单界面，只需输入文件夹名称
- 调用 `bw create folder` 命令

### 🌐 国际化
- 新增密码生成器相关字符串（en-US / zh-CN）
- 新增创建文件夹相关字符串
- 新增文件夹选择相关字符串

### 🔧 技术改进
- 使用 `JsonObject.ToJsonString()` 替代 `JsonSerializer.Serialize()` 避免 AOT 警告
- 使用 `JsonNode.Parse()` 动态构建 JSON 数组避免泛型警告
- 优化 Adaptive Cards 模板动态生成

### 📚 文档
- 更新 README.md 功能特性列表
- 全面更新 Project-Architecture.md 架构文档
- 新增 Troubleshooting.md 问题解决指南

---

## [v1.1.0] - 2025-12-28

### ✨ 新增功能

#### 🗑️ 回收站功能
- **查看回收站** - 在筛选页面添加"回收站"选项
- **恢复项目** (`RestoreCommand`) - 从回收站恢复已删除的项目
- **永久删除** (`PermanentDeleteCommand`) - 从回收站永久删除项目
- 回收站视图中的项目显示不同的命令菜单
- 删除命令显示为红色（`IsCritical = true`）

#### ✏️ 编辑项目功能
- **编辑项目** (`EditItemPage`) - 使用 Adaptive Cards 表单编辑现有项目
- 支持编辑所有项目类型（登录、银行卡、身份、安全笔记）
- 预填充现有数据
- 调用 `bw edit item` 命令

#### ➕ 创建项目功能
- **创建项目** (`CreateItemPage`) - 使用 Adaptive Cards 表单创建新项目
- 支持创建所有项目类型
- 类型选择页面 (`CreateItemTypeSelectorPage`)
- 调用 `bw create item` 命令

#### ⏱️ TOTP 页面增强
- **独立 TOTP 页面** (`TotpPage`) - 实时显示 TOTP 验证码
- 倒计时进度条显示
- 一键复制功能
- 每秒自动刷新

### 🔧 CLI 服务扩展
- `CreateItemAsync()` - 创建项目
- `EditItemAsync()` - 编辑项目
- `DeleteItemAsync()` - 删除项目（移到回收站）
- `RestoreItemAsync()` - 恢复项目
- `PermanentDeleteItemAsync()` - 永久删除项目

### 🌐 国际化
- 新增 CRUD 操作相关字符串
- 新增回收站相关字符串
- 新增 TOTP 页面相关字符串

---

## [v1.0.0] - 2025-12-16

### ✨ 新增功能
- **图标服务优化**：添加不可用域名列表，优化图标获取逻辑
  - 新增已知无图标的域名列表（adobe.com, account.adobe.com）
  - 对不可用域名自动使用默认网页图标
  - 优化图标加载的错误处理流程

### 📚 文档
- 更新相关文档以反映最新的图标服务改进

---

## [v0.9.0] - 2025-12-14

### ✨ 新增功能
- **设置管理功能**（核心功能）
  - **Bitwarden CLI 路径配置**：支持自定义 `bw.exe` 路径（默认使用 PATH）
  - **API Key 认证支持**：
    - Bitwarden API Client ID 配置
    - Bitwarden API Client Secret 配置
    - 自动设置 `BW_CLIENTID` 和 `BW_CLIENTSECRET` 环境变量
  - **自定义环境变量**：支持配置 Bitwarden CLI 的其他环境变量
    - 格式：`KEY1=VALUE1;KEY2=VALUE2`
    - 支持自建服务器（`BW_SERVER`）
    - 支持自签名证书（`NODE_EXTRA_CA_CERTS`）
  - 完整的设置 UI 集成到 Command Palette 设置页面

### 🔧 优化
- **BitwardenCliService.cs**：重构以支持自定义环境变量
  - 自动从 SettingsManager 读取配置
  - 将环境变量注入到 CLI 进程

### 🌐 国际化
- **新增字符串资源**：
  - 英文（en-US）：添加 26 个设置相关字符串
  - 中文（zh-CN）：添加 32 个设置相关字符串

### 📚 文档
- **README.md**：完整更新
  - 新增 API Key 认证章节
  - 详细说明设置配置方法
  - 添加自定义环境变量使用示例

---

## [v0.8.0] - 2025-12-14

### ✨ 新增功能
- **图标服务性能优化**
  - **内存缓存机制**：最多缓存 200 个域名的图标 URL
  - **缓存淘汰策略**：缓存满时自动移除一半最旧条目
  - **域名提取优化**：智能提取注册域名（`accounts.google.com` → `google.com`）

### 🔧 优化
- **IconService.cs**：
  - 完全重构
  - 添加缓存层
  - 优化域名提取算法
  - 支持复杂域名格式（两字 TLD 如 co.uk, com.au）

### 📚 文档
- **README.md**：
  - 新增性能优化章节
  - 详细说明图标缓存机制
  - 添加性能指标数据

---

## [v0.7.0] - 2025-12-14

### ✨ 国际化支持（重大更新）

#### 🌐 新增中文本地化
- **完整的中文翻译**：460+ 条资源字符串
- **双语支持**：英文（默认） + 简体中文
- **动态语言切换**：基于系统区域设置

#### 📦 资源文件结构
```
Strings/
├── en-US/
│   └── Resources.resw  (464 条)
└── zh-CN/
    └── Resources.resw  (464 条)
```

#### 🎯 本地化覆盖范围
- **应用名称**：AppDisplayName
- **操作按钮**：CommandCopy*, Action*, Toast*
- **状态消息**：Status*, UnlockPage*, FilterPage*, MainPage*
- **项目字段**：Item*, Card*, Identity*
- **页面标签**：所有页面标题和提示

#### 🔧 技术实现
- **ResourceHelper.cs**：全新实现的核心辅助类
  - `GetString()`：带参数格式化的资源获取
  - 快速访问方法：`AppDisplayName`, `CommandCopyPassword` 等
  - 缓存机制：单例 ResourceLoader
- **所有 UI 文本**：重构使用资源文件

#### 📚 文档
- **LOCALIZATION_SUMMARY.md**：详细的技术说明文档
- **docs/Localization-Guide.md**：完整的本地化指南
  - 添加新语言步骤
  - 资源文件结构说明
  - ResourceHelper 使用指南

#### 🔨 代码变更
更新了以下文件以支持本地化：
- `BitwardenForCommandPaletteCommandsProvider.cs`
- `Commands/ItemCommands.cs`（83 处）
- `Pages/BitwardenForCommandPalettePage.cs`（69 处）
- `Pages/FilterPage.cs`（61 处）
- `Pages/UnlockPage.cs`（21 处）

---

## [v0.6.0] - 2025-12-13

### ✨ 新增功能 - 筛选系统

#### 🔍 多维度筛选
- **收藏筛选**：仅显示收藏项目
- **类型筛选**：
  - 仅登录项目
  - 仅银行卡
  - 仅身份信息
  - 仅安全笔记
- **文件夹筛选**：
  - 按文件夹筛选（动态加载）
  - "无文件夹"选项

#### 🎛️ 筛选界面 (`FilterPage.cs`)
- 独立的筛选页面
- 动态列表显示所有选项
- 视觉标记（✅ 标签）当前激活的筛选

#### ✨ 命令扩展 - 银行卡支持

| 命令 | 功能 |
|------|------|
| `CopyCardNumberCommand` | 复制卡号（最后4位） |
| `CopyCardCvvCommand` | 复制 CVV |
| `CopyCardExpirationCommand` | 复制有效期（MM/YYYY） |
| `CopyCardholderNameCommand` | 复制持卡人姓名 |

#### ✨ 命令扩展 - 身份信息支持

| 命令 | 功能 |
|------|------|
| `CopyFullNameCommand` | 复制全名（Title + First + Middle + Last）|
| `CopyEmailCommand` | 复制邮箱 |
| `CopyPhoneCommand` | 复制电话 |
| `CopyAddressCommand` | 复制完整地址 |
| `CopyCompanyCommand` | 复制公司 |
| `CopySsnCommand` | 复制 SSN |
| `CopyPassportCommand` | 复制护照号 |
| `CopyLicenseCommand` | 复制驾照号 |

#### ✨ 命令扩展 - 安全笔记
- `CopyNotesCommand`：复制笔记内容

#### 🔧 主页面 (`BitwardenForCommandPalettePage.cs`) 大幅重构
- 新增 `_currentFilter` 状态管理
- 新增 `FilterItems()` 方法：支持多重筛选
- 新增 `GetFilterDescription()`：显示当前筛选状态
- 新增 `CreateFilterItem()`：创建筛选按钮
- 新增多种上下文命令添加方法：
  - `AddLoginCommands()`
  - `AddCardCommands()`
  - `AddIdentityCommands()`
  - `AddSecureNoteCommands()`
- 子标题优化：根据项目类型显示不同信息

#### 📦 新增数据模型属性
在 `BitwardenItem.cs` 中添加：
- `Fields`：自定义字段数组
- `CollectionIds`：集合 ID
- 日期属性：`RevisionDate`, `CreationDate`, `DeletedDate`

#### 🔧 新增服务方法
在 `BitwardenCliService.cs` 中：
- `GetItemsByFolderAsync()`：按文件夹获取项目
- `GetFoldersAsync()`：获取所有文件夹

### 🔨 代码统计
- **新增**：1104 行
- **修改**：45 行
- **总计**：6 个文件修改

---

## [v0.5.0] - 2025-12-13

### ✨ 新增功能 - 图标系统

#### 🎨 网站图标支持
- **IconService.cs**：全新服务类，166 行代码
- **图标来源**：Bitwarden 官方图标服务（`https://icons.bitwarden.net`）
- **显示位置**：每个登录项目左侧

#### 📊 图标逻辑
- 登录项目 → 尝试获取网站图标
- 其他类型 → 使用默认图标（Fluent UI）
  - 银行卡：信用卡图标
  - 身份：联系人图标
  - 安全笔记：笔记图标

#### 🔧 主页面集成
- `CreateListItem()`：添加图标支持
- 图标获取：`IconService.GetItemIcon(item)`

---

## [v0.4.0] - 2025-12-13

### 🔧 代码重构与优化

#### 模型重构
- **BitwardenItem.cs**：优化属性结构
  - 添加 `ItemType` 属性
  - 添加 `Subtitle` 计算属性
  - 优化 JSON 序列化

#### 搜索功能优化
- **BitwardenForCommandPalettePage.cs**：
  - 改进 `UpdateSearchText()` 实现
  - 优化 `FilterItems()` 搜索逻辑

#### 解锁逻辑改进
- **UnlockPage.cs**：重构表单处理
  - 改进错误处理
  - 优化成功回调机制

#### CLI 服务优化
- **BitwardenCliService.cs**：
  - 改进 `UnlockAsync()`：支持多种输出格式
  - 优化会话密钥提取逻辑

#### 📚 文档完善
- **README.md**：
  - 新增功能特性章节
  - 添加前置要求说明
  - 详细安装步骤
  - 完整使用指南
  - 项目结构说明
  - 技术栈说明
  - 多语言支持说明
  - 性能优化说明
  - 开发说明

---

## [v0.3.0] - 2025-12-13

### ✨ 新增功能 - 命令实现

#### Bitwarden CLI 服务 (`BitwardenCliService.cs`)
全新实现，254 行代码，包含：

**认证与状态**
- `GetStatusAsync()`：获取密码库状态
- `UnlockAsync()`：解锁密码库
- `LockAsync()`：锁定密码库
- `SyncAsync()`：同步密码库

**数据访问**
- `GetItemsAsync()`：获取所有项目
- `SearchItemsAsync()`：搜索项目
- `GetTotpAsync()`：生成 TOTP 验证码

**技术特性**
- 单例模式
- 异步执行
- AOT 兼容 JSON 序列化
- 错误处理

#### 命令实现 (`ItemCommands.cs`)
145 行代码，包含：

**登录项目命令**
- `CopyPasswordCommand`：复制密码
- `CopyUsernameCommand`：复制用户名
- `CopyUrlCommand`：复制 URL
- `OpenUrlCommand`：在浏览器打开 URL
- `CopyTotpCommand`：复制 TOTP 验证码

#### 页面实现

**主页面 (`BitwardenForCommandPalettePage.cs`)**
313 行代码：
- 状态检查和流程控制
- 项目列表渲染
- 搜索功能
- 加载/错误/空状态处理
- 上下文命令集成

**解锁页面 (`UnlockPage.cs`)**
123 行代码：
- Adaptive Cards 表单 UI
- 主密码输入
- 解锁逻辑
- 错误反馈

#### 数据模型

**BitwardenItem.cs**（251 行）
- 完整的 Bitwarden 数据模型
- 登录、银行卡、身份、安全笔记
- 自定义字段支持
- 完整的类型定义

**BitwardenStatus.cs**（47 行）
- 状态模型
- 属性：`IsUnlocked`, `IsLocked`, `IsLoggedOut`

---

## [v0.2.0] - 2025-12-13

### 💼 许可证
- **新增**：MIT License
- 允许自由使用、修改和分发

---

## [v0.1.0] - 2025-12-13 🎉

### 🚀 初始发布

#### 项目基础结构
- **解决方案文件**：BitwardenForCommandPalette.sln / .slnx
- **项目配置**：.NET 9.0, Windows 10.0.26100.0
- **MSIX 打包**：Package.appxmanifest

#### 核心组件
- **扩展入口**：
  - `BitwardenForCommandPalette.cs`：IExtension 实现
  - `BitwardenForCommandPaletteCommandsProvider.cs`：命令提供者
  - `Program.cs`：COM 服务器启动

- **主页面**：
  - `BitwardenForCommandPalettePage.cs`：DynamicListPage 实现

#### 资源文件
- 应用图标：Sqare44x44Logo, StoreLogo 等
- 启动画面：SplashScreen
- 应用图标：app.ico

#### 配置文件
- **构建配置**：
  - `Directory.Build.props`
  - `Directory.Packages.props`
  - `nuget.config`
- **发布配置**：
  - win-x64 发布配置
  - win-arm64 发布配置
- **项目配置**：
  - LaunchSettings.json
  - app.manifest

#### .gitignore
- 完整的 Visual Studio 忽略规则
- .NET 项目构建输出
- 用户配置文件

---

## 版本说明

### 版本号约定
- **v1.0.0+**：正式发布版本，生产环境可用
- **v0.x.0**：预发布版本，包含新功能但可能不稳定
- **v0.9.x**：功能冻结，准备发布

### 发布周期
- 快速迭代：每个提交可能带来重大功能
- 文档同步：每次功能更新都更新相应文档
- 质量保障：所有功能都经过测试

---

## 升级指南

### 从 v0.5.x 升级到 v1.0.0
1. **配置设置**：建议配置 Bitwarden API Key 以获得更好的体验
2. **图标缓存**：系统会自动缓存，无需手动操作
3. **中文支持**：根据系统语言自动切换

### 从 v0.7.x 升级到 v0.8.x
1. **无破坏性变更**：无缝升级
2. **性能提升**：自动享受缓存优化
3. **错误减少**：不可用域名自动处理

### 从 v0.6.x 升级到 v0.7.x
1. **语言设置**：检查系统区域设置以匹配翻译
2. **重新部署**：需要重新部署以加载新资源文件

---

## 已知问题与限制

### 当前版本 (v1.0.0) 已知限制
1. **不支持的 URI 格式**：
   - `android://` 开头的 URI 会被跳过
   - `ios://` 开头的 URI 会被跳过

2. **图标有限性**：
   - 依赖 Bitwarden 官方图标服务
   - 部分域名可能没有图标

3. **会话管理**：
   - 会话密钥仅存储在内存中
   - 重启应用后需要重新解锁

### 计划未来的改进
- [ ] 自动锁定功能
- [ ] 密码生成器
- [ ] 项目详情查看
- [ ] 键盘快捷键
- [ ] 深色/浅色主题图标
- [ ] 更多筛选选项

---

## 作者信息

**主要开发者**：kusutoriクスとり
**GitHub**：https://github.com/kusutori

---

## 致谢

- **Microsoft PowerToys**：提供 Command Palette 扩展框架
- **Bitwarden**：优秀的开源密码管理器
- **社区贡献**：所有提出建议和反馈的用户

---

**文档版本**：v1.0.0
**最后更新**：2025-12-17
**生成方式**：基于 Git 提交历史自动分析
