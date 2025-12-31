// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BitwardenForCommandPalette.Helpers;
using BitwardenForCommandPalette.Models;
using BitwardenForCommandPalette.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette.Pages;

/// <summary>
/// Page for selecting the type of item to create
/// </summary>
internal sealed partial class CreateItemTypeSelectorPage : ListPage
{
    private readonly Action? _onCreated;
    private BitwardenFolder[]? _folders;

    public CreateItemTypeSelectorPage(Action? onCreated = null)
    {
        _onCreated = onCreated;
        Icon = new IconInfo("\uE710"); // Add icon
        Name = ResourceHelper.CreateItemPageTitle;
        Title = ResourceHelper.CreateItemPageTitle;
        PlaceholderText = ResourceHelper.CreateItemPagePlaceholder;
    }

    public override IListItem[] GetItems()
    {
        // Load folders for create item pages
        _folders = BitwardenCliService.Instance.GetFoldersAsync().Result;

        return
        [
            new ListItem(new CreateItemPage(BitwardenItemType.Login, _folders, _onCreated))
            {
                Title = ResourceHelper.CreateItemTypeLogin,
                Subtitle = ResourceHelper.CreateItemTypeLoginSubtitle,
                Icon = new IconInfo("\uE77B") // Contact icon
            },
            new ListItem(new CreateItemPage(BitwardenItemType.Card, _folders, _onCreated))
            {
                Title = ResourceHelper.CreateItemTypeCard,
                Subtitle = ResourceHelper.CreateItemTypeCardSubtitle,
                Icon = new IconInfo("\uE8C7") // Payment icon
            },
            new ListItem(new CreateItemPage(BitwardenItemType.Identity, _folders, _onCreated))
            {
                Title = ResourceHelper.CreateItemTypeIdentity,
                Subtitle = ResourceHelper.CreateItemTypeIdentitySubtitle,
                Icon = new IconInfo("\uE779") // Contact2 icon
            },
            new ListItem(new CreateItemPage(BitwardenItemType.SecureNote, _folders, _onCreated))
            {
                Title = ResourceHelper.CreateItemTypeNote,
                Subtitle = ResourceHelper.CreateItemTypeNoteSubtitle,
                Icon = new IconInfo("\uE8F1") // Document icon
            },
            // Folder creation
            new ListItem(new CreateFolderPage(_onCreated))
            {
                Title = ResourceHelper.CreateFolderTitle,
                Subtitle = ResourceHelper.CreateFolderSubtitle,
                Icon = new IconInfo("\uE8B7") // Folder icon
            },
            // Separator for generators
            new ListItem(new PasswordGeneratorPage())
            {
                Title = ResourceHelper.GeneratorPassword,
                Subtitle = ResourceHelper.GeneratorPasswordSubtitle,
                Icon = new IconInfo("\uE8D7") // Key icon
            },
            new ListItem(new PassphraseGeneratorPage())
            {
                Title = ResourceHelper.GeneratorPassphrase,
                Subtitle = ResourceHelper.GeneratorPassphraseSubtitle,
                Icon = new IconInfo("\uE8F1") // Document icon
            }
        ];
    }
}

/// <summary>
/// Page for creating a new vault item using Adaptive Cards
/// </summary>
internal sealed partial class CreateItemPage : ContentPage
{
    private readonly CreateItemForm _createForm;

    public CreateItemPage(BitwardenItemType itemType, BitwardenFolder[]? folders, Action? onCreated = null)
    {
        _createForm = new CreateItemForm(itemType, folders, onCreated);
        Icon = new IconInfo("\uE710"); // Add icon
        Name = GetNameForItemType(itemType);
        Title = GetTitleForItemType(itemType);
    }

    private static string GetNameForItemType(BitwardenItemType itemType)
    {
        return itemType switch
        {
            BitwardenItemType.Login => ResourceHelper.CreateItemTypeLogin,
            BitwardenItemType.Card => ResourceHelper.CreateItemTypeCard,
            BitwardenItemType.Identity => ResourceHelper.CreateItemTypeIdentity,
            BitwardenItemType.SecureNote => ResourceHelper.CreateItemTypeNote,
            _ => ResourceHelper.CreateItemPageTitle
        };
    }

