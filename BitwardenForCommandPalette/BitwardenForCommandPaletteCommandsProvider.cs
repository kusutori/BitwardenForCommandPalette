// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BitwardenForCommandPalette.Helpers;
using BitwardenForCommandPalette.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette;

public partial class BitwardenForCommandPaletteCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly Settings _settings = new();

    public BitwardenForCommandPaletteCommandsProvider()
    {
        DisplayName = ResourceHelper.AppDisplayName;
        Icon = IconHelpers.FromRelativePath("Assets\\Square44x44Logo.targetsize-24_altform-unplated.png");
        
        InitializeSettings();
        
        _commands = [
            new CommandItem(new BitwardenForCommandPalettePage()) { Title = DisplayName, Icon = Icon },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

    private void InitializeSettings()
    {
        var settingsManager = SettingsManager.Instance;

        // Bitwarden CLI path setting
        var bwPathSetting = new TextSetting(
            "BwPath",
            ResourceHelper.SettingsBwPathLabel,
            ResourceHelper.SettingsBwPathDescription,
            settingsManager.BwPath
        );

        // Client ID setting
        var clientIdSetting = new TextSetting(
            "ClientId",
            ResourceHelper.SettingsBwClientIdLabel,
            ResourceHelper.SettingsBwClientIdDescription,
            settingsManager.BwClientId
        );

        // Client Secret setting
        var clientSecretSetting = new TextSetting(
            "ClientSecret",
            ResourceHelper.SettingsBwClientSecretLabel,
            ResourceHelper.SettingsBwClientSecretDescription,
            settingsManager.BwClientSecret
        );

        // Custom environment variables setting  
        var customEnvSetting = new TextSetting(
            "CustomEnv",
            ResourceHelper.SettingsCustomEnvLabel,
            ResourceHelper.SettingsCustomEnvDescription,
            settingsManager.CustomEnvironment
        )
        {
            Multiline = true
        };

        _settings.Add(bwPathSetting);
        _settings.Add(clientIdSetting);
        _settings.Add(clientSecretSetting);
        _settings.Add(customEnvSetting);

        // Handle settings changes
        _settings.SettingsChanged += (sender, args) =>
        {
            settingsManager.BwPath = _settings.GetSetting<string>("BwPath") ?? "bw";
            settingsManager.BwClientId = _settings.GetSetting<string>("ClientId") ?? string.Empty;
            settingsManager.BwClientSecret = _settings.GetSetting<string>("ClientSecret") ?? string.Empty;
            settingsManager.CustomEnvironment = _settings.GetSetting<string>("CustomEnv") ?? string.Empty;
        };

        // Set the Settings property for CommandProvider
        Settings = _settings;
    }
}
