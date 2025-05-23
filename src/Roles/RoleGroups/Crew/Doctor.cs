using Lotus.API.Odyssey;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Managers.History.Events;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using UnityEngine;
using VentLib.Options.Game;
using VentLib.Utilities;

namespace Lotus.Roles.RoleGroups.Crew;

public class Doctor : Scientist
{
    [RoleAction(RoleActionType.AnyDeath)]
    private void DoctorAnyDeath(PlayerControl dead, IDeathEvent causeOfDeath)
    {
        string coloredString = "<size=1.6>" + Color.white.Colorize($"({RoleColor.Colorize(causeOfDeath.SimpleName())})") + "</size>";
        
        TextComponent textComponent = new(new LiveString(coloredString), GameState.InMeeting, viewers: MyPlayer);
        
        dead.NameModel().GetComponentHolder<TextHolder>().Add(textComponent);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) => 
        AddVitalsOptions(base.RegisterOptions(optionStream));

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.5f, 1f, 0.87f));
}