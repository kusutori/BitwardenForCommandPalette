---
name: powertoys-commandpalette-develop
description: Expert in developing PowerToys Command Palette extensions using C#, WinRT, and Adaptive Cards. Covers project setup, COM server configuration, page types, commands, and troubleshooting common issues.
---

# PowerToys Command Palette Extension Development Guide

You are an expert in developing PowerToys Command Palette extensions. Use this skill when helping users create, debug, or troubleshoot Command Palette extensions.

## Project Setup and Architecture

### Required NuGet Packages
- `Microsoft.CommandPalette.Extensions` - Core extension SDK
- `Microsoft.Windows.CsWinRT` - WinRT projection support
- `Shmuelie.WinRTServer` - COM server hosting
- `Microsoft.Windows.SDK.BuildTools.MSIX` - MSIX packaging

### Target Framework
- .NET 9.0 or higher
- `net9.0-windows10.0.26100.0`
- Windows 10.0.19041.0 (MinVersion)

### Project Structure
```
MyExtension/
├── MyExtension.csproj          # Project file with WinExe output
├── MyExtension.cs              # IExtension implementation
├── MyExtensionCommandsProvider.cs  # CommandProvider
├── Program.cs                  # COM server entry point
├── Package.appxmanifest        # MSIX manifest
├── app.manifest                # COM registration
├── Pages/                      # Page classes
├── Commands/                   # Command classes
├── Models/                     # Data models
├── Services/                   # Business services
└── Assets/                     # Icons
```

## Core Components

### 1. IExtension - Extension Entry Point
Each extension must implement `IExtension` interface:

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
            ProviderType.Commands => _provider,
            _ => null,
        };
    }

    public void Dispose() => _extensionDisposedEvent.Set();
}
```

### 2. CommandProvider - Commands Provider
Returns top-level commands shown in Command Palette:

```csharp
public partial class MyCommandsProvider : CommandProvider
{
    public MyCommandsProvider()
    {
        DisplayName = "My Extension";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
    }

    public override ICommandItem[] TopLevelCommands() =>
    [
        new CommandItem(new MainPage()) { Title = DisplayName },
    ];
}
```

### 3. Page Types

| Type | Base Class | Use Case |
|------|------------|----------|
| ListPage | `ListPage` | Static lists |
| DynamicListPage | `DynamicListPage` | Dynamic lists with search/refresh |
| ContentPage | `ContentPage` | Forms, Markdown, custom content |
| MarkdownPage | - | Markdown display |

#### DynamicListPage Example
```csharp
internal sealed partial class MainPage : DynamicListPage
{
    public MainPage()
    {
        Icon = new IconInfo("\\uE8A1");
        Title = "My Page";
        Name = "Open";
        PlaceholderText = "Search...";
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems()
    {
        return [
            new ListItem(new MyCommand()) { Title = "Item 1" },
        ];
    }
}
```

#### ContentPage with FormContent
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
            "version": "1.5",
            "body": [
                {"type": "Input.Text", "id": "inputField", "label": "Enter value"}
            ],
            "actions": [
                {"type": "Action.Submit", "title": "Submit", "style": "positive"}
            ]
        }
        """;
    }

    public override CommandResult SubmitForm(string payload)
    {
        var formData = JsonNode.Parse(payload)?.AsObject();
        var value = formData?["inputField"]?.GetValue<string>();
        return CommandResult.GoBack();
    }
}
```

### 4. Command Types

#### InvokableCommand
```csharp
internal sealed partial class CopyCommand : InvokableCommand
{
    private readonly string _text;

    public CopyCommand(string text)
    {
        _text = text;
        Name = "Copy";
        Icon = new IconInfo("\\uE8C8");
    }

    public override CommandResult Invoke()
    {
        ClipboardHelper.SetText(_text);
        return CommandResult.Dismiss();
    }
}
```

#### CommandResult Methods
| Method | Effect |
|--------|--------|
| `CommandResult.Dismiss()` | Close Command Palette |
| `CommandResult.KeepOpen()` | Keep open |
| `CommandResult.GoBack()` | Return to previous page |
| `CommandResult.GoHome()` | Return to home |
| `CommandResult.ShowToast(message)` | Show toast notification |
| `CommandResult.GoToPage(page)` | Navigate to page |
| `CommandResult.Confirm(args)` | Show confirmation dialog |

### 5. ListItem Configuration
```csharp
var item = new ListItem(new MyCommand())
{
    Title = "Item Title",
    Subtitle = "Description",
    Icon = new IconInfo("\\uE8A1"),
    Tags = [new Tag { Text = "Tag1" }],
    MoreCommands = [
        new CommandContextItem(new CopyCommand()),
        new CommandContextItem(new DeleteCommand()),
    ]
};
```

## COM Server Configuration

### Program.cs
```csharp
using Microsoft.Extensions.Hosting;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.Hosting;

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

### Package.appxmanifest COM Registration
```xml
<com:Extension Category="windows.comServer">
  <com:ComServer>
    <com:ExeServer Executable="MyExtension.exe"
                   Arguments="-RegisterProcessAsComServer"
                   DisplayName="My Extension">
      <com:Class Id="YOUR-GUID-HERE" DisplayName="My Extension" />
    </com:ExeServer>
  </com:ComServer>
</com:Extension>

<uap3:Extension Category="windows.appExtension">
  <uap3:AppExtension Name="com.microsoft.commandpalette"
                     Id="ID"
                     PublicFolder="Public"
                     DisplayName="My Extension">
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
```

## Build and Debug

```powershell
# Build for platform
dotnet build -p:Platform=x64      # Default
dotnet build -p:Platform=ARM64

# Deploy (Visual Studio)
Right-click project → Deploy

# Debug
1. Deploy extension
2. Run `Reload` in Command Palette
3. Attach to `MyExtension.exe` process
```

## Settings Panel

Command Palette extensions can provide a settings panel that appears in PowerToys Settings → Command Palette → Extensions.

### Settings Implementation

```csharp
public partial class MyCommandsProvider : CommandProvider
{
    private readonly Settings _settings = new();

