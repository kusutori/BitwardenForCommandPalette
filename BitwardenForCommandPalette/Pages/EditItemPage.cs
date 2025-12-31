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
/// Page for editing a vault item using Adaptive Cards
/// </summary>
internal sealed partial class EditItemPage : ContentPage
{
    private readonly EditItemForm _editForm;

    public EditItemPage(BitwardenItem item, Action? onSaved = null)
    {
        _editForm = new EditItemForm(item, onSaved);
        Icon = new IconInfo("\uE70F"); // Edit icon
        Name = ResourceHelper.CommandEditItem;
        Title = ResourceHelper.GetString("EditItemPageTitle", item.Name ?? "");
    }

    public override IContent[] GetContent() => [_editForm];
}

/// <summary>
/// Form content for editing a vault item
/// </summary>
internal sealed partial class EditItemForm : FormContent
{
    private readonly BitwardenItem _item;
    private readonly Action? _onSaved;

    public EditItemForm(BitwardenItem item, Action? onSaved = null)
    {
        _item = item;
        _onSaved = onSaved;

        // Set the template based on item type
        TemplateJson = GetTemplateForItemType(item.ItemType);
        DataJson = GetDataForItem(item);
    }

    private static string GetTemplateForItemType(BitwardenItemType itemType)
    {
        return itemType switch
        {
            BitwardenItemType.Login => GetLoginTemplate(),
            BitwardenItemType.Card => GetCardTemplate(),
            BitwardenItemType.Identity => GetIdentityTemplate(),
            BitwardenItemType.SecureNote => GetSecureNoteTemplate(),
            _ => GetLoginTemplate()
        };
    }

