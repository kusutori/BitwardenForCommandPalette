# Adaptive Cards 开发指南

> 基于 Command Palette 扩展开发的 Adaptive Cards 使用指南

## 目录

- [概述](#概述)
- [基础结构](#基础结构)
- [布局元素](#布局元素)
- [输入控件](#输入控件)
- [动作按钮](#动作按钮)
- [样式与格式化](#样式与格式化)
- [表单验证](#表单验证)
- [在 Command Palette 中的使用](#在-command-palette-中的使用)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

---

## 概述

### 什么是 Adaptive Cards？

Adaptive Cards 是微软推出的一种开放的卡片交换格式，允许开发者用统一的 JSON 格式描述 UI，然后由不同的宿主应用渲染成原生 UI。

### Command Palette 支持的版本

- **Schema 版本**: 1.6
- **渲染器**: WinUI3 AdaptiveCards Renderer
- **文档**: [adaptivecards.io](https://adaptivecards.io)

### 基本 JSON 结构

```json
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        // 卡片内容元素
    ],
    "actions": [
        // 操作按钮
    ]
}
```

---

## 布局元素

### Container - 容器

容器用于将多个元素组合在一起，可以设置背景样式。

```json
{
    "type": "Container",
    "style": "emphasis",      // default, emphasis, good, attention, warning, accent
    "bleed": true,            // 是否扩展到卡片边缘
    "items": [
        // 子元素
    ]
}
```

**style 可选值：**
| 值 | 说明 |
|---|------|
| `default` | 默认样式 |
| `emphasis` | 强调样式（灰色背景） |
| `good` | 成功样式（绿色调） |
| `attention` | 警告样式（红色调） |
| `warning` | 提示样式（黄色调） |
| `accent` | 强调样式（主题色） |

### ColumnSet - 列集

用于创建多列布局。

```json
{
    "type": "ColumnSet",
    "columns": [
        {
            "type": "Column",
            "width": "auto",       // auto, stretch, 或具体像素值 "50px"
            "verticalContentAlignment": "center",  // top, center, bottom
            "items": [
                // 列内容
            ]
        },
        {
            "type": "Column",
            "width": "stretch",
            "items": [
                // 另一列内容
            ]
        }
    ]
}
```

**width 可选值：**
| 值 | 说明 |
|---|------|
| `auto` | 根据内容自动调整 |
| `stretch` | 填充剩余空间 |
| `"50px"` | 固定像素宽度 |
| `1`, `2` | 权重比例 |

### FactSet - 事实集

用于显示键值对形式的信息。

```json
{
    "type": "FactSet",
    "facts": [
        {
            "title": "用户名：",
            "value": "john@example.com"
        },
        {
            "title": "创建日期：",
            "value": "2024-01-15"
        }
    ]
}
```

---

## 文本与图像

### TextBlock - 文本块

```json
{
    "type": "TextBlock",
    "text": "标题文本",
    "size": "large",           // small, default, medium, large, extraLarge
    "weight": "bolder",        // lighter, default, bolder
    "color": "accent",         // default, dark, light, accent, good, warning, attention
    "wrap": true,              // 是否自动换行
    "isSubtle": true,          // 是否使用柔和颜色
    "style": "heading",        // default, heading（无障碍标记）
    "horizontalAlignment": "center"  // left, center, right
}
```

**size 可选值：**
| 值 | 说明 |
|---|------|
| `small` | 12px |
| `default` | 14px |
| `medium` | 14px |
| `large` | 20px |
| `extraLarge` | 26px |

### Image - 图像

```json
{
    "type": "Image",
    "url": "https://example.com/image.png",
    "size": "small",           // auto, stretch, small, medium, large
    "style": "person",         // default, person（圆形裁剪）
    "altText": "图像描述",
    "width": "50px",           // 指定宽度
    "height": "50px"           // 指定高度
}
```

### RichTextBlock - 富文本

```json
{
    "type": "RichTextBlock",
    "inlines": [
        {
            "type": "TextRun",
            "text": "普通文本 "
        },
        {
            "type": "TextRun",
            "text": "粗体文本",
            "weight": "bolder"
        },
        {
            "type": "TextRun",
            "text": " 斜体文本",
            "italic": true
        }
    ]
}
```

---

## 输入控件

### Input.Text - 文本输入

```json
{
    "type": "Input.Text",
    "id": "inputId",           // 必须，用于获取输入值
    "label": "输入标签",        // 显示在输入框上方
    "placeholder": "请输入...",
    "style": "text",           // text, tel, url, email, password
    "isRequired": true,        // 是否必填
    "errorMessage": "此字段必填",
    "isMultiline": false,      // 是否多行
    "maxLength": 100,          // 最大字符数
    "regex": "^[a-zA-Z]+$",    // 正则验证（1.3+）
    "spacing": "medium"        // 间距
}
```

**style 可选值：**
| 值 | 说明 |
|---|------|
| `text` | 普通文本 |
| `password` | 密码（显示为圆点） |
| `tel` | 电话号码 |
| `url` | URL 地址 |
| `email` | 电子邮箱 |

### Input.Number - 数字输入

```json
{
    "type": "Input.Number",
    "id": "ageInput",
    "label": "年龄",
    "placeholder": "请输入年龄",
    "min": 0,
    "max": 150,
    "value": 25
}
```

### Input.Date - 日期选择

```json
{
    "type": "Input.Date",
    "id": "birthDate",
    "label": "出生日期",
    "min": "1900-01-01",
    "max": "2024-12-31",
    "value": "2000-01-01"
}
```

### Input.Time - 时间选择

```json
{
    "type": "Input.Time",
    "id": "meetingTime",
    "label": "会议时间",
    "min": "09:00",
    "max": "18:00",
    "value": "14:00"
}
```

### Input.Toggle - 开关

```json
{
    "type": "Input.Toggle",
    "id": "acceptTerms",
    "title": "我同意服务条款",
    "label": "请确认：",
    "valueOn": "true",
    "valueOff": "false",
    "value": "false",
    "isRequired": true
}
```

### Input.ChoiceSet - 选择集

```json
{
    "type": "Input.ChoiceSet",
    "id": "colorChoice",
    "label": "选择颜色",
    "style": "compact",        // compact（下拉）, expanded（展开）, filtered（可搜索）
    "isMultiSelect": false,
    "value": "1",              // 默认选中值
    "choices": [
        {
            "title": "红色",
            "value": "1"
        },
        {
            "title": "绿色", 
            "value": "2"
        },
        {
            "title": "蓝色",
            "value": "3"
        }
    ]
}
```

**style 可选值：**
| 值 | 说明 |
|---|------|
| `compact` | 下拉列表 |
| `expanded` | 展开的单选/复选框 |
| `filtered` | 可搜索的下拉列表（1.5+） |

---

## 动作按钮

### Action.Submit - 提交

```json
{
    "type": "Action.Submit",
    "title": "提交",
    "style": "positive",       // default, positive, destructive
    "tooltip": "点击提交表单",
    "iconUrl": "https://example.com/icon.png",
    "data": {
        "action": "submit",
        "extraData": "value"
    }
}
```

**style 可选值：**
| 值 | 说明 |
|---|------|
| `default` | 默认样式 |
| `positive` | 积极/确认样式（通常为绿色） |
| `destructive` | 危险/删除样式（通常为红色） |

### Action.OpenUrl - 打开链接

```json
{
    "type": "Action.OpenUrl",
    "title": "打开网站",
    "url": "https://example.com"
}
```

### Action.ShowCard - 展开子卡片

```json
{
    "type": "Action.ShowCard",
    "title": "显示更多",
    "card": {
        "type": "AdaptiveCard",
        "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
        "version": "1.6",
        "body": [
            {
                "type": "Input.Text",
                "id": "comment",
                "label": "评论",
                "isMultiline": true
            }
        ],
        "actions": [
            {
                "type": "Action.Submit",
                "title": "提交评论"
            }
        ]
    }
}
```

### Action.ToggleVisibility - 切换可见性

```json
{
    "type": "Action.ToggleVisibility",
    "title": "显示/隐藏详情",
    "targetElements": [
        "detailsContainer"
    ]
}
```

配合使用的元素：

```json
{
    "type": "Container",
    "id": "detailsContainer",
    "isVisible": false,
    "items": [
        // 详情内容
    ]
}
```

---

## 样式与格式化

### Spacing - 间距

```json
{
    "type": "TextBlock",
    "text": "文本",
    "spacing": "medium"    // none, small, default, medium, large, extraLarge, padding
}
```

| 值 | 像素值 |
|---|------|
| `none` | 0 |
| `small` | 4px |
| `default` | 8px |
| `medium` | 20px |
| `large` | 30px |
| `extraLarge` | 40px |

### Separator - 分隔线

```json
{
    "type": "TextBlock",
    "text": "分隔线上方",
    "separator": true
}
```

### Height - 高度

```json
{
    "type": "Container",
    "height": "stretch"    // auto, stretch
}
```

---

## 表单验证

### 必填验证

```json
{
    "type": "Input.Text",
    "id": "name",
    "label": "姓名",
    "isRequired": true,
    "errorMessage": "姓名是必填项"
}
```

### 正则验证（1.3+）

```json
{
    "type": "Input.Text",
    "id": "email",
    "label": "邮箱",
    "regex": "^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]{2,4}$",
    "errorMessage": "请输入有效的邮箱地址"
}
```

### 关联输入验证（1.3+）

```json
{
    "type": "Action.Submit",
    "title": "提交",
    "associatedInputs": "auto"    // auto, none
}
```

---

## 在 Command Palette 中的使用

### FormContent 类

在 Command Palette 扩展中，使用 `FormContent` 类来创建表单：

```csharp
internal sealed partial class MyForm : FormContent
{
    public MyForm()
    {
        TemplateJson = """
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        {
            "type": "Input.Text",
            "id": "myInput",
            "label": "输入",
            "isRequired": true
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "提交",
            "style": "positive"
        }
    ]
}
""";
    }

    public override CommandResult SubmitForm(string payload)
    {
        var formInput = JsonNode.Parse(payload)?.AsObject();
        if (formInput == null)
        {
            return CommandResult.GoBack();
        }

        var myValue = formInput["myInput"]?.GetValue<string>();
        
        // 处理表单数据...
        
        return CommandResult.Dismiss();
    }
}
```

### 数据绑定（模板）

使用 `DataJson` 进行数据绑定：

```csharp
TemplateJson = """
{
    "type": "AdaptiveCard",
    "body": [
        {
            "type": "TextBlock",
            "text": "欢迎，${userName}！"
        }
    ]
}
""";

DataJson = """
{
    "userName": "张三"
}
""";
```

### 回车键提交

在 Command Palette 中，当表单只有一个输入框时，按回车键会自动触发 `Action.Submit`。这是由 AdaptiveCards WinUI3 渲染器提供的功能。

> **注意**：如果表单有多个输入框，回车键会在输入框之间移动焦点，需要点击按钮或 Tab 到按钮后按回车才能提交。

---

## 最佳实践

### 1. 布局设计

```json
// ✅ 推荐：使用 Container 和 ColumnSet 组织布局
{
    "type": "Container",
    "style": "emphasis",
    "items": [
        {
            "type": "ColumnSet",
            "columns": [
                {
                    "type": "Column",
                    "width": "auto",
                    "items": [
                        { "type": "Image", "url": "...", "size": "small" }
                    ]
                },
                {
                    "type": "Column", 
                    "width": "stretch",
                    "items": [
                        { "type": "TextBlock", "text": "标题", "weight": "bolder" }
                    ]
                }
            ]
        }
    ]
}
```

### 2. 表单验证

```json
// ✅ 推荐：始终为必填字段提供错误消息
{
    "type": "Input.Text",
    "id": "password",
    "label": "密码",
    "style": "password",
    "isRequired": true,
    "errorMessage": "密码是必填项"
}
```

### 3. 无障碍支持

```json
// ✅ 推荐：使用 label 而不是单独的 TextBlock
{
    "type": "Input.Text",
    "id": "name",
    "label": "姓名"    // 自带无障碍支持
}

// ❌ 不推荐：使用单独的 TextBlock
{
    "type": "TextBlock",
    "text": "姓名"
},
{
    "type": "Input.Text",
    "id": "name"
}
```

### 4. 按钮样式

```json
// ✅ 推荐：使用语义化的按钮样式
{
    "type": "Action.Submit",
    "title": "保存",
    "style": "positive"    // 确认操作用 positive
},
{
    "type": "Action.Submit",
    "title": "删除",
    "style": "destructive"  // 危险操作用 destructive
}
```

---

## 常见问题

### Q: 如何让表单按回车提交？

A: 在 Command Palette 中，单输入框表单会自动支持回车提交。对于多输入框表单，需要用户 Tab 到按钮后按回车，或直接点击按钮。

### Q: 如何动态更新卡片内容？

A: 修改 `TemplateJson` 或 `DataJson` 属性后，调用 `OnPropertyChanged` 通知更新：

```csharp
DataJson = newData;
OnPropertyChanged(nameof(DataJson));
```

### Q: 图片不显示怎么办？

A: 确保图片 URL 可访问，且 HTTPS 协议。也可以使用 data URI：

```json
{
    "type": "Image",
    "url": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUA..."
}
```

### Q: 如何获取 Action.Submit 的 data？

A: 在 `SubmitForm` 中使用第二个参数 `data`：

```csharp
public override ICommandResult SubmitForm(string inputs, string data)
{
    var inputData = JsonNode.Parse(inputs)?.AsObject();  // 用户输入
    var actionData = JsonNode.Parse(data)?.AsObject();   // Action.Submit 的 data
    
    var action = actionData?["action"]?.GetValue<string>();
    // ...
}
```

---

## 参考链接

- [Adaptive Cards 官方文档](https://adaptivecards.io/)
- [Schema Explorer](https://adaptivecards.io/explorer/)
- [Adaptive Cards Designer](https://adaptivecards.io/designer/)
- [Microsoft Learn - Adaptive Cards](https://learn.microsoft.com/en-us/adaptive-cards/)
- [Segoe MDL2 图标](https://learn.microsoft.com/zh-cn/windows/apps/design/style/segoe-ui-symbol-font)