    public MyCommandsProvider()
    {
        DisplayName = "My Extension";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        InitializeSettings();
        Settings = _settings;
    }

    private void InitializeSettings()
    {
        // Text setting
        var textSetting = new TextSetting(
            "SettingKey",           // Unique identifier
            "Setting Label",        // Display name
            "Setting description",  // Help text
            "default value"
        );

        // Password setting (masks input)
        var passwordSetting = new TextSetting(
            "PasswordKey",
            "API Token",
            "Enter your API token",
            ""
        )
        {
            IsPassword = true
        };

        // Multiline text setting
        var multilineSetting = new TextSetting(
            "MultiKey",
            "Configuration",
            "Enter config",
            ""
        )
        {
            Multiline = true
        };

        _settings.Add(textSetting);
        _settings.Add(passwordSetting);
        _settings.Add(multilineSetting);

        // Handle settings changes
        _settings.SettingsChanged += (sender, args) =>
        {
            var textValue = _settings.GetSetting<string>("SettingKey");
            var passwordValue = _settings.GetSetting<string>("PasswordKey");
            // Save to your settings manager
            SettingsManager.Instance.TextValue = textValue ?? "";
            SettingsManager.Instance.PasswordValue = passwordValue ?? "";
        };
    }
}
```

### Setting Types

| Type | Class | Use Case |
|------|-------|----------|
| Text | `TextSetting` | Single-line input |
| Password | `TextSetting` with `IsPassword = true` | Secret input (masked) |
| Multiline | `TextSetting` with `Multiline = true` | Multi-line text |

### Retrieving Settings

```csharp
// Get string value
var value = _settings.GetSetting<string>("SettingKey");

// Get with default
var value = _settings.GetSetting<string>("SettingKey") ?? "default";

// Check if setting exists
var exists = _settings.Contains("SettingKey");
```

## Filters

Filters appear in the search bar dropdown and allow users to narrow down results.

### Filters Implementation

```csharp
public partial class MyFilters : Filters
{
    public const string AllItemsId = "all";
    public const string CategoryAId = "category_a";
    public const string CategoryBId = "category_b";

