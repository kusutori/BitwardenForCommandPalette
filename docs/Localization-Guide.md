# 多语言支持 (Localization)

本项目已实现完整的多语言支持，使用 Windows 资源文件 (.resw) 来存储本地化字符串。

## 文件结构

```
BitwardenForCommandPalette/
├── Strings/
│   ├── en-US/
│   │   └── Resources.resw    # 英文资源
│   └── zh-CN/
│       └── Resources.resw    # 中文资源
└── Helpers/
    └── ResourceHelper.cs      # 资源访问辅助类
```

## 支持的语言

目前支持的语言：
- **en-US**: 英语（美国）- 默认语言
- **zh-CN**: 简体中文

## 添加新语言

### 1. 创建语言文件夹

在 `Strings/` 目录下创建新的语言代码文件夹，例如：
- `ja-JP` - 日语
- `fr-FR` - 法语
- `de-DE` - 德语
- `es-ES` - 西班牙语

### 2. 复制资源文件

将 `Strings/en-US/Resources.resw` 复制到新的语言文件夹中：

```powershell
Copy-Item "Strings/en-US/Resources.resw" "Strings/ja-JP/Resources.resw"
```

### 3. 翻译资源字符串

打开新的 `Resources.resw` 文件，翻译 `<value>` 标签中的内容：

**英文版本**:
```xml
<data name="CommandCopyPassword" xml:space="preserve">
  <value>Copy Password</value>
</data>
```

**翻译后** (日语示例):
```xml
<data name="CommandCopyPassword" xml:space="preserve">
  <value>パスワードをコピー</value>
</data>
```

**重要**: 
- 不要修改 `name` 属性
- 不要修改包含 `{0}`, `{1}` 等占位符的位置
- 保持 XML 结构不变

### 4. 测试新语言

1. 在 Windows 设置中将系统语言更改为目标语言
2. 重新部署扩展
3. 启动 Command Palette，检查翻译是否正确显示

## 资源文件结构

### 资源键类别

| 类别 | 前缀 | 说明 | 示例 |
|------|------|------|------|
| 应用程序 | `App` | 应用名称等 | `AppDisplayName` |
| 操作 | `Action` | 通用操作 | `ActionOpen`, `ActionUnlock` |
| 命令 | `Command` | 用户命令 | `CommandCopyPassword` |
| Toast | `Toast` | 提示消息 | `ToastPasswordCopied` |
| 页面 | `*Page*` | 页面相关 | `UnlockPageTitle` |
| 筛选 | `Filter` | 筛选选项 | `FilterAllItems` |
| 状态 | `Status` | 状态消息 | `StatusLoading` |
| 主页面 | `Main` | 主界面 | `MainPageTitle` |
| 项目 | `Item` | 项目显示 | `ItemSubtitleSecureNote` |

### 格式化字符串

某些资源字符串包含格式占位符：

```xml
<!-- 单个占位符 -->
<data name="ToastFieldCopied" xml:space="preserve">
  <value>{0} copied</value>
</data>

<!-- 使用示例 -->
ResourceHelper.ToastFieldCopied("Email")  // 输出: "Email copied"
```

## ResourceHelper 使用

### 简单字符串

```csharp
using BitwardenForCommandPalette.Helpers;

// 获取简单字符串
var title = ResourceHelper.AppDisplayName;
var buttonText = ResourceHelper.CommandCopyPassword;
```

### 格式化字符串

```csharp
// 带参数的字符串
var message = ResourceHelper.ToastFieldCopied("Email");
var filterDesc = ResourceHelper.FilterDescFolder("Work");
```

### 动态访问

```csharp
// 直接通过键访问
var text = ResourceHelper.GetString("CommandCopyPassword");

// 带格式化参数
var formatted = ResourceHelper.GetString("ToastFieldCopied", fieldName);
```

## 在代码中使用资源

### 在命令中

```csharp
public MyCommand()
{
    Name = ResourceHelper.CommandMyAction;
    Icon = new IconInfo("\uE8C8");
}

public override CommandResult Invoke()
{
    // ... 操作逻辑
    return CommandResult.ShowToast(ResourceHelper.ToastActionCompleted);
}
```

### 在页面中

```csharp
public MyPage()
{
    Title = ResourceHelper.MyPageTitle;
    Name = ResourceHelper.ActionOpen;
    PlaceholderText = ResourceHelper.MyPagePlaceholder;
}
```

### 在 Adaptive Card 中

使用字符串插值将资源嵌入 JSON：

```csharp
TemplateJson = $$"""
{
    "type": "TextBlock",
    "text": "{{ResourceHelper.UnlockCardTitle}}"
}
""";
```

## 最佳实践

### 1. 保持键名一致

- 使用清晰的命名约定
- 为相关的字符串使用相同的前缀
- 保持命名的语义化

### 2. 不要硬编码字符串

❌ **错误**:
```csharp
Title = "Copy Password";
```

✅ **正确**:
```csharp
Title = ResourceHelper.CommandCopyPassword;
```

### 3. 注意字符串长度

不同语言的文本长度可能差异很大：
- 中文通常比英文短
- 德语通常比英文长
- 设计 UI 时预留足够空间

### 4. 占位符顺序

某些语言可能需要调整占位符顺序：

```xml
<!-- 英文 -->
<value>Copy {0} to clipboard</value>

<!-- 日语（可能需要不同顺序） -->
<value>{0}をクリップボードにコピー</value>
```

### 5. 文化敏感内容

- 避免使用特定文化的习语或俚语
- 注意日期、时间、数字格式
- 考虑不同的文化习惯

## 测试检查清单

在添加新语言后，测试以下内容：

- [ ] 所有菜单项和按钮标签正确显示
- [ ] Toast 消息正确显示
- [ ] 页面标题和占位符文本正确
- [ ] 筛选选项标签正确
- [ ] 状态消息正确
- [ ] 错误消息清晰易懂
- [ ] Adaptive Card 表单正确显示
- [ ] 长文本不会导致 UI 布局问题
- [ ] 格式化字符串的参数正确替换

## 故障排除

### 资源未加载

1. 确认 .resw 文件位于正确的文件夹
2. 检查文件编码是否为 UTF-8
3. 确保 .csproj 中包含 `<PRIResource Include="Strings\**\*.resw" />`
4. 清理并重新构建项目

### 显示默认语言而非目标语言

1. 检查文件夹名称是否与 Windows 语言代码完全匹配
2. 确认所有键都已正确翻译
3. 验证 XML 格式没有错误

### 格式化字符串问题

1. 确保占位符 `{0}`, `{1}` 等在所有语言中存在
2. 检查 `ResourceHelper.GetString()` 调用中的参数数量
3. 使用 `CultureInfo.CurrentCulture` 进行格式化

## 贡献翻译

如果您想为项目贡献新的语言翻译：

1. Fork 项目
2. 创建新的语言文件夹和 Resources.resw
3. 完成翻译
4. 测试翻译质量
5. 提交 Pull Request

感谢您为项目的多语言支持做出贡献！
