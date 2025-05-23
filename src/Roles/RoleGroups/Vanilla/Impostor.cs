using System.Collections.Generic;
using AmongUs.GameOptions;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Stats;
using Lotus.Factions;
using Lotus.Managers.History.Events;
using Lotus.Options;
using Lotus.Roles.Interactions;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using UnityEngine;
using VentLib.Options.Game;

namespace Lotus.Roles.RoleGroups.Vanilla;

public partial class Impostor : CustomRole, IModdable, ISabotagerRole
{
    private const float DefaultFloatValue = -1;

    public virtual bool CanSabotage() => canSabotage && !RoleAbilityFlags.HasFlag(RoleAbilityFlag.CannotSabotage);
    protected bool canSabotage = true;

    public float KillCooldown
    {
        set => killCooldown = value;
        get
        {
            float cooldown = killCooldown ?? AUSettings.KillCooldown();
            return cooldown <= DefaultFloatValue ? AUSettings.KillCooldown() : cooldown;
        }
    }

    public int KillDistance
    {
        set => killDistance = value;
        get
        {
            int distance = killDistance ?? AUSettings.KillDistance();
            return distance < 0 ? AUSettings.KillDistance() : distance;
        }
    }

    private float? killCooldown;
    private int? killDistance;

    [RoleAction(RoleActionType.Attack, Subclassing = false)]
    public virtual bool TryKill(PlayerControl target)
    {
        InteractionResult result = MyPlayer.InteractWith(target, DirectInteraction.FatalInteraction.Create(this));
        Game.MatchData.GameHistory.AddEvent(new KillEvent(MyPlayer, target, result is InteractionResult.Proceed));
        return result is InteractionResult.Proceed;
    }

    protected GameOptionBuilder AddKillCooldownOptions(GameOptionBuilder optionBuilder, string key = "Kill Cooldown", string name = "Kill Cooldown", int defaultIndex = 0)
    {
        return optionBuilder.SubOption(sub => sub.Name(name)
            .Key(key)
            .Value(v => v.Text(GeneralOptionTranslations.GlobalText).Color(new Color(1f, 0.61f, 0.33f)).Value(DefaultFloatValue).Build())
            .AddFloatRange(0, 120, 2.5f, defaultIndex, GeneralOptionTranslations.SecondsSuffix)
            .BindFloat(f => KillCooldown = f)
            .Build());
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .VanillaRole(RoleTypes.Impostor)
            .Faction(FactionInstances.Impostors)
            .CanVent(true)
            .OptionOverride(Override.KillCooldown, () => KillCooldown)
            .RoleColor(Color.red)
            .RoleAbilityFlags(RoleAbilityFlag.IsAbleToKill);

    public override List<Statistic> Statistics() => new() { VanillaStatistics.Kills };
}