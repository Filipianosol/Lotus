using HarmonyLib;
using Lotus.Utilities;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Logging;

namespace Lotus.Patches.Meetings;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
class MeetingUpdatePatch
{
    public static void Postfix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
            __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
            {
                var player = Utils.GetPlayerById(x.TargetPlayerId);
                player.RpcExileV2();
                VentLogger.High($"Execute: {player.GetNameWithRole()}", "Execution");
            });
    }
}