    private static string GetTitleForItemType(BitwardenItemType itemType)
    {
        return itemType switch
        {
            BitwardenItemType.Login => ResourceHelper.CreateItemTitleLogin,
            BitwardenItemType.Card => ResourceHelper.CreateItemTitleCard,
            BitwardenItemType.Identity => ResourceHelper.CreateItemTitleIdentity,
            BitwardenItemType.SecureNote => ResourceHelper.CreateItemTitleNote,
            _ => ResourceHelper.CreateItemPageTitle
        };
    }

    public override IContent[] GetContent() => [_createForm];
}

/// <summary>
/// Form content for creating a new vault item
/// </summary>
internal sealed partial class CreateItemForm : FormContent
{
    private readonly BitwardenItemType _itemType;
    private readonly BitwardenFolder[]? _folders;
    private readonly Action? _onCreated;

    public CreateItemForm(BitwardenItemType itemType, BitwardenFolder[]? folders, Action? onCreated = null)
    {
        _itemType = itemType;
        _folders = folders;
        _onCreated = onCreated;

        // Set the template based on item type
        TemplateJson = GetTemplateForItemType(itemType, folders);
        DataJson = GetDataForNewItem(itemType);
    }

    private static string GetTemplateForItemType(BitwardenItemType itemType, BitwardenFolder[]? folders)
    {
        return itemType switch
        {
            BitwardenItemType.Login => GetLoginTemplate(folders),
            BitwardenItemType.Card => GetCardTemplate(folders),
            BitwardenItemType.Identity => GetIdentityTemplate(folders),
            BitwardenItemType.SecureNote => GetSecureNoteTemplate(folders),
            _ => GetLoginTemplate(folders)
        };
    }

    private static string GetFolderChoiceSetJson(BitwardenFolder[]? folders)
    {
        // Build the folder Input.ChoiceSet
        var choices = new JsonArray();

        // Add "No Folder" option first - use JsonNode.Parse to avoid AOT warnings
        var noFolderTitle = ResourceHelper.CreateItemNoFolder.Replace("\"", "\\\"");
        choices.Add(JsonNode.Parse($"{{\"title\":\"{noFolderTitle}\",\"value\":\"\"}}"));

        // Add all folders
        if (folders != null)
        {
            foreach (var folder in folders)
            {
                if (!string.IsNullOrEmpty(folder.Id) && !string.IsNullOrEmpty(folder.Name))
                {
                    // Escape special characters in folder name
                    var escapedName = folder.Name.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    choices.Add(JsonNode.Parse($"{{\"title\":\"{escapedName}\",\"value\":\"{folder.Id}\"}}"));
                }
            }
        }

        var choiceSet = new JsonObject
        {
            ["type"] = "Input.ChoiceSet",
            ["id"] = "folderId",
            ["label"] = "${folderLabel}",
            ["value"] = "",
            ["style"] = "compact",
            ["choices"] = choices
        };

        return choiceSet.ToJsonString();
    }

    private static string GetLoginTemplate(BitwardenFolder[]? folders)
    {
        var folderChoiceSet = GetFolderChoiceSetJson(folders);
        return $$"""
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
            "type": "Input.Text",
            "id": "name",
            "label": "${nameLabel}",
            "value": "${name}",
            "isRequired": true,
            "errorMessage": "${nameRequired}"
        },
        {{folderChoiceSet}},
        {
            "type": "Input.Text",
            "id": "username",
            "label": "${usernameLabel}",
            "value": "${username}"
        },
        {
            "type": "Input.Text",
            "id": "password",
            "label": "${passwordLabel}",
            "value": "${password}",
            "style": "password"
        },
        {
            "type": "Input.Text",
            "id": "url",
            "label": "${urlLabel}",
            "value": "${url}",
            "style": "url"
        },
        {
            "type": "Input.Text",
            "id": "totp",
            "label": "${totpLabel}",
            "value": "${totp}"
        },
        {
            "type": "Input.Text",
            "id": "notes",
            "label": "${notesLabel}",
            "value": "${notes}",
            "isMultiline": true
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "${createButton}",
            "style": "positive",
            "data": {
                "action": "create"
            }
        }
    ]
}
""";
    }