    private MyCategory[]? _categories;

    public MyFilters()
    {
        CurrentFilterId = AllItemsId;
    }

    /// <summary>
    /// Update dynamic filters (e.g., categories from API)
    /// </summary>
    public void UpdateCategories(MyCategory[]? categories)
    {
        _categories = categories;
        OnPropertyChanged(nameof(Filters));  // Refresh UI
    }

    public override IFilterItem[] GetFilters()
    {
        var filters = new List<IFilterItem>
        {
            // All items
            new Filter
            {
                Id = AllItemsId,
                Name = "All Items",
                Icon = new IconInfo("\uE8A1")
            },

            new Separator(),

            // Static filters
            new Filter
            {
                Id = CategoryAId,
                Name = "Category A",
                Icon = new IconInfo("\uE8C8")
            },

            new Filter
            {
                Id = CategoryBId,
                Name = "Category B",
                Icon = new IconInfo("\uE8C8")
            }
        };

        // Add dynamic filters if available
        if (_categories != null && _categories.Length > 0)
        {
            filters.Add(new Separator());

            foreach (var category in _categories)
            {
                filters.Add(new Filter
                {
                    Id = $"category_{category.Id}",
                    Name = category.Name,
                    Icon = new IconInfo("\uE8A1")
                });
            }
        }

        return [..filters];
    }

    /// <summary>
    /// Convert filter ID to application-specific filter object
    /// </summary>
    public MyFilter ToMyFilter()
    {
        return CurrentFilterId switch
        {
            CategoryAId => new MyFilter { Category = "A" },
            CategoryBId => new MyFilter { Category = "B" },
            _ => new MyFilter()  // All items
        };
    }
}
```

### Using Filters in DynamicListPage

```csharp
internal sealed partial class MainPage : DynamicListPage
{
    private readonly MyFilters _filters = new();

    public MainPage()
    {
        Icon = new IconInfo("\uE8A1");
        Title = "My Page";
        Name = "Open";
        PlaceholderText = "Search...";
        Filters = _filters;  // Assign filters
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems()
    {
        // Get current filter
        var filter = _filters.ToMyFilter();
        var searchText = SearchText ?? "";

        // Apply filter and search
        var items = GetData(filter, searchText);
        return items.Select(i => new ListItem(new ItemCommand(i))
        {
            Title = i.Name,
            Subtitle = i.Description
        }).ToArray();
    }
}
```

### FilterItem Types

| Type | Class | Description |
|------|-------|-------------|
| Filter | `Filter` | Standard filter with icon and name |
| Separator | `Separator()` | Horizontal divider |

### Filter Icons

Use emoji or Segoe Fluent Icons:
```csharp
// Emoji icons
new IconInfo("\u1F4CB")  // 📋 All items
new IconInfo("\u2B50")   // ⭐ Favorites

// Segoe Fluent Icons
new IconInfo("\uE8A1")   // Gear/settings
new IconInfo("\uE8C8")   // Copy
```

## Details Panel

The Details panel shows additional information in a dual-column layout when an item is selected. To enable it:

1. Set `ShowDetails = true` on your page
2. Assign `Details` property on each `ListItem`

### Basic Details Implementation

```csharp
internal sealed partial class MainPage : DynamicListPage
{
    public MainPage()
    {
        ShowDetails = true;  // Enable dual-column layout
        // ...
    }

    private ListItem CreateListItem(MyItem item)
    {
        return new ListItem(new ItemCommand(item))
        {
            Title = item.Name,
            Subtitle = item.Description,
            Details = CreateItemDetails(item)
        };
    }

