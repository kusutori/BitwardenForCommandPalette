// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BitwardenForCommandPalette.Helpers;
using BitwardenForCommandPalette.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.ApplicationModel.DataTransfer;

namespace BitwardenForCommandPalette.Pages;

/// <summary>
/// Page for generating random passwords using Adaptive Cards
/// </summary>
internal sealed partial class PasswordGeneratorPage : ContentPage
{
    private readonly PasswordGeneratorForm _generatorForm;

    public PasswordGeneratorPage()
    {
        _generatorForm = new PasswordGeneratorForm();
        Icon = new IconInfo("\uE8D7"); // Key icon
        Name = ResourceHelper.GeneratorPassword;
        Title = ResourceHelper.GeneratorPasswordTitle;
    }

    public override IContent[] GetContent() => [_generatorForm];
}

/// <summary>
/// Form content for generating random passwords
/// </summary>
internal sealed partial class PasswordGeneratorForm : FormContent
{
    private string _generatedPassword = string.Empty;

    public PasswordGeneratorForm()
    {
        TemplateJson = GetPasswordGeneratorTemplate();
        DataJson = GetPasswordGeneratorData();
    }

    private static string GetPasswordGeneratorTemplate()
    {
        return """
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        {
            "type": "TextBlock",
            "text": "${title}",
            "size": "large",
            "weight": "bolder",
            "spacing": "medium"
        },
        {
            "type": "TextBlock",
            "text": "${description}",
            "wrap": true,
            "spacing": "small"
        },
        {
            "type": "Container",
            "style": "emphasis",
            "items": [
                {
                    "type": "TextBlock",
                    "id": "generatedPassword",
                    "text": "${generatedPassword}",
                    "fontType": "monospace",
                    "size": "large",
                    "weight": "bolder",
                    "wrap": true,
                    "horizontalAlignment": "center"
                }
            ],
            "spacing": "medium"
        },
        {
            "type": "Input.Number",
            "id": "length",
            "label": "${lengthLabel}",
            "value": ${length},
            "min": 5,
            "max": 128
        },
        {
            "type": "Input.Toggle",
            "id": "uppercase",
            "title": "${uppercaseLabel}",
            "value": "${uppercase}",
            "valueOn": "true",
            "valueOff": "false"
        },
        {
            "type": "Input.Toggle",
            "id": "lowercase",
            "title": "${lowercaseLabel}",
            "value": "${lowercase}",
            "valueOn": "true",
            "valueOff": "false"
        },
        {
            "type": "Input.Toggle",
            "id": "numbers",
            "title": "${numbersLabel}",
            "value": "${numbers}",
            "valueOn": "true",
            "valueOff": "false"
        },
        {
            "type": "Input.Toggle",
            "id": "special",
            "title": "${specialLabel}",
            "value": "${special}",
            "valueOn": "true",
            "valueOff": "false"
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "${generateButton}",
            "data": {
                "action": "generate"
            }
        },
        {
            "type": "Action.Submit",
            "title": "${copyButton}",
            "style": "positive",
            "data": {
                "action": "copy"
            }
        }
    ]
}
""";
    }

    private string GetPasswordGeneratorData()
    {
        var data = new JsonObject
        {
            ["title"] = ResourceHelper.GeneratorPasswordTitle,
            ["description"] = ResourceHelper.GeneratorPasswordDescription,
            ["generatedPassword"] = string.IsNullOrEmpty(_generatedPassword)
                ? ResourceHelper.GeneratorClickGenerate
                : _generatedPassword,
            ["lengthLabel"] = ResourceHelper.GeneratorLengthLabel,
            ["length"] = 16,
            ["uppercaseLabel"] = ResourceHelper.GeneratorUppercaseLabel,
            ["uppercase"] = "true",
            ["lowercaseLabel"] = ResourceHelper.GeneratorLowercaseLabel,
            ["lowercase"] = "true",
            ["numbersLabel"] = ResourceHelper.GeneratorNumbersLabel,
            ["numbers"] = "true",
            ["specialLabel"] = ResourceHelper.GeneratorSpecialLabel,
            ["special"] = "false",
            ["generateButton"] = ResourceHelper.GeneratorGenerateButton,
            ["copyButton"] = ResourceHelper.GeneratorCopyButton
        };

        return data.ToJsonString();
    }

