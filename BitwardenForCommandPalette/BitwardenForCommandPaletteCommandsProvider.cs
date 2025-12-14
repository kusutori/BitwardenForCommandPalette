// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BitwardenForCommandPalette.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BitwardenForCommandPalette;

public partial class BitwardenForCommandPaletteCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public BitwardenForCommandPaletteCommandsProvider()
    {
        DisplayName = ResourceHelper.AppDisplayName;
        Icon = IconHelpers.FromRelativePath("Assets\\Square44x44Logo.targetsize-24_altform-unplated.png");
        _commands = [
            new CommandItem(new BitwardenForCommandPalettePage()) { Title = DisplayName, Icon = Icon },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
