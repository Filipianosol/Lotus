using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.API;
using Lotus.Extensions;
using Lotus.GUI;
using UnityEngine;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace Lotus.Roles.RoleGroups.Impostors;

public class YingYanger : Vanilla.Impostor
{
    private DateTime lastCheck = DateTime.Now;
    private List<PlayerControl> cursedPlayers;
    private Dictionary<byte, Remote<IndicatorComponent>> remotes;

    private float YingYangCD;
    private bool ResetToYingYang;
    private bool InYingMode;

    protected override void Setup(PlayerControl player) => cursedPlayers = new List<PlayerControl>();
    protected override void PostSetup() => remotes = new Dictionary<byte, Remote<IndicatorComponent>>();

    [RoleAction(RoleActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        if (MyPlayer.InteractWith(target, DirectInteraction.HostileInteraction.Create(this)) is InteractionResult.Halt) return false;
        if (!InYingMode) return false;

        cursedPlayers.Add(target);
        remotes.GetValueOrDefault(target.PlayerId)?.Delete();
        IndicatorComponent component = new(new LiveString("◆", new Color(0.36f, 0f, 0.58f)), GameState.Roaming, viewers: MyPlayer);
        remotes[target.PlayerId] = target.NameModel().GetComponentHolder<IndicatorHolder>().Add(component);
        MyPlayer.RpcGuardAndKill(target);

        if (cursedPlayers.Count >= 2) InYingMode = false;
        return true;
    }

    [RoleAction(RoleActionType.RoundStart)]
    private void RoundStart() => InYingMode = true;

    [RoleAction(RoleActionType.RoundEnd)]
    private void RoundEnd() => cursedPlayers.Clear();

    [RoleAction(RoleActionType.FixedUpdate)]
    private void YingYangerKillCheck()
    {
        double elapsed = (DateTime.Now - lastCheck).TotalSeconds;
        if (elapsed < ModConstants.RoleFixedUpdateCooldown) return;
        lastCheck = DateTime.Now;
        foreach (PlayerControl player in new List<PlayerControl>(cursedPlayers))
        {
            if (!player.IsAlive())
            {
                RemovePuppet(player);
                continue;
            }
            List<PlayerControl> inRangePlayers = player.GetPlayersInAbilityRangeSorted().Where(p => p.Relationship(MyPlayer) is not Relation.FullAllies && cursedPlayers.Contains(p)).ToList();
            if (inRangePlayers.Count == 0) continue;
            player.RpcMurderPlayer(inRangePlayers.GetRandom());
            RemovePuppet(player);
        }
        cursedPlayers.Where(p => p.Data.IsDead).ToArray().Do(RemovePuppet);
        if (cursedPlayers.Count <= 2 && !InYingMode && ResetToYingYang) InYingMode = true;
    }

    private void RemovePuppet(PlayerControl puppet)
    {
        remotes.GetValueOrDefault(puppet.PlayerId)?.Delete();
        cursedPlayers.Remove(puppet);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .SubOption(sub => sub
            .Name("Ying Yang Cooldown")
            .BindFloat(v => YingYangCD = v)
            .AddFloatRange(2.5f, 180, 2.5f, 5, "s")
            .Build())
        .SubOption(sub => sub
            .Name("Reset to Ying Yang on Target Death")
            .BindBool(v => ResetToYingYang = v)
            .AddOnOffValues()
            .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(new IndirectKillCooldown(KillCooldown, () => InYingMode));
}