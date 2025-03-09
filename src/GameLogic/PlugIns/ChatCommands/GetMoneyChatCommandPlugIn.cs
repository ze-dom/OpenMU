// <copyright file="GetMoneyChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A chat command plugin to get a character's money.
/// </summary>
[Guid("B8E35F57-2ED4-4BAD-9F95-9C88E1B92B0B")]
[PlugIn("Get money command", "Gets money of a player.")]
[ChatCommandHelp(Command, "Gets money of a player. Usage: /getmoney <optional:character>", null)]
public class GetMoneyChatCommandPlugIn : ChatCommandPlugInBase<GetMoneyChatCommandPlugIn.Arguments>, IDisabledByDefault
{
    private const string Command = "/getmoney";
    private const CharacterStatus MinimumStatus = CharacterStatus.GameMaster;
    private const string CharacterNotFoundMessage = "Character '{0}' not found.";
    private const string MoneyGetMessage = "Money of '{0}': {1}.";

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, Arguments arguments)
    {
        var targetPlayer = player;
        if (arguments?.CharacterName != null)
        {
            targetPlayer = player.GameContext.GetPlayerByCharacterName(arguments.CharacterName);
            if (targetPlayer?.SelectedCharacter == null ||
                !targetPlayer.SelectedCharacter.Name.Equals(arguments.CharacterName, StringComparison.OrdinalIgnoreCase))
            {
                await this.ShowMessageToAsync(player, string.Format(CharacterNotFoundMessage, arguments.CharacterName)).ConfigureAwait(false);
                return;
            }
        }

        if (targetPlayer?.SelectedCharacter?.Inventory == null)
        {
            return;
        }

        await this.ShowMessageToAsync(player, string.Format(MoneyGetMessage, targetPlayer.SelectedCharacter.Name, targetPlayer.Money)).ConfigureAwait(false);
    }

    /// <summary>
    /// Arguments for the Get Money chat command.
    /// </summary>
    public class Arguments : ArgumentsBase
    {
        /// <summary>
        /// Gets or sets the character name to get money for.
        /// </summary>
        public string? CharacterName { get; set; }
    }
}