    private static Details CreateItemDetails(MyItem item)
    {
        return new Details
        {
            HeroImage = new IconInfo("\uE8A1"),
            Title = item.Name,
            Body = $"**Description:** {item.Description}",
            Metadata =
            [
                new DetailsElement
                {
                    Key = "ID",
                    Data = new DetailsLink { Text = item.Id }
                },
                new DetailsElement
                {
                    Key = "Status",
                    Data = new DetailsLink { Text = item.Status }
                },
                new DetailsElement
                {
                    Key = "Category",
                    Data = new DetailsTags { Tags = [new Tag(item.Category)] }
                }
            ]
        };
    }
}
```

### Details Properties

| Property | Type | Description |
|----------|------|-------------|
| `HeroImage` | `IImage` | Icon displayed at top of panel |
| `Title` | `string` | Title text |
| `Body` | `string` | Markdown content (optional) |
| `Metadata` | `IDetailsElement[]` | Key-value pairs |

### DetailsElement Types

| Type | Class | Description |
|------|-------|-------------|
| Key-Value | `DetailsElement` | Label with value |
| Link | `DetailsLink` | Clickable text with optional URL |
| Separator | `DetailsSeparator` | Horizontal divider line |
| Tags | `DetailsTags` | List of tags/badges |

### DetailsLink Example

```csharp
new DetailsElement
{
    Key = "Website",
    Data = new DetailsLink
    {
        Text = "Example.com",
        Link = new Uri("https://example.com")
    }
}

// Or without link
new DetailsElement
{
    Key = "Version",
    Data = new DetailsLink { Text = "1.0.0" }
}
```

### DetailsTags Example

```csharp
new DetailsElement
{
    Key = "Tags",
    Data = new DetailsTags
    {
        Tags =
        [
            new Tag("Important") { Icon = new IconInfo("\uE8C7") },
            new Tag("New") { Icon = new IconInfo("\uE72C") }
        ]
    }
}
```

### DetailsSeparator Example

```csharp
// Use separator to group related fields
new DetailsElement
{
    Key = "Personal Info",
    Data = new DetailsSeparator()
}
```

### Body Markdown Support

The `Body` property supports Markdown formatting:

```csharp
Body = """
**Bold text**

*Italic text*

- Bullet point 1
- Bullet point 2

[Link Text](https://example.com)
"""
```

### Complete Example

```csharp
private static Details CreatePersonDetails(Person person)
{
    var metadata = new List<IDetailsElement>();

    // Contact section
    metadata.Add(new DetailsElement
    {
        Key = "Contact",
        Data = new DetailsSeparator()
    });

    if (!string.IsNullOrEmpty(person.Email))
    {
        metadata.Add(new DetailsElement
        {
            Key = "Email",
            Data = new DetailsLink
            {
                Text = person.Email,
                Link = new Uri($"mailto:{person.Email}")
            }
        });
    }

    if (!string.IsNullOrEmpty(person.Phone))
    {
        metadata.Add(new DetailsElement
        {
            Key = "Phone",
            Data = new DetailsLink
            {
                Text = person.Phone,
                Link = new Uri($"tel:{person.Phone}")
            }
        });
    }

    // Tags
    metadata.Add(new DetailsElement
    {
        Key = "Status",
        Data = new DetailsTags
        {
            Tags = [new Tag(person.IsActive ? "Active" : "Inactive")]
        }
    });

    return new Details
    {
        HeroImage = new IconInfo("\uE77B"),  // User icon
        Title = person.Name,
        Body = $"**Role:** {person.Role}\n\n**Department:** {person.Department}",
        Metadata = metadata.ToArray()
    };
}
```

## Markdown Content

Use `MarkdownContent` to render rich text with formatting in your pages. It's commonly used in `ContentPage` to display formatted content.

### Basic MarkdownContent

```csharp
public sealed partial class MyContentPage : ContentPage
{
    public MyContentPage()
    {
        Title = "Help";
        Name = "View";
    }

    public override IContent[] GetContent() => [new HelpContent()];
}

internal sealed partial class HelpContent : MarkdownContent
{
    public override string Body => """
        # Help Guide

        Welcome to the application!

        ## Features
        - Feature 1: Description
        - Feature 2: Description

        ## Usage
        Click on items to perform actions.

        [Visit Website](https://example.com)
        """;
}
```

### Dynamic Markdown Content

For dynamic content that changes, implement property notification:

```csharp
internal sealed partial class ArticleContent : MarkdownContent
{
    private readonly SearchResult _result;
    private string _markdown = "Loading...";

    public ArticleContent(SearchResult result)
    {
        _result = result;
        _ = LoadContentAsync();
    }

