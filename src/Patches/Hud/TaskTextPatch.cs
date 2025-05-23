using System;
using HarmonyLib;
using Lotus.Extensions;
using Lotus.Roles;
using Lotus.Victory;
using VentLib.Utilities;

namespace Lotus.Patches.Hud;

[HarmonyPatch(typeof(TaskPanelBehaviour), nameof(TaskPanelBehaviour.SetTaskText))]
class TaskTextPatch
{
    private static CustomRole _role = null!;
    private static DateTime _undeferredCheck = DateTime.Now;
    
    // タスク表示の文章が更新・適用された後に実行される
    public static void Postfix(TaskPanelBehaviour __instance)
    {
        if (LobbyBehaviour.Instance != null) return;
        PlayerControl player = PlayerControl.LocalPlayer;
        
        if (CheckEndGamePatch.Deferred) _undeferredCheck = DateTime.Now;

        if (DateTime.Now.Subtract(_undeferredCheck).TotalSeconds > 2) _role = player.GetCustomRole();
        if (ReferenceEquals(_role, null)) return;

        string modifiedText = __instance.taskText.text;
        int impostorTaskIndex = modifiedText.IndexOf(":</color>", StringComparison.Ordinal);
        if (impostorTaskIndex != -1) modifiedText = modifiedText[(9 + impostorTaskIndex)..];
        string roleWithInfo = $"{_role.RoleName}:\r\n";
        roleWithInfo += _role.Blurb + (_role.RealRole.IsImpostor() ? "" : "\r\n");
        __instance.taskText.text = _role.RoleColor.Colorize(roleWithInfo) + modifiedText;
    }
}