    private static string GetCardTemplate(BitwardenFolder[]? folders)
    {
        var folderChoiceSet = GetFolderChoiceSetJson(folders);
        return $$"""
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
            "type": "Input.Text",
            "id": "name",
            "label": "${nameLabel}",
            "value": "${name}",
            "isRequired": true,
            "errorMessage": "${nameRequired}"
        },
        {{folderChoiceSet}},
        {
            "type": "Input.Text",
            "id": "cardholderName",
            "label": "${cardholderNameLabel}",
            "value": "${cardholderName}"
        },
        {
            "type": "Input.Text",
            "id": "number",
            "label": "${cardNumberLabel}",
            "value": "${number}"
        },
        {
            "type": "ColumnSet",
            "columns": [
                {
                    "type": "Column",
                    "width": "stretch",
                    "items": [
                        {
                            "type": "Input.Text",
                            "id": "expMonth",
                            "label": "${expMonthLabel}",
                            "value": "${expMonth}",
                            "placeholder": "MM"
                        }
                    ]
                },
                {
                    "type": "Column",
                    "width": "stretch",
                    "items": [
                        {
                            "type": "Input.Text",
                            "id": "expYear",
                            "label": "${expYearLabel}",
                            "value": "${expYear}",
                            "placeholder": "YYYY"
                        }
                    ]
                }
            ]
        },
        {
            "type": "Input.Text",
            "id": "code",
            "label": "${cvvLabel}",
            "value": "${code}",
            "style": "password"
        },
        {
            "type": "Input.Text",
            "id": "notes",
            "label": "${notesLabel}",
            "value": "${notes}",
            "isMultiline": true
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "${createButton}",
            "style": "positive",
            "data": {
                "action": "create"
            }
        }
    ]
}
""";
    }

    private static string GetIdentityTemplate(BitwardenFolder[]? folders)
    {
        var folderChoiceSet = GetFolderChoiceSetJson(folders);
        return $$"""
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
            "type": "Input.Text",
            "id": "name",
            "label": "${nameLabel}",
            "value": "${name}",
            "isRequired": true,
            "errorMessage": "${nameRequired}"
        },
        {{folderChoiceSet}},
        {
            "type": "ColumnSet",
            "columns": [
                {
                    "type": "Column",
                    "width": "stretch",
                    "items": [
                        {
                            "type": "Input.Text",
                            "id": "firstName",
                            "label": "${firstNameLabel}",
                            "value": "${firstName}"
                        }
                    ]
                },
                {
                    "type": "Column",
                    "width": "stretch",
                    "items": [
                        {
                            "type": "Input.Text",
                            "id": "lastName",
                            "label": "${lastNameLabel}",
                            "value": "${lastName}"
                        }
                    ]
                }
            ]
        },
        {
            "type": "Input.Text",
            "id": "email",
            "label": "${emailLabel}",
            "value": "${email}",
            "style": "email"
        },
        {
            "type": "Input.Text",
            "id": "phone",
            "label": "${phoneLabel}",
            "value": "${phone}",
            "style": "tel"
        },
        {
            "type": "Input.Text",
            "id": "company",
            "label": "${companyLabel}",
            "value": "${company}"
        },
        {
            "type": "Input.Text",
            "id": "address1",
            "label": "${addressLabel}",
            "value": "${address1}"
        },
        {
            "type": "ColumnSet",
            "columns": [
                {
                    "type": "Column",
                    "width": "stretch",
                    "items": [
                        {
                            "type": "Input.Text",
                            "id": "city",
                            "label": "${cityLabel}",
                            "value": "${city}"
                        }
                    ]
                },
                {
                    "type": "Column",
                    "width": "stretch",
                    "items": [
                        {
                            "type": "Input.Text",
                            "id": "postalCode",
                            "label": "${postalCodeLabel}",
                            "value": "${postalCode}"
                        }
                    ]
                }
            ]
        },
        {
            "type": "Input.Text",
            "id": "notes",
            "label": "${notesLabel}",
            "value": "${notes}",
            "isMultiline": true
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "${createButton}",
            "style": "positive",
            "data": {
                "action": "create"
            }
        }
    ]
}
""";
    }

    private static string GetSecureNoteTemplate(BitwardenFolder[]? folders)
    {
        var folderChoiceSet = GetFolderChoiceSetJson(folders);
        return $$"""
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
            "type": "Input.Text",
            "id": "name",
            "label": "${nameLabel}",
            "value": "${name}",
            "isRequired": true,
            "errorMessage": "${nameRequired}"
        },
        {{folderChoiceSet}},
        {
            "type": "Input.Text",
            "id": "notes",
            "label": "${notesLabel}",
            "value": "${notes}",
            "isMultiline": true
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "${createButton}",
            "style": "positive",
            "data": {
                "action": "create"
            }
        }
    ]
}
""";
    }

