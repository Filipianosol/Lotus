using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.API.Vanilla.Meetings;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Utilities;
using Lotus.Extensions;
using VentLib.Logging;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;
using VentLib.Utilities.Optionals;
using static MeetingHud;

namespace Lotus.Patches.Meetings;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
public class CheckForEndVotingPatch
{
    public static bool Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        MeetingDelegate meetingDelegate = MeetingDelegate.Instance;
        if (!meetingDelegate.IsForceEnd() && __instance.playerStates.Any(ps => !ps.AmDead && !ps.DidVote)) return false;
        VentLogger.Debug("Beginning End Voting", "CheckEndVotingPatch");

        // Calculate the exiled player once so that we can send the voting complete signal
        VentLogger.Trace($"End Vote Count: {meetingDelegate.CurrentVoteCount().Select(kv => $"{Utils.GetPlayerById(kv.Key).GetNameWithRole()}: {kv.Value}").Join()}");
        meetingDelegate.CalculateExiledPlayer();


        byte exiledPlayer = meetingDelegate.ExiledPlayer?.PlayerId ?? 255;


        ActionHandle handle = ActionHandle.NoInit();
        Game.TriggerForAll(RoleActionType.VotingComplete, ref handle, meetingDelegate);

        // WE DO NOT RECALCULATE THE EXILED PLAYER!
        // This means its up to roles that modify the meeting delegate to properly update the exiled player


        // Generate voter states to reflect voting
        List<VoterState> votingStates = GenerateVoterStates(meetingDelegate);

        List<byte> playerVotes = meetingDelegate.CurrentVotes()
            // Kinda weird logic here, we take the existing List<Optional<>> and filter it to only existing votes
            // Then we filter all votes to only the votes of the exiled player
            // Finally we transform the exiled player votes into the player's playerID
            .SelectMany(kv => kv.Value.Filter().Where(i => i == exiledPlayer).Select(i => kv.Key)).ToList();

        if (meetingDelegate.ExiledPlayer != null)
        {
            PlayerControl p;
            if ((p = meetingDelegate.ExiledPlayer.Object) != null) p.SetName(AUSettings.ConfirmImpostor() ? $"<b>{p.GetCustomRole().RoleName}</b>\n{p.name}" : p.name);
            Hooks.MeetingHooks.ExiledHook.Propagate(new ExiledHookEvent(meetingDelegate.ExiledPlayer, playerVotes));
        }
        __instance.RpcVotingComplete(votingStates.ToArray(), meetingDelegate.ExiledPlayer, meetingDelegate.IsTie);
        return false;
    }

    private static List<VoterState> GenerateVoterStates(MeetingDelegate meetingDelegate)
    {
        List<VoterState> votingStates = new();
        meetingDelegate.CurrentVotes().ForEach(kv =>
        {
            byte playerId = kv.Key;
            Optional<PlayerControl> player = Utils.PlayerById(playerId);
            kv.Value.ForEach(voted =>
            {
                string votedName = voted.FlatMap(b => Utils.PlayerById(b)).Map(p => p.GetNameWithRole()).OrElse("No One");
                player.IfPresent(p => VentLogger.Log(LogLevel.All,$"{p.GetNameWithRole()} voted for {votedName}"));
                votingStates.Add(new VoterState
                {
                    VoterId = playerId,
                    VotedForId = voted.OrElse(253) // Skip vote byte
                });
            });
        });
        return votingStates;
    }

    [QuickPrefix(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
    public static void VotingCompletePatch(MeetingHud __instance, [HarmonyArgument(1)] GameData.PlayerInfo? playerInfo)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MeetingDelegate meetingDelegate = MeetingDelegate.Instance;
        meetingDelegate.ExiledPlayer = playerInfo;

        ActionHandle noCancel = ActionHandle.NoInit();
        Game.TriggerForAll(RoleActionType.MeetingEnd, ref noCancel, Optional<GameData.PlayerInfo>.Of(playerInfo),
            meetingDelegate.IsTie, new Dictionary<byte, int>(meetingDelegate.CurrentVoteCount()), new Dictionary<byte, List<Optional<byte>>>(meetingDelegate.CurrentVotes()));

    }
}