    public override CommandResult SubmitForm(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var action = root.GetProperty("action").GetString();

            if (action == "generate")
            {
                return GeneratePassword(root);
            }
            else if (action == "copy")
            {
                return CopyPassword();
            }

            return CommandResult.KeepOpen();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SubmitForm error: {ex.Message}");
            return CommandResult.ShowToast(ResourceHelper.GeneratorGenerateFailed);
        }
    }

    private CommandResult GeneratePassword(JsonElement root)
    {
        var length = 16;
        var uppercase = true;
        var lowercase = true;
        var numbers = true;
        var special = false;

        if (root.TryGetProperty("length", out var lengthProp))
        {
            if (lengthProp.ValueKind == JsonValueKind.Number)
            {
                length = lengthProp.GetInt32();
            }
            else if (lengthProp.ValueKind == JsonValueKind.String &&
                     int.TryParse(lengthProp.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLength))
            {
                length = parsedLength;
            }
        }

        if (root.TryGetProperty("uppercase", out var upperProp))
            uppercase = upperProp.GetString() == "true";
        if (root.TryGetProperty("lowercase", out var lowerProp))
            lowercase = lowerProp.GetString() == "true";
        if (root.TryGetProperty("numbers", out var numProp))
            numbers = numProp.GetString() == "true";
        if (root.TryGetProperty("special", out var specProp))
            special = specProp.GetString() == "true";

        // Generate the password
        var password = BitwardenCliService.GeneratePasswordAsync(
            length, uppercase, lowercase, numbers, special).GetAwaiter().GetResult();

        if (string.IsNullOrEmpty(password))
        {
            return CommandResult.ShowToast(ResourceHelper.GeneratorGenerateFailed);
        }

        _generatedPassword = password;

        // Update the form data with the new password and preserve settings
        var data = new JsonObject
        {
            ["title"] = ResourceHelper.GeneratorPasswordTitle,
            ["description"] = ResourceHelper.GeneratorPasswordDescription,
            ["generatedPassword"] = _generatedPassword,
            ["lengthLabel"] = ResourceHelper.GeneratorLengthLabel,
            ["length"] = length,
            ["uppercaseLabel"] = ResourceHelper.GeneratorUppercaseLabel,
            ["uppercase"] = uppercase ? "true" : "false",
            ["lowercaseLabel"] = ResourceHelper.GeneratorLowercaseLabel,
            ["lowercase"] = lowercase ? "true" : "false",
            ["numbersLabel"] = ResourceHelper.GeneratorNumbersLabel,
            ["numbers"] = numbers ? "true" : "false",
            ["specialLabel"] = ResourceHelper.GeneratorSpecialLabel,
            ["special"] = special ? "true" : "false",
            ["generateButton"] = ResourceHelper.GeneratorGenerateButton,
            ["copyButton"] = ResourceHelper.GeneratorCopyButton
        };

        DataJson = data.ToJsonString();

        return CommandResult.KeepOpen();
    }

    private CommandResult CopyPassword()
    {
        if (string.IsNullOrEmpty(_generatedPassword))
        {
            return CommandResult.ShowToast(ResourceHelper.GeneratorNoPassword);
        }

        var dataPackage = new DataPackage();
        dataPackage.SetText(_generatedPassword);
        Clipboard.SetContent(dataPackage);

        return CommandResult.ShowToast(ResourceHelper.GeneratorPasswordCopied);
    }
}

/// <summary>
/// Page for generating random passphrases using Adaptive Cards
/// </summary>
internal sealed partial class PassphraseGeneratorPage : ContentPage
{
    private readonly PassphraseGeneratorForm _generatorForm;

    public PassphraseGeneratorPage()
    {
        _generatorForm = new PassphraseGeneratorForm();
        Icon = new IconInfo("\uE8F1"); // Document icon
        Name = ResourceHelper.GeneratorPassphrase;
        Title = ResourceHelper.GeneratorPassphraseTitle;
    }

    public override IContent[] GetContent() => [_generatorForm];
}

/// <summary>
/// Form content for generating random passphrases
/// </summary>
internal sealed partial class PassphraseGeneratorForm : FormContent
{
    private string _generatedPassphrase = string.Empty;

    public PassphraseGeneratorForm()
    {
        TemplateJson = GetPassphraseGeneratorTemplate();
        DataJson = GetPassphraseGeneratorData();
    }

    private static string GetPassphraseGeneratorTemplate()
    {
        return """
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        {
            "type": "TextBlock",
            "text": "${title}",
            "size": "large",
            "weight": "bolder",
            "spacing": "medium"
        },
        {
            "type": "TextBlock",
            "text": "${description}",
            "wrap": true,
            "spacing": "small"
        },
        {
            "type": "Container",
            "style": "emphasis",
            "items": [
                {
                    "type": "TextBlock",
                    "id": "generatedPassphrase",
                    "text": "${generatedPassphrase}",
                    "fontType": "monospace",
                    "size": "large",
                    "weight": "bolder",
                    "wrap": true,
                    "horizontalAlignment": "center"
                }
            ],
            "spacing": "medium"
        },
        {
            "type": "Input.Number",
            "id": "words",
            "label": "${wordsLabel}",
            "value": ${words},
            "min": 3,
            "max": 20
        },
        {
            "type": "Input.Text",
            "id": "separator",
            "label": "${separatorLabel}",
            "value": "${separator}",
            "maxLength": 5
        },
        {
            "type": "Input.Toggle",
            "id": "capitalize",
            "title": "${capitalizeLabel}",
            "value": "${capitalize}",
            "valueOn": "true",
            "valueOff": "false"
        },
        {
            "type": "Input.Toggle",
            "id": "includeNumber",
            "title": "${includeNumberLabel}",
            "value": "${includeNumber}",
            "valueOn": "true",
            "valueOff": "false"
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "${generateButton}",
            "data": {
                "action": "generate"
            }
        },
        {
            "type": "Action.Submit",
            "title": "${copyButton}",
            "style": "positive",
            "data": {
                "action": "copy"
            }
        }
    ]
}
""";
    }

