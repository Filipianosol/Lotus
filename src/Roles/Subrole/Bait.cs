using TOHTOR.GUI;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Utilities;

namespace TOHTOR.Roles.Subrole;

public class Bait: Subrole
{
    [DynElement(UI.Subrole)]
    private string SubroleIndicator() => RoleColor.Colorize("★");

    [RoleAction(RoleActionType.MyDeath)]
    private void BaitDies(PlayerControl killer) => killer.ReportDeadBody(MyPlayer.Data);

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0f, 0.7f, 0.7f));

}