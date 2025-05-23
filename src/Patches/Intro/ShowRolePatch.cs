using HarmonyLib;
using Lotus.Roles;
using Lotus.Utilities;
using Lotus.Extensions;
using Lotus.Roles.Legacy;
using VentLib.Utilities;

namespace Lotus.Patches.Intro;

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
class ShowRolePatch
{
    public static void Postfix(IntroCutscene __instance)
    {
        Async.Schedule(() =>
        {
            CustomRole role = PlayerControl.LocalPlayer.GetCustomRole();
            if (!role.IsVanilla())
            {
                __instance.YouAreText.color = role.RoleColor;
                __instance.RoleText.text = role.RoleName;
                __instance.RoleText.color = role.RoleColor;
                __instance.RoleBlurbText.color = role.RoleColor;

                __instance.RoleBlurbText.text = PlayerControl.LocalPlayer.GetCustomRole().Blurb;
            }

            __instance.RoleText.text += Utils.GetSubRolesText(PlayerControl.LocalPlayer.PlayerId);

        }, 0.01f);

    }
}