    private static string GetLoginTemplate()
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
            "id": "name",
            "label": "${nameLabel}",
            "value": "${name}",
            "isRequired": true,
            "errorMessage": "${nameRequired}"
        },
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
            "title": "${saveButton}",
            "style": "positive",
            "data": {
                "action": "save"
            }
        }
    ]
}
""";
    }

    private static string GetCardTemplate()
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
            "id": "name",
            "label": "${nameLabel}",
            "value": "${name}",
            "isRequired": true,
            "errorMessage": "${nameRequired}"
        },
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
            "title": "${saveButton}",
            "style": "positive",
            "data": {
                "action": "save"
            }
        }
    ]
}
""";
    }

    private static string GetIdentityTemplate()
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
            "id": "name",
            "label": "${nameLabel}",
            "value": "${name}",
            "isRequired": true,
            "errorMessage": "${nameRequired}"
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
            "title": "${saveButton}",
            "style": "positive",
            "data": {
                "action": "save"
            }
        }
    ]
}
""";
    }

    private static string GetSecureNoteTemplate()
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
            "id": "name",
            "label": "${nameLabel}",
            "value": "${name}",
            "isRequired": true,
            "errorMessage": "${nameRequired}"
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
            "title": "${saveButton}",
            "style": "positive",
            "data": {
                "action": "save"
            }
        }
    ]
}
""";
    }

    private static string GetDataForItem(BitwardenItem item)
    {
        var data = new JsonObject
        {
            ["title"] = ResourceHelper.EditItemPageTitle.Replace("{0}", "").Trim(),
            ["nameLabel"] = ResourceHelper.EditItemNameLabel,
            ["name"] = item.Name ?? "",
            ["nameRequired"] = ResourceHelper.EditItemNameRequired,
            ["notesLabel"] = ResourceHelper.EditItemNotesLabel,
            ["notes"] = item.Notes ?? "",
            ["saveButton"] = ResourceHelper.EditItemSaveButton
        };

        switch (item.ItemType)
        {
            case BitwardenItemType.Login:
                data["usernameLabel"] = ResourceHelper.EditItemUsernameLabel;
                data["username"] = item.Login?.Username ?? "";
                data["passwordLabel"] = ResourceHelper.EditItemPasswordLabel;
                data["password"] = item.Login?.Password ?? "";
                data["urlLabel"] = ResourceHelper.EditItemUrlLabel;
                data["url"] = item.Login?.Uris?.Length > 0 ? item.Login.Uris[0].Uri ?? "" : "";
                data["totpLabel"] = ResourceHelper.EditItemTotpLabel;
                data["totp"] = item.Login?.Totp ?? "";
                break;

            case BitwardenItemType.Card:
                data["cardholderNameLabel"] = ResourceHelper.EditItemCardholderNameLabel;
                data["cardholderName"] = item.Card?.CardholderName ?? "";
                data["cardNumberLabel"] = ResourceHelper.EditItemCardNumberLabel;
                data["number"] = item.Card?.Number ?? "";
                data["expMonthLabel"] = ResourceHelper.EditItemExpMonthLabel;
                data["expMonth"] = item.Card?.ExpMonth ?? "";
                data["expYearLabel"] = ResourceHelper.EditItemExpYearLabel;
                data["expYear"] = item.Card?.ExpYear ?? "";
                data["cvvLabel"] = ResourceHelper.EditItemCvvLabel;
                data["code"] = item.Card?.Code ?? "";
                break;

            case BitwardenItemType.Identity:
                data["firstNameLabel"] = ResourceHelper.EditItemFirstNameLabel;
                data["firstName"] = item.Identity?.FirstName ?? "";
                data["lastNameLabel"] = ResourceHelper.EditItemLastNameLabel;
                data["lastName"] = item.Identity?.LastName ?? "";
                data["emailLabel"] = ResourceHelper.EditItemEmailLabel;
                data["email"] = item.Identity?.Email ?? "";
                data["phoneLabel"] = ResourceHelper.EditItemPhoneLabel;
                data["phone"] = item.Identity?.Phone ?? "";
                data["companyLabel"] = ResourceHelper.EditItemCompanyLabel;
                data["company"] = item.Identity?.Company ?? "";
                data["addressLabel"] = ResourceHelper.EditItemAddressLabel;
                data["address1"] = item.Identity?.Address1 ?? "";
                data["cityLabel"] = ResourceHelper.EditItemCityLabel;
                data["city"] = item.Identity?.City ?? "";
                data["postalCodeLabel"] = ResourceHelper.EditItemPostalCodeLabel;
                data["postalCode"] = item.Identity?.PostalCode ?? "";
                break;
        }

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

            if (action != "save")
            {
                return CommandResult.GoBack();
            }

            // Get the item name (required)
            var name = formData["name"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(name))
            {
                return CommandResult.ShowToast(ResourceHelper.EditItemNameRequired);
            }

            // Build the edit payload based on item type
            var editResult = _item.ItemType switch
            {
                BitwardenItemType.Login => EditLoginItem(formData, name),
                BitwardenItemType.Card => EditCardItem(formData, name),
                BitwardenItemType.Identity => EditIdentityItem(formData, name),
                BitwardenItemType.SecureNote => EditSecureNoteItem(formData, name),
                _ => Task.FromResult(false)
            };

            if (editResult.Result)
            {
                _onSaved?.Invoke();
                return CommandResult.ShowToast(ResourceHelper.EditItemSuccess);
            }
            else
            {
                return CommandResult.ShowToast(ResourceHelper.EditItemFailed);
            }
        }
        catch (Exception ex)
        {
            return CommandResult.ShowToast($"{ResourceHelper.EditItemFailed}: {ex.Message}");
        }
    }

    private Task<bool> EditLoginItem(JsonObject formData, string name)
    {
        var username = formData["username"]?.GetValue<string>() ?? "";
        var password = formData["password"]?.GetValue<string>() ?? "";
        var url = formData["url"]?.GetValue<string>() ?? "";
        var totp = formData["totp"]?.GetValue<string>() ?? "";
        var notes = formData["notes"]?.GetValue<string>() ?? "";

        // Build the updated item JSON
        var urisArray = new JsonArray();
        if (!string.IsNullOrEmpty(url))
        {
            urisArray.Add(JsonNode.Parse($"{{\"uri\":\"{url}\"}}"));
        }

        var updatedItem = new JsonObject
        {
            ["name"] = name,
            ["notes"] = string.IsNullOrEmpty(notes) ? null : notes,
            ["login"] = new JsonObject
            {
                ["username"] = string.IsNullOrEmpty(username) ? null : username,
                ["password"] = string.IsNullOrEmpty(password) ? null : password,
                ["totp"] = string.IsNullOrEmpty(totp) ? null : totp,
                ["uris"] = urisArray
            }
        };

        return BitwardenCliService.EditItemAsync(_item.Id!, updatedItem);
    }

    private Task<bool> EditCardItem(JsonObject formData, string name)
    {
        var cardholderName = formData["cardholderName"]?.GetValue<string>() ?? "";
        var number = formData["number"]?.GetValue<string>() ?? "";
        var expMonth = formData["expMonth"]?.GetValue<string>() ?? "";
        var expYear = formData["expYear"]?.GetValue<string>() ?? "";
        var code = formData["code"]?.GetValue<string>() ?? "";
        var notes = formData["notes"]?.GetValue<string>() ?? "";

        var updatedItem = new JsonObject
        {
            ["name"] = name,
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

        return BitwardenCliService.EditItemAsync(_item.Id!, updatedItem);
    }

    private Task<bool> EditIdentityItem(JsonObject formData, string name)
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

        var updatedItem = new JsonObject
        {
            ["name"] = name,
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

        return BitwardenCliService.EditItemAsync(_item.Id!, updatedItem);
    }

    private Task<bool> EditSecureNoteItem(JsonObject formData, string name)
    {
        var notes = formData["notes"]?.GetValue<string>() ?? "";

        var updatedItem = new JsonObject
        {
            ["name"] = name,
            ["notes"] = string.IsNullOrEmpty(notes) ? null : notes,
            ["secureNote"] = new JsonObject
            {
                ["type"] = 0
            }
        };

        return BitwardenCliService.EditItemAsync(_item.Id!, updatedItem);
    }
}