    private static string GetDataForNewItem(BitwardenItemType itemType)
    {
        var data = new JsonObject
        {
            ["title"] = GetFormTitleForItemType(itemType),
            ["nameLabel"] = ResourceHelper.EditItemNameLabel,
            ["name"] = "",
            ["nameRequired"] = ResourceHelper.EditItemNameRequired,
            ["folderLabel"] = ResourceHelper.CreateItemFolderLabel,
            ["notesLabel"] = ResourceHelper.EditItemNotesLabel,
            ["notes"] = "",
            ["createButton"] = ResourceHelper.CreateItemButton
        };

        switch (itemType)
        {
            case BitwardenItemType.Login:
                data["usernameLabel"] = ResourceHelper.EditItemUsernameLabel;
                data["username"] = "";
                data["passwordLabel"] = ResourceHelper.EditItemPasswordLabel;
                data["password"] = "";
                data["urlLabel"] = ResourceHelper.EditItemUrlLabel;
                data["url"] = "";
                data["totpLabel"] = ResourceHelper.EditItemTotpLabel;
                data["totp"] = "";
                break;

            case BitwardenItemType.Card:
                data["cardholderNameLabel"] = ResourceHelper.EditItemCardholderNameLabel;
                data["cardholderName"] = "";
                data["cardNumberLabel"] = ResourceHelper.EditItemCardNumberLabel;
                data["number"] = "";
                data["expMonthLabel"] = ResourceHelper.EditItemExpMonthLabel;
                data["expMonth"] = "";
                data["expYearLabel"] = ResourceHelper.EditItemExpYearLabel;
                data["expYear"] = "";
                data["cvvLabel"] = ResourceHelper.EditItemCvvLabel;
                data["code"] = "";
                break;

            case BitwardenItemType.Identity:
                data["firstNameLabel"] = ResourceHelper.EditItemFirstNameLabel;
                data["firstName"] = "";
                data["lastNameLabel"] = ResourceHelper.EditItemLastNameLabel;
                data["lastName"] = "";
                data["emailLabel"] = ResourceHelper.EditItemEmailLabel;
                data["email"] = "";
                data["phoneLabel"] = ResourceHelper.EditItemPhoneLabel;
                data["phone"] = "";
                data["companyLabel"] = ResourceHelper.EditItemCompanyLabel;
                data["company"] = "";
                data["addressLabel"] = ResourceHelper.EditItemAddressLabel;
                data["address1"] = "";
                data["cityLabel"] = ResourceHelper.EditItemCityLabel;
                data["city"] = "";
                data["postalCodeLabel"] = ResourceHelper.EditItemPostalCodeLabel;
                data["postalCode"] = "";
                break;
        }

        return data.ToJsonString();
    }

    private static string GetFormTitleForItemType(BitwardenItemType itemType)
    {
        return itemType switch
        {
            BitwardenItemType.Login => ResourceHelper.CreateItemTitleLogin,
            BitwardenItemType.Card => ResourceHelper.CreateItemTitleCard,
            BitwardenItemType.Identity => ResourceHelper.CreateItemTitleIdentity,
            BitwardenItemType.SecureNote => ResourceHelper.CreateItemTitleNote,
            _ => ResourceHelper.CreateItemPageTitle
        };
    }

    public override ICommandResult SubmitForm(string payload, string data)
    {
        try
        {
            var formData = JsonNode.Parse(payload)?.AsObject();
            if (formData == null)
            {
                return CommandResult.GoBack();
            }

            // Parse action data
            var actionData = JsonNode.Parse(data)?.AsObject();
            var action = actionData?["action"]?.GetValue<string>();

            if (action != "create")
            {
                return CommandResult.GoBack();
            }

            // Get the item name (required)
            var name = formData["name"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(name))
            {
                return CommandResult.ShowToast(ResourceHelper.EditItemNameRequired);
            }

            // Get the folder ID (optional)
            var folderId = formData["folderId"]?.GetValue<string>();

            // Build the create payload based on item type
            var createResult = _itemType switch
            {
                BitwardenItemType.Login => CreateLoginItem(formData, name, folderId),
                BitwardenItemType.Card => CreateCardItem(formData, name, folderId),
                BitwardenItemType.Identity => CreateIdentityItem(formData, name, folderId),
                BitwardenItemType.SecureNote => CreateSecureNoteItem(formData, name, folderId),
                _ => Task.FromResult(false)
            };

            if (createResult.Result)
            {
                _onCreated?.Invoke();
                return CommandResult.ShowToast(ResourceHelper.CreateItemSuccess);
            }
            else
            {
                return CommandResult.ShowToast(ResourceHelper.CreateItemFailed);
            }
        }
        catch (Exception ex)
        {
            return CommandResult.ShowToast($"{ResourceHelper.CreateItemFailed}: {ex.Message}");
        }
    }