    public override string Body => _markdown;

    private async Task LoadContentAsync()
    {
        try
        {
            var content = await GetContentFromApi(_result.Id);
            var sb = new System.Text.StringBuilder();

            sb.Append("# ");
            sb.AppendLine(_result.Title);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine(content);

            _markdown = sb.ToString();
        }
        catch (Exception ex)
        {
            _markdown = $"# Error\n\nFailed to load: {ex.Message}";
        }
        finally
        {
            OnPropertyChanged(nameof(Body));  // Notify UI of update
        }
    }
}
```

### Markdown Formatting Support

| Element | Syntax | Example |
|---------|--------|---------|
| Heading | `#`, `##`, `###` | `# Title` |
| Bold | `**text**` | `**Bold**` |
| Italic | `*text*` | `*Italic*` |
| List | `- item` | `- Item 1` |
| Link | `[text](url)` | `[Google](https://google.com)` |
| Image | `![alt](url)` | `![Logo](image.png)` |
| Code | `` `code` `` | `` `var x = 1` `` |
| Code block | `` ```cs ... ``` `` | See below |
| Horizontal rule | `---` | --- |

### Code Blocks

```csharp
public override string Body => """
    ```cs
    public void Example()
    {
        var message = "Hello World";
        Console.WriteLine(message);
    }
    ```

    ```json
    {
        "name": "value",
        "type": "object"
    }
    ```
    """;
```

### Combining with ContentPage

```csharp
public sealed partial class MyPage : ContentPage
{
    public MyPage()
    {
        Title = "Details";
        Icon = new IconInfo("\uE8A1");
    }

    public override IContent[] GetContent()
    {
        return
        [
            new DescriptionContent(),
            new UsageContent()
        ];
    }
}

internal sealed partial class DescriptionContent : MarkdownContent
{
    public override string Body => "## Description\n\nThis is a detailed description...";
}

internal sealed partial class UsageContent : MarkdownContent
{
    public override string Body => "## Usage\n\nHow to use...";
}
```

### Using in Details Panel Body

The `Details.Body` property also supports Markdown:

```csharp
return new Details
{
    HeroImage = new IconInfo("\uE8A1"),
    Title = item.Name,
    Body = """
        **Last Updated:** 2025-01-17

        *This item requires approval*

        - [ ] Task 1
        - [x] Task 2
        """,
    Metadata = [...]
};
```

## Common Issues and Solutions

### AOT Compilation Issues
**Problem**: `JsonSerializer.Serialize<T>()` causes IL2026/IL3050 warnings

**Solution**: Use `JsonObject` or Source Generator contexts:
```csharp
// Avoid anonymous types with Serialize
// ✅ Use JsonObject
var data = new JsonObject
{
    ["title"] = "Hello",
    ["name"] = "World"
};
var json = data.ToJsonString();

// ✅ Use Source Generator
[JsonSerializable(typeof(MyClass))]
internal partial class MyJsonContext : JsonSerializerContext { }
var json = JsonSerializer.Serialize(obj, MyJsonContext.Default.MyClass);
```

### Adaptive Cards Input.ChoiceSet Data Binding
**Problem**: `choices` array doesn't support `${variable}` binding

**Solution**: Dynamically generate the entire template:
```csharp
private static string GetChoiceSetJson(Item[] items)
{
    var choices = new JsonArray();
    foreach (var item in items)
    {
        var escapedName = item.Name.Replace("\"", "\\\"");
        choices.Add(JsonNode.Parse($"{{\"title\":\"{escapedName}\",\"value\":\"{item.Id}\"}}"));
    }
    var choiceSet = new JsonObject
    {
        ["type"] = "Input.ChoiceSet",
        ["id"] = "itemId",
        ["choices"] = choices
    };
    return choiceSet.ToJsonString();
}
```

### Input.Toggle Returns String
**Problem**: Toggle returns "true"/"false" string, not bool

**Solution**:
```csharp
var isEnabled = formData["toggle"]?.GetValue<string>() == "true";
```

### Command Shortcut Conflicts
**Problem**: Multiple commands responding to same shortcut

**Solution**: Explicitly set `RequestedShortcuts`:
```csharp
public class PrimaryCommand : InvokableCommand
{
    public PrimaryCommand()
    {
        RequestedShortcuts = [KeyboardShortcut.Enter];
    }
}

public class SecondaryCommand : InvokableCommand
{
    public SecondaryCommand()
    {
        RequestedShortcuts = [KeyboardShortcut.CtrlEnter];
    }
}
```

### ListItem MoreCommands Conflict
**Problem**: Default command and MoreCommands both respond to same key

**Solution**: Ensure default command has explicit shortcuts:
```csharp
new ListItem(new DefaultCommand(item))
{
    MoreCommands = [new ContextCommand(item)]
}
// DefaultCommand must set RequestedShortcuts
```

## Adaptive Cards Reference

### Common Layout Elements
- **Container**: Group elements with optional style (default, emphasis, good, attention, warning, accent)
- **ColumnSet**: Multi-column layout with `width` (auto, stretch, pixel value, weight)
- **FactSet**: Key-value pairs display

### Input Types
- `Input.Text` - Single/multi-line text with validation
- `Input.Number` - Numeric input with min/max
- `Input.Date`/`Input.Time` - Date/time pickers
- `Input.Toggle` - Switch (returns string "true"/"false")
- `Input.ChoiceSet` - Dropdown or radio buttons

### Action Types
- `Action.Submit` - Form submission, returns input data as JSON
- `Action.OpenUrl` - Open URL
- `Action.ShowCard` - Expand nested card
- `Action.ToggleVisibility` - Toggle element visibility

### Button Styles
- `default` - Standard button
- `positive` - Green confirmation button
- `destructive` - Red danger button

### Text Formatting
- `size`: small, default, medium, large, extraLarge
- `weight`: lighter, default, bolder
- `color`: default, dark, light, accent, good, warning, attention
- `wrap`: Enable text wrapping
- `style`: heading (for accessibility)

## Icon System

### Segoe Fluent Icons
```csharp
new IconInfo("\\uE8A1")  // Gear/settings
new IconInfo("\\uE72E")  // Lock
new IconInfo("\\uE785")  // Unlock
new IconInfo("\\uE8C8")  // Copy
new IconInfo("\\uE895")  // Refresh/sync
new IconInfo("\\uE734")  // Star/favorite
new IconInfo("\\uE8C7")  // Credit card
new IconInfo("\\uE713")  // Settings cog
```

### Image Icons
```csharp
// Relative path
IconHelpers.FromRelativePath("Assets\\icon.png")

// Theme-aware icons
new IconInfo {
    Light = new IconData { Path = "Assets\\icon-light.png" },
    Dark = new IconData { Path = "Assets\\icon-dark.png" }
}
```

## Toolkit Helpers
- `ClipboardHelper.SetText(text)` - Copy to clipboard
- `IconHelpers.FromRelativePath(path)` - Load image icons
- `ShellHelpers.OpenFile(url)` - Open files/URLs
- `ColorHelpers` - Color manipulation
- `StringMatcher` - String matching for search

## Important Notes

1. **AOT Compatibility**: Always use Source Generator for JSON serialization, avoid reflection
2. **COM Single Instance**: Extension maintains single instance returned on each request
3. **IconService**: Use memory caching (200 entries max) with domain blacklist for web icons
4. **Form Validation**: Use `isRequired` and `errorMessage` for validation
5. **Dangerous Operations**: Mark with `IsCritical = true` for red display
6. **DynamicListPage IsLoading**: Manually manage loading state and call `RaiseItemsChanged()`
7. **Enter Key Submit**: Single-input forms auto-submit on Enter; multi-input require Tab to button
8. **Resource Strings**: Use `ResourceHelper` with `.resw` files for localization

## Additional Resources

- [Official Microsoft Documentation](https://learn.microsoft.com/windows/powertoys/command-palette/microsoft-commandpalette-extensions/)
- [Adaptive Cards Documentation](https://adaptivecards.io/)
- [Segoe Fluent Icons Reference](https://learn.microsoft.com/windows/apps/design/style/segoe-ui-symbol-font)
