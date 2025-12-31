# 开发问题与解决方案

本文档记录了在 Bitwarden For Command Palette 开发过程中遇到的问题及其解决方案，供开发者参考。

---

## 目录

- [AOT 编译相关问题](#aot-编译相关问题)
- [Adaptive Cards 相关问题](#adaptive-cards-相关问题)
- [Command Palette SDK 相关问题](#command-palette-sdk-相关问题)
- [Bitwarden CLI 相关问题](#bitwarden-cli-相关问题)
- [UI/UX 相关问题](#uiux-相关问题)

---

## AOT 编译相关问题

### 问题 1：JsonSerializer.Serialize 导致 AOT 警告

**错误信息**：
```
warning IL2026: Using member 'System.Text.Json.JsonSerializer.Serialize<T>(T)' which has 'RequiresUnreferencedCodeAttribute'
warning IL3050: Using member 'System.Text.Json.JsonSerializer.Serialize<T>(T)' which has 'RequiresDynamicCodeAttribute'
```

**原因**：
.NET AOT 编译要求在编译时知道所有类型信息。使用泛型 `JsonSerializer.Serialize<T>()` 或匿名类型会导致运行时反射，与 AOT 不兼容。

**错误代码**：
```csharp
// ❌ 使用匿名类型
var data = new { title = "Hello", name = "World" };
var json = JsonSerializer.Serialize(data);

// ❌ 使用泛型方法
var json = JsonSerializer.Serialize<MyClass>(obj);
```

**解决方案**：
```csharp
// ✅ 使用 JsonObject 构建 JSON
var data = new JsonObject
{
    ["title"] = "Hello",
    ["name"] = "World"
};
var json = data.ToJsonString();

// ✅ 使用 Source Generator（需要预定义上下文）
[JsonSerializable(typeof(MyClass))]
internal partial class MyJsonContext : JsonSerializerContext { }

var json = JsonSerializer.Serialize(obj, MyJsonContext.Default.MyClass);
```

**相关文件**：
- `Services/BitwardenCliService.cs` - 定义了 `BitwardenJsonContext`
- `Pages/CreateItemPage.cs` - 使用 `JsonObject.ToJsonString()`
- `Pages/GeneratorPage.cs` - 使用 `JsonObject.ToJsonString()`

---

### 问题 2：JsonArray.Add<T> 导致 AOT 警告

**错误信息**：
```
warning IL2026: Using member 'System.Text.Json.Nodes.JsonArray.Add<T>(T)' which has 'RequiresUnreferencedCodeAttribute'
```

**原因**：
`JsonArray.Add<T>()` 内部使用了泛型序列化，需要运行时类型信息。

**错误代码**：
```csharp
// ❌ 直接添加 JsonObject
var choices = new JsonArray();
choices.Add(new JsonObject
{
    ["title"] = "Option 1",
    ["value"] = "1"
});
```

**解决方案**：
```csharp
// ✅ 使用 JsonNode.Parse() 添加
var choices = new JsonArray();
choices.Add(JsonNode.Parse("{\"title\":\"Option 1\",\"value\":\"1\"}"));

// ✅ 或者转义后构建 JSON 字符串
var title = "Option 1".Replace("\"", "\\\"");
choices.Add(JsonNode.Parse($"{{\"title\":\"{title}\",\"value\":\"1\"}}"));
```

**相关文件**：
- `Pages/CreateItemPage.cs` - `GetFolderChoiceSetJson()` 方法

---

### 问题 3：Source Generator 上下文定义

**说明**：
为了支持 AOT 编译，需要为所有需要 JSON 序列化/反序列化的类型定义 Source Generator 上下文。

**实现**：
```csharp
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(BitwardenStatus))]
[JsonSerializable(typeof(BitwardenItem[]))]
[JsonSerializable(typeof(BitwardenFolder[]))]
internal partial class BitwardenJsonContext : JsonSerializerContext
{
}
```

**使用**：
```csharp
// 反序列化
var status = JsonSerializer.Deserialize(json, BitwardenJsonContext.Default.BitwardenStatus);

// 序列化
var json = JsonSerializer.Serialize(item, BitwardenJsonContext.Default.BitwardenItem);
```

---

## Adaptive Cards 相关问题

### 问题 4：Input.ChoiceSet 的 choices 不支持数据绑定

**问题描述**：
Adaptive Cards 的 `Input.ChoiceSet` 组件的 `choices` 数组不支持通过 `${variable}` 语法进行数据绑定，必须在模板中硬编码。

**错误尝试**：
```json
{
    "type": "Input.ChoiceSet",
    "id": "folder",
    "choices": "${folders}"  // ❌ 不生效
}
```

**解决方案**：
动态生成整个模板字符串，将 choices 直接嵌入到模板中。

```csharp
private static string GetFolderChoiceSetJson(BitwardenFolder[]? folders)
{
    var choices = new JsonArray();
    
    // 动态添加选项
    choices.Add(JsonNode.Parse("{\"title\":\"No Folder\",\"value\":\"\"}"));
    
    if (folders != null)
    {
        foreach (var folder in folders)
        {
            var escapedName = folder.Name.Replace("\\", "\\\\").Replace("\"", "\\\"");
            choices.Add(JsonNode.Parse($"{{\"title\":\"{escapedName}\",\"value\":\"{folder.Id}\"}}"));
        }
    }

    var choiceSet = new JsonObject
    {
        ["type"] = "Input.ChoiceSet",
        ["id"] = "folderId",
        ["label"] = "${folderLabel}",  // 标签仍可数据绑定
        ["choices"] = choices
    };

    return choiceSet.ToJsonString();
}

// 在模板中使用字符串插值
private static string GetLoginTemplate(BitwardenFolder[]? folders)
{
    var folderChoiceSet = GetFolderChoiceSetJson(folders);
    return $$"""
    {
        "type": "AdaptiveCard",
        "body": [
            // ... 其他字段
            {{folderChoiceSet}},
            // ... 其他字段
        ]
    }
    """;
}
```

**相关文件**：
- `Pages/CreateItemPage.cs`
- `Pages/EditItemPage.cs`

---

### 问题 5：Input.Toggle 返回字符串而非布尔值

**问题描述**：
Adaptive Cards 的 `Input.Toggle` 组件在表单提交时返回的是字符串 `"true"` 或 `"false"`，而不是布尔值。

**错误代码**：
```csharp
// ❌ 直接获取布尔值会失败
var includeUppercase = formData["uppercase"]?.GetValue<bool>() ?? true;
```

**解决方案**：
```csharp
// ✅ 先获取字符串，再解析为布尔值
var includeUppercase = formData["uppercase"]?.GetValue<string>() == "true";

// ✅ 或者使用更健壮的解析
var uppercaseStr = formData["uppercase"]?.GetValue<string>() ?? "true";
var includeUppercase = uppercaseStr.Equals("true", StringComparison.OrdinalIgnoreCase);
```

**相关文件**：
- `Pages/GeneratorPage.cs` - `PasswordGeneratorForm.SubmitForm()`

---

### 问题 6：FormContent 的 TemplateJson 和 DataJson 分离

**说明**：
Command Palette 的 `FormContent` 使用 Adaptive Cards 模板系统，需要分离模板和数据。

**模板（TemplateJson）**：
```json
{
    "type": "AdaptiveCard",
    "body": [
        {
            "type": "TextBlock",
            "text": "${title}"
        },
        {
            "type": "Input.Text",
            "id": "name",
            "label": "${nameLabel}",
            "value": "${name}"
        }
    ]
}
```

**数据（DataJson）**：
```json
{
    "title": "Create Login",
    "nameLabel": "Name",
    "name": ""
}
```

**注意事项**：
- 模板中使用 `${variable}` 语法引用数据
- 表单字段的 `id` 用于提交时获取值
- 数据绑定只适用于简单值，不适用于数组或复杂对象

---

## Command Palette SDK 相关问题

### 问题 7：ListItem 的 MoreCommands 与默认命令冲突

**问题描述**：
当 `ListItem` 同时设置了 `Command`（默认命令）和 `MoreCommands`（更多命令）时，如果 `MoreCommands` 中的命令也设置了 `InvokeCommand.RequestedShortcuts`，可能会与默认命令冲突。

**场景**：
回收站中的项目，默认 Enter 键应该恢复项目，Ctrl+Enter 应该永久删除。

**错误实现**：
```csharp
// ❌ 两个命令都响应 Enter 键
new ListItem(new RestoreCommand(item))
{
    MoreCommands = [new PermanentDeleteCommand(item)]
}

// RestoreCommand 没有设置 RequestedShortcuts
// PermanentDeleteCommand 设置了 Ctrl+Enter，但也会响应 Enter
```

**正确实现**：
```csharp
// ✅ 明确设置默认命令的快捷键
public class RestoreCommand : InvokableCommand
{
    public RestoreCommand()
    {
        // 只响应 Enter 键
        RequestedShortcuts = [KeyboardShortcut.Enter];
    }
}

public class PermanentDeleteCommand : InvokableCommand
{
    public PermanentDeleteCommand()
    {
        // 只响应 Ctrl+Enter
        RequestedShortcuts = [KeyboardShortcut.CtrlEnter];
    }
}
```

**相关文件**：
- `Pages/BitwardenForCommandPalettePage.cs` - 回收站项目处理

---

### 问题 8：DynamicListPage 的 IsLoading 状态管理

**问题描述**：
`DynamicListPage` 的 `IsLoading` 属性需要手动管理，否则用户看不到加载指示器。

**最佳实践**：
```csharp
public override IListItem[] GetItems()
{
    IsLoading = true;
    RaiseItemsChanged();  // 通知 UI 更新加载状态
    
    try
    {
        // 执行异步操作
        var items = LoadItemsAsync().Result;
        return items;
    }
    finally
    {
        IsLoading = false;
        // 不需要再次 RaiseItemsChanged，返回的数组会触发更新
    }
}
```

---

### 问题 9：ContentPage 与 FormContent 的关系

**说明**：
`ContentPage` 是一个页面容器，`FormContent` 是其中的内容。一个 `ContentPage` 可以包含多个 `IContent`。

**结构**：
```csharp
internal sealed class MyPage : ContentPage
{
    private readonly MyForm _form;
    
    public MyPage()
    {
        _form = new MyForm();
        Name = "Page Name";
        Title = "Page Title";
    }
    
    public override IContent[] GetContent() => [_form];
}

internal sealed class MyForm : FormContent
{
    public MyForm()
    {
        TemplateJson = "...";
        DataJson = "...";
    }
    
    public override ICommandResult SubmitForm(string payload, string data)
    {
        // payload: 表单字段值（JSON）
        // data: Action.Submit 的 data 属性（JSON）
        return CommandResult.ShowToast("Success");
    }
}
```

---

## Bitwarden CLI 相关问题

### 问题 10：CLI 输出包含非 JSON 内容

**问题描述**：
某些情况下，Bitwarden CLI 的输出可能包含额外的文本（如警告信息），导致 JSON 解析失败。

**解决方案**：
```csharp
private static async Task<T?> ParseCliOutputAsync<T>(string output)
{
    // 尝试找到 JSON 的起始位置
    var jsonStart = output.IndexOf('{');
    if (jsonStart == -1)
    {
        jsonStart = output.IndexOf('[');
    }
    
    if (jsonStart == -1)
    {
        return default;
    }
    
    var jsonContent = output[jsonStart..];
    return JsonSerializer.Deserialize<T>(jsonContent, options);
}
```

---

### 问题 11：bw create 命令需要 Base64 编码

**问题描述**：
`bw create item` 和 `bw create folder` 命令需要将 JSON 数据进行 Base64 编码后传入。

**实现**：
```csharp
public static async Task<bool> CreateItemAsync(JsonObject newItem)
{
    var itemJson = newItem.ToJsonString();
    var encodedJson = Convert.ToBase64String(Encoding.UTF8.GetBytes(itemJson));
    
    var (output, error, exitCode) = await ExecuteCommandAsync(
        $"create item \"{encodedJson}\" --session \"{SessionKey}\"");
    
    return exitCode == 0;
}
```

**相关文件**：
- `Services/BitwardenCliService.cs`

---

### 问题 12：Session Key 管理

**问题描述**：
Bitwarden CLI 解锁后返回的 Session Key 需要在后续所有命令中使用。

**管理策略**：
```csharp
public class BitwardenCliService
{
    // 单例模式确保 Session Key 全局共享
    private static BitwardenCliService? _instance;
    public static BitwardenCliService Instance => _instance ??= new BitwardenCliService();
    
    // Session Key 存储
    public string? SessionKey { get; private set; }
    
    public async Task<bool> UnlockAsync(string password)
    {
        var (output, _, exitCode) = await ExecuteCommandAsync($"unlock \"{password}\" --raw");
        
        if (exitCode == 0 && !string.IsNullOrEmpty(output))
        {
            SessionKey = output.Trim();
            return true;
        }
        return false;
    }
    
    // 所有后续命令都使用 SessionKey
    private async Task<string> ExecuteWithSessionAsync(string command)
    {
        return await ExecuteCommandAsync($"{command} --session \"{SessionKey}\"");
    }
}
```

---

## UI/UX 相关问题

### 问题 13：IsCritical 标记危险操作

**问题描述**：
需要将删除等危险操作以红色显示，提醒用户注意。

**解决方案**：
```csharp
public class DeleteCommand : InvokableCommand
{
    public DeleteCommand()
    {
        Name = "Delete";
        // 设置 IsCritical 使命令以红色显示
        IsCritical = true;
    }
}
```

**相关文件**：
- `Commands/ItemCommands.cs` - `DeleteItemCommand`, `PermanentDeleteCommand`

---

### 问题 14：Tags 显示状态标记

**问题描述**：
需要在列表项上显示状态标记（如 ✅ 表示选中，⭐ 表示收藏）。

**实现**：
```csharp
var listItem = new ListItem(command)
{
    Title = item.Name,
    Tags = item.Favorite 
        ? [new Tag { Text = "⭐" }] 
        : []
};

// 筛选页面中显示当前激活的筛选
var filterItem = new ListItem(command)
{
    Title = "Favorites Only",
    Tags = isActive 
        ? [new Tag { Text = "✅" }] 
        : []
};
```

---

### 问题 15：详情面板 Markdown 渲染

**问题描述**：
`Details` 属性支持 Markdown 格式，可以创建富文本详情面板。

**实现**：
```csharp
private string BuildDetailsMarkdown(BitwardenItem item)
{
    var sb = new StringBuilder();
    
    // 标题
    sb.AppendLine($"## {item.Name}");
    sb.AppendLine();
    
    // 列表
    sb.AppendLine("| Field | Value |");
    sb.AppendLine("|-------|-------|");
    sb.AppendLine($"| Username | {item.Login?.Username ?? "—"} |");
    sb.AppendLine($"| Password | ••••••••• |");
    
    // 代码块（用于显示笔记）
    if (!string.IsNullOrEmpty(item.Notes))
    {
        sb.AppendLine();
        sb.AppendLine("### Notes");
        sb.AppendLine("```");
        sb.AppendLine(item.Notes);
        sb.AppendLine("```");
    }
    
    return sb.ToString();
}
```

---

## 性能优化问题

### 问题 16：图标缓存策略

**问题描述**：
频繁请求网站图标会影响性能，需要缓存机制。

**实现**：
```csharp
public static class IconService
{
    private static readonly Dictionary<string, string> _iconUrlCache = new();
    private const int MaxCacheSize = 200;
    
    public static string GetIconUrl(string? url)
    {
        var domain = ExtractDomain(url);
        
        if (_iconUrlCache.TryGetValue(domain, out var cachedUrl))
        {
            return cachedUrl;
        }
        
        // 缓存淘汰
        if (_iconUrlCache.Count >= MaxCacheSize)
        {
            // 移除一半缓存
            var keysToRemove = _iconUrlCache.Keys.Take(MaxCacheSize / 2).ToList();
            foreach (var key in keysToRemove)
            {
                _iconUrlCache.Remove(key);
            }
        }
        
        var iconUrl = $"https://icons.bitwarden.net/{domain}/icon.png";
        _iconUrlCache[domain] = iconUrl;
        
        return iconUrl;
    }
}
```

**相关文件**：
- `Services/IconService.cs`

---

## 总结

开发 Command Palette 扩展时，主要需要注意以下几点：

1. **AOT 兼容性**：避免使用运行时反射，使用 Source Generator 和 `JsonObject`
2. **Adaptive Cards 限制**：了解数据绑定的边界，动态生成模板时注意字符转义
3. **SDK 特性**：熟悉 `ListItem`、`ContentPage`、`FormContent` 等组件的正确用法
4. **CLI 集成**：处理 CLI 输出的边界情况，正确管理 Session Key
5. **用户体验**：使用 `IsCritical`、`Tags`、`Details` 等属性增强界面

遇到问题时，建议：
1. 查看编译警告，特别是 IL2026/IL3050 相关的 AOT 警告
2. 使用 Debug.WriteLine 输出调试信息
3. 参考官方 Adaptive Cards Designer 测试模板
4. 查看 Bitwarden CLI 的帮助文档 (`bw --help`)