    private static Task<bool> CreateLoginItem(JsonObject formData, string name, string? folderId)
    {
        var username = formData["username"]?.GetValue<string>() ?? "";
        var password = formData["password"]?.GetValue<string>() ?? "";
        var url = formData["url"]?.GetValue<string>() ?? "";
        var totp = formData["totp"]?.GetValue<string>() ?? "";
        var notes = formData["notes"]?.GetValue<string>() ?? "";

        // Build the new item JSON
        var urisArray = new JsonArray();
        if (!string.IsNullOrEmpty(url))
        {
            urisArray.Add(JsonNode.Parse($"{{\"uri\":\"{url}\"}}"));
        }

        var newItem = new JsonObject
        {
            ["type"] = (int)BitwardenItemType.Login,
            ["name"] = name,
            ["folderId"] = string.IsNullOrEmpty(folderId) ? null : folderId,
            ["notes"] = string.IsNullOrEmpty(notes) ? null : notes,
            ["login"] = new JsonObject
            {
                ["username"] = string.IsNullOrEmpty(username) ? null : username,
                ["password"] = string.IsNullOrEmpty(password) ? null : password,
                ["totp"] = string.IsNullOrEmpty(totp) ? null : totp,
                ["uris"] = urisArray
            }
        };

        return BitwardenCliService.CreateItemAsync(newItem);
    }

    private static Task<bool> CreateCardItem(JsonObject formData, string name, string? folderId)
    {
        var cardholderName = formData["cardholderName"]?.GetValue<string>() ?? "";
        var number = formData["number"]?.GetValue<string>() ?? "";
        var expMonth = formData["expMonth"]?.GetValue<string>() ?? "";
        var expYear = formData["expYear"]?.GetValue<string>() ?? "";
        var code = formData["code"]?.GetValue<string>() ?? "";
        var notes = formData["notes"]?.GetValue<string>() ?? "";

        var newItem = new JsonObject
        {
            ["type"] = (int)BitwardenItemType.Card,
            ["name"] = name,
            ["folderId"] = string.IsNullOrEmpty(folderId) ? null : folderId,
            ["notes"] = string.IsNullOrEmpty(notes) ? null : notes,
            ["card"] = new JsonObject
            {
                ["cardholderName"] = string.IsNullOrEmpty(cardholderName) ? null : cardholderName,
                ["number"] = string.IsNullOrEmpty(number) ? null : number,
                ["expMonth"] = string.IsNullOrEmpty(expMonth) ? null : expMonth,
                ["expYear"] = string.IsNullOrEmpty(expYear) ? null : expYear,
                ["code"] = string.IsNullOrEmpty(code) ? null : code
            }
        };

        return BitwardenCliService.CreateItemAsync(newItem);
    }

    private static Task<bool> CreateIdentityItem(JsonObject formData, string name, string? folderId)
    {
        var firstName = formData["firstName"]?.GetValue<string>() ?? "";
        var lastName = formData["lastName"]?.GetValue<string>() ?? "";
        var email = formData["email"]?.GetValue<string>() ?? "";
        var phone = formData["phone"]?.GetValue<string>() ?? "";
        var company = formData["company"]?.GetValue<string>() ?? "";
        var address1 = formData["address1"]?.GetValue<string>() ?? "";
        var city = formData["city"]?.GetValue<string>() ?? "";
        var postalCode = formData["postalCode"]?.GetValue<string>() ?? "";
        var notes = formData["notes"]?.GetValue<string>() ?? "";

        var newItem = new JsonObject
        {
            ["type"] = (int)BitwardenItemType.Identity,
            ["name"] = name,
            ["folderId"] = string.IsNullOrEmpty(folderId) ? null : folderId,
            ["notes"] = string.IsNullOrEmpty(notes) ? null : notes,
            ["identity"] = new JsonObject
            {
                ["firstName"] = string.IsNullOrEmpty(firstName) ? null : firstName,
                ["lastName"] = string.IsNullOrEmpty(lastName) ? null : lastName,
                ["email"] = string.IsNullOrEmpty(email) ? null : email,
                ["phone"] = string.IsNullOrEmpty(phone) ? null : phone,
                ["company"] = string.IsNullOrEmpty(company) ? null : company,
                ["address1"] = string.IsNullOrEmpty(address1) ? null : address1,
                ["city"] = string.IsNullOrEmpty(city) ? null : city,
                ["postalCode"] = string.IsNullOrEmpty(postalCode) ? null : postalCode
            }
        };

        return BitwardenCliService.CreateItemAsync(newItem);
    }

