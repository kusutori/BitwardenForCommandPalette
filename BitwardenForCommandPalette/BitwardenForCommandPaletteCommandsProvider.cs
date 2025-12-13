// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette;

public partial class BitwardenForCommandPaletteCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public BitwardenForCommandPaletteCommandsProvider()
    {
        DisplayName = "Bitwarden For Command Palette";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        _commands = [
            new CommandItem(new BitwardenForCommandPalettePage()) { Title = DisplayName },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