    private string GetPassphraseGeneratorData()
    {
        var data = new JsonObject
        {
            ["title"] = ResourceHelper.GeneratorPassphraseTitle,
            ["description"] = ResourceHelper.GeneratorPassphraseDescription,
            ["generatedPassphrase"] = string.IsNullOrEmpty(_generatedPassphrase)
                ? ResourceHelper.GeneratorClickGenerate
                : _generatedPassphrase,
            ["wordsLabel"] = ResourceHelper.GeneratorWordsLabel,
            ["words"] = 4,
            ["separatorLabel"] = ResourceHelper.GeneratorSeparatorLabel,
            ["separator"] = "-",
            ["capitalizeLabel"] = ResourceHelper.GeneratorCapitalizeLabel,
            ["capitalize"] = "true",
            ["includeNumberLabel"] = ResourceHelper.GeneratorIncludeNumberLabel,
            ["includeNumber"] = "false",
            ["generateButton"] = ResourceHelper.GeneratorGenerateButton,
            ["copyButton"] = ResourceHelper.GeneratorCopyButton
        };

        return data.ToJsonString();
    }

    public override CommandResult SubmitForm(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var action = root.GetProperty("action").GetString();

            if (action == "generate")
            {
                return GeneratePassphrase(root);
            }
            else if (action == "copy")
            {
                return CopyPassphrase();
            }

            return CommandResult.KeepOpen();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SubmitForm error: {ex.Message}");
            return CommandResult.ShowToast(ResourceHelper.GeneratorGenerateFailed);
        }
    }

    private CommandResult GeneratePassphrase(JsonElement root)
    {
        var words = 4;
        var separator = "-";
        var capitalize = true;
        var includeNumber = false;

        if (root.TryGetProperty("words", out var wordsProp))
        {
            if (wordsProp.ValueKind == JsonValueKind.Number)
            {
                words = wordsProp.GetInt32();
            }
            else if (wordsProp.ValueKind == JsonValueKind.String &&
                     int.TryParse(wordsProp.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedWords))
            {
                words = parsedWords;
            }
        }

        if (root.TryGetProperty("separator", out var sepProp))
            separator = sepProp.GetString() ?? "-";
        if (root.TryGetProperty("capitalize", out var capProp))
            capitalize = capProp.GetString() == "true";
        if (root.TryGetProperty("includeNumber", out var numProp))
            includeNumber = numProp.GetString() == "true";

        // Generate the passphrase
        var passphrase = BitwardenCliService.GeneratePassphraseAsync(
            words, separator, capitalize, includeNumber).GetAwaiter().GetResult();

        if (string.IsNullOrEmpty(passphrase))
        {
            return CommandResult.ShowToast(ResourceHelper.GeneratorGenerateFailed);
        }

        _generatedPassphrase = passphrase;

        // Update the form data with the new passphrase and preserve settings
        var data = new JsonObject
        {
            ["title"] = ResourceHelper.GeneratorPassphraseTitle,
            ["description"] = ResourceHelper.GeneratorPassphraseDescription,
            ["generatedPassphrase"] = _generatedPassphrase,
            ["wordsLabel"] = ResourceHelper.GeneratorWordsLabel,
            ["words"] = words,
            ["separatorLabel"] = ResourceHelper.GeneratorSeparatorLabel,
            ["separator"] = separator,
            ["capitalizeLabel"] = ResourceHelper.GeneratorCapitalizeLabel,
            ["capitalize"] = capitalize ? "true" : "false",
            ["includeNumberLabel"] = ResourceHelper.GeneratorIncludeNumberLabel,
            ["includeNumber"] = includeNumber ? "true" : "false",
            ["generateButton"] = ResourceHelper.GeneratorGenerateButton,
            ["copyButton"] = ResourceHelper.GeneratorCopyButton
        };

        DataJson = data.ToJsonString();

        return CommandResult.KeepOpen();
    }

    private CommandResult CopyPassphrase()
    {
        if (string.IsNullOrEmpty(_generatedPassphrase))
        {
            return CommandResult.ShowToast(ResourceHelper.GeneratorNoPassphrase);
        }

        var dataPackage = new DataPackage();
        dataPackage.SetText(_generatedPassphrase);
        Clipboard.SetContent(dataPackage);

        return CommandResult.ShowToast(ResourceHelper.GeneratorPassphraseCopied);
    }
}