    private static Task<bool> CreateSecureNoteItem(JsonObject formData, string name, string? folderId)
    {
        var notes = formData["notes"]?.GetValue<string>() ?? "";

        var newItem = new JsonObject
        {
            ["type"] = (int)BitwardenItemType.SecureNote,
            ["name"] = name,
            ["folderId"] = string.IsNullOrEmpty(folderId) ? null : folderId,
            ["notes"] = string.IsNullOrEmpty(notes) ? null : notes,
            ["secureNote"] = new JsonObject
            {
                ["type"] = 0
            }
        };

        return BitwardenCliService.CreateItemAsync(newItem);
    }
}

/// <summary>
/// Page for creating a new folder using Adaptive Cards
/// </summary>
internal sealed partial class CreateFolderPage : ContentPage
{
    private readonly CreateFolderForm _createForm;

    public CreateFolderPage(Action? onCreated = null)
    {
        _createForm = new CreateFolderForm(onCreated);
        Icon = new IconInfo("\uE8B7"); // Folder icon
        Name = ResourceHelper.CreateFolderTitle;
        Title = ResourceHelper.CreateFolderTitle;
    }

    public override IContent[] GetContent() => [_createForm];
}

/// <summary>
/// Form content for creating a new folder
/// </summary>
internal sealed partial class CreateFolderForm : FormContent
{
    private readonly Action? _onCreated;

    public CreateFolderForm(Action? onCreated = null)
    {
        _onCreated = onCreated;

        TemplateJson = GetFolderTemplate();
        DataJson = GetFolderData();
    }

    private static string GetFolderTemplate()
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
            "type": "Input.Text",
            "id": "folderName",
            "label": "${folderNameLabel}",
            "value": "",
            "isRequired": true,
            "errorMessage": "${folderNameRequired}"
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "${createButton}",
            "style": "positive",
            "data": {
                "action": "create"
            }
        }
    ]
}
""";
    }

    private static string GetFolderData()
    {
        var data = new JsonObject
        {
            ["title"] = ResourceHelper.CreateFolderTitle,
            ["folderNameLabel"] = ResourceHelper.CreateFolderNameLabel,
            ["folderNameRequired"] = ResourceHelper.CreateFolderNameRequired,
            ["createButton"] = ResourceHelper.CreateItemButton
        };

        return data.ToJsonString();
    }

    public override ICommandResult SubmitForm(string payload, string data)
    {
        try
        {
            var formData = JsonNode.Parse(payload)?.AsObject();
            if (formData == null)
            {
                return CommandResult.GoBack();
            }

            // Parse action data
            var actionData = JsonNode.Parse(data)?.AsObject();
            var action = actionData?["action"]?.GetValue<string>();

            if (action != "create")
            {
                return CommandResult.GoBack();
            }

            // Get the folder name (required)
            var folderName = formData["folderName"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(folderName))
            {
                return CommandResult.ShowToast(ResourceHelper.CreateFolderNameRequired);
            }

            // Create the folder
            var createResult = BitwardenCliService.CreateFolderAsync(folderName).Result;

            if (createResult)
            {
                _onCreated?.Invoke();
                return CommandResult.ShowToast(ResourceHelper.CreateFolderSuccess);
            }
            else
            {
                return CommandResult.ShowToast(ResourceHelper.CreateFolderFailed);
            }
        }
        catch (Exception ex)
        {
            return CommandResult.ShowToast($"{ResourceHelper.CreateFolderFailed}: {ex.Message}");
        }
    }
}
