using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Managers;
using Lotus.Options;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using static Lotus.Roles.RoleGroups.Crew.Investigator.Translations.Options;

namespace Lotus.Roles.RoleGroups.Crew;

public class Investigator : Crewmate
{
    public static List<(Func<CustomRole, bool> predicate, GameOptionBuilder builder, bool allColored)> RoleTypeBuilders = new()
    {
        (r => r.SpecialType is SpecialType.NeutralKilling, new GameOptionBuilder()
            .KeyName("Neutral Killing Are Red", TranslationUtil.Colorize(NeutralKillingRed, Color.red, ModConstants.Palette.NeutralColor, ModConstants.Palette.KillingColor))
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(0).Color(Color.red).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(1).Color(Color.green).Build())
            .Value(v => v.Text(GeneralOptionTranslations.CustomText).Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build())
            .ShowSubOptionPredicate(i => (int)i == 2), false),
        (r => r.SpecialType is SpecialType.Neutral, new GameOptionBuilder()
            .KeyName("Neutral Passive Are Red", TranslationUtil.Colorize(NeutralPassiveRed, Color.red, ModConstants.Palette.NeutralColor, ModConstants.Palette.PassiveColor))
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(0).Color(Color.red).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(1).Color(Color.green).Build())
            .Value(v => v.Text(GeneralOptionTranslations.CustomText).Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build())
            .ShowSubOptionPredicate(i => (int)i == 2), false),
        (r => r.Faction is Factions.Impostors.Madmates, new GameOptionBuilder()
            .KeyName("Madmates Are Red", TranslationUtil.Colorize(MadmateRed, Color.red, ModConstants.Palette.MadmateColor))
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(0).Color(Color.red).Build())
            .Value(v => v.Text(GeneralOptionTranslations.AllText).Value(1).Color(Color.green).Build())
            .Value(v => v.Text(GeneralOptionTranslations.CustomText).Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build())
            .ShowSubOptionPredicate(i => (int)i == 2), false)
    };

    // 2 = Color red, 1 = Color green
    public static Dictionary<Type, int> RoleColoringDictionary = new();
    [NewOnSetup] private List<byte> investigated = null!;

    [UIComponent(UI.Cooldown)]
    private Cooldown abilityCooldown = null!;

    public Investigator()
    {
        CustomRoleManager.AddOnFinishCall(PopulateInvestigatorOptions);
    }

    [RoleAction(RoleActionType.OnPet)]
    private void Investigate()
    {
        if (abilityCooldown.NotReady()) return;
        List<PlayerControl> players = MyPlayer.GetPlayersInAbilityRangeSorted().Where(p => !investigated.Contains(p.PlayerId)).ToList();
        if (players.Count == 0) return;

        abilityCooldown.Start();
        PlayerControl player = players[0];
        if (MyPlayer.InteractWith(player, DirectInteraction.NeutralInteraction.Create(this)) is InteractionResult.Halt) return;

        investigated.Add(player.PlayerId);
        CustomRole role = player.GetCustomRole();

        int setting = RoleTypeBuilders.FirstOrOptional(rtb => rtb.predicate(role)).Map(rtb => rtb.allColored ? 2 : 1).OrElse(0);
        if (setting == 0) setting = RoleColoringDictionary.GetValueOrDefault(role.GetType(), 1);

        Color color = setting == 2 ? Color.green : Color.red;

        NameComponent nameComponent = new(new LiveString(player.name, color), GameStates.IgnStates, ViewMode.Replace, MyPlayer);
        player.NameModel().GetComponentHolder<NameHolder>().Add(nameComponent);
    }

    // This is the most complicated options because of all the individual settings
    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Investigate Cooldown", InvestigateCooldown)
                .BindFloat(abilityCooldown.SetDuration)
                .AddFloatRange(2.5f, 120, 2.5f, 10, GeneralOptionTranslations.SecondsSuffix)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(1f, 0.79f, 0.51f));

    private void PopulateInvestigatorOptions()
    {
        CustomRoleManager.AllRoles.ForEach(r =>
        {
            RoleTypeBuilders.FirstOrOptional(b => b.predicate(r)).Map(i => i.builder)
                .IfPresent(builder =>
                {
                    builder.SubOption(sub => sub.KeyName(r.EnglishRoleName, r.RoleColor.Colorize(r.RoleName))
                        .AddEnableDisabledValues()
                        .BindBool(b =>
                        {
                            if (b) RoleColoringDictionary[r.GetType()] = 2;
                            else RoleColoringDictionary[r.GetType()] = 1;
                        })
                        .Build());
                });
        });
        RoleTypeBuilders.ForEach(rtb =>
        {
            rtb.builder.BindInt(i => rtb.allColored = i == 1);
            Option option = rtb.builder.Build();
            RoleOptions.AddChild(option);
            CustomRoleManager.RoleOptionManager.Register(option, OptionLoadMode.LoadOrCreate);
        });
    }

    [Localized(nameof(Investigator))]
    internal static class Translations
    {
        [Localized(ModConstants.Options)]
        internal static class Options
        {
            [Localized(nameof(InvestigateCooldown))]
            public static string InvestigateCooldown = "Investigate Cooldown";

            [Localized(nameof(NeutralKillingRed))]
            public static string NeutralKillingRed = "Neutral::1 Killing::2 Are Red::0";

            [Localized(nameof(NeutralPassiveRed))]
            public static string NeutralPassiveRed = "Neutral::1 Passive::2 Are Red::0";

            [Localized(nameof(MadmateRed))]
            public static string MadmateRed = "Madmates::1 Are Red::0";
        }
    }

}