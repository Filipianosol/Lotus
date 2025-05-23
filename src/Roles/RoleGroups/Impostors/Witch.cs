using System.Collections.Generic;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Extensions;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.Roles.RoleGroups.Impostors;

public class Witch: Vanilla.Impostor
{
    private static Color cursingColor = new(0.37f, 0.74f, 0.35f);
    private bool freelySwitchModes;
    private bool switchModesAfterAttack;
    
    [NewOnSetup] private Dictionary<byte, Remote<IndicatorComponent>> indicators;
    [NewOnSetup] private HashSet<byte> cursedPlayers;

    private bool isCursingMode = true;
    
    [UIComponent(UI.Text)]
    private string ModeDisplay() => freelySwitchModes ? isCursingMode ? cursingColor.Colorize(Translations.CursingModeText) : Color.red.Colorize(Translations.KillingModeText) : "";

    [RoleAction(RoleActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        if (!isCursingMode)
        {
            if (switchModesAfterAttack) isCursingMode = !isCursingMode;
            return base.TryKill(target);
        }
        
        MyPlayer.RpcMark(target);
        if (switchModesAfterAttack) isCursingMode = !isCursingMode;
        if (MyPlayer.InteractWith(target, DirectInteraction.HostileInteraction.Create(this)) is InteractionResult.Halt) return false;
        if (cursedPlayers.Contains(target.PlayerId)) return false;
        
        cursedPlayers.Add(target.PlayerId);
        indicators.GetValueOrDefault(target.PlayerId)?.Delete();
        indicators[target.PlayerId] = target.NameModel().GCH<IndicatorHolder>().Add(new SimpleIndicatorComponent("†", Color.red, GameState.InMeeting));

        string eventMessage = TranslationUtil.Colorize(Translations.CursedMessage.Formatted(MyPlayer.name, target.name), RoleColor, target.GetCustomRole().RoleColor);
        Game.MatchData.GameHistory.AddEvent(new GenericTargetedEvent(MyPlayer, target, eventMessage));
        return false;
    }

    [RoleAction(RoleActionType.OnPet)]
    public void SwitchWitchMode()
    {
        if (freelySwitchModes) isCursingMode = !isCursingMode;
    }

    [RoleAction(RoleActionType.MeetingEnd)]
    public void KillCursedPlayers(Optional<GameData.PlayerInfo> exiledPlayer)
    {
        if (exiledPlayer.Compare(p => p.PlayerId == MyPlayer.PlayerId)) return;
        cursedPlayers.Filter(Players.PlayerById).ForEach(p => ProtectedRpc.CheckMurder(MyPlayer, p));
        cursedPlayers.Clear();
        indicators.ForEach(i => i.Value.Delete());
        indicators.Clear();
    }


    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) => 
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Freely Switch Modes", Translations.Options.FreelySwitchModes)
                .AddOnOffValues()
                .BindBool(b => freelySwitchModes = b)
                .Build())
            .SubOption(sub => sub.KeyName("Switch Modes After Attack", Translations.Options.SwitchModesAfterAttack)
                .AddOnOffValues()
                .BindBool(b => switchModesAfterAttack = b)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) => 
        base.Modify(roleModifier)
            .OptionOverride(new IndirectKillCooldown(KillCooldown, () => isCursingMode));
    

    [Localized(nameof(Witch))]
    private static class Translations
    {
        [Localized(nameof(CursedMessage))]
        public static string CursedMessage = "{0}::0 cursed {1}::1 to die at the end of next meeting.";

        [Localized(nameof(CursingModeText))]
        public static string CursingModeText = "Cursing";

        [Localized(nameof(KillingModeText))]
        public static string KillingModeText = "Killing";
        
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(FreelySwitchModes))]
            public static string FreelySwitchModes = "Freely Switch Modes";

            [Localized(nameof(SwitchModesAfterAttack))]
            public static string SwitchModesAfterAttack = "Switch Modes After Attack";
        }
    }
}