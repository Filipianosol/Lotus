using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Managers;
using Lotus.Managers.History;
using Lotus.Roles;
using Lotus.Roles.Overrides;
using Lotus.RPC;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace Lotus.API.Odyssey;

public class MatchData
{
    internal ulong MatchID;

    public GameHistory GameHistory = new();
    public DateTime StartTime = DateTime.Now;

    public Dictionary<ulong, FrozenPlayer> FrozenPlayers = new();
    public VanillaRoleTracker VanillaRoleTracker = new();

    public HashSet<byte> UnreportableBodies = new();
    public int MeetingsCalled;


    public RoleData Roles = new();


    public class RoleData
    {
        public Dictionary<byte, CustomRole> MainRoles = new();
        public Dictionary<byte, List<CustomRole>> SubRoles = new();
        private readonly Dictionary<byte, RemoteList<GameOptionOverride>> rolePersistentOverrides = new();

        public Remote<GameOptionOverride> AddOverride(byte playerId, GameOptionOverride @override)
        {
            return rolePersistentOverrides.GetOrCompute(playerId, () => new RemoteList<GameOptionOverride>()).Add(@override);
        }

        public IEnumerable<GameOptionOverride> GetOverrides(byte playerId)
        {
            return rolePersistentOverrides.GetOrCompute(playerId, () => new RemoteList<GameOptionOverride>());
        }

        public void AddMainRole(byte playerId, CustomRole role) => MainRoles[playerId] = role;
        public void AddSubrole(byte playerId, CustomRole subrole) => SubRoles.GetOrCompute(playerId, () => new List<CustomRole>()).Add(subrole);

        public CustomRole GetMainRole(byte playerId) => MainRoles.GetValueOrDefault(playerId, CustomRoleManager.Default);
        public List<CustomRole> GetSubroles(byte playerId) => SubRoles.GetOrCompute(playerId, () => new List<CustomRole>());

    }


    public static List<CustomRole> GetEnabledRoles() => CustomRoleManager.AllRoles.Where(r => r.IsEnabled()).ToList();

    [ModRPC((uint) ModCalls.SetCustomRole, RpcActors.Host, RpcActors.NonHosts, MethodInvocation.ExecuteBefore)]
    public static void AssignRole(PlayerControl player, CustomRole role, bool sendToClient = false)
    {
        CustomRole assigned = Game.MatchData.Roles.MainRoles[player.PlayerId] = role.Instantiate(player);
        Game.MatchData.FrozenPlayers.GetOptional(player.GetGameID()).IfPresent(fp => fp.Role = assigned);
        if (Game.State is GameState.InLobby or GameState.InIntro) player.GetTeamInfo().MyRole = role.RealRole;
        if (sendToClient) assigned.Assign();
    }

    [ModRPC((uint) ModCalls.SetSubrole, RpcActors.Host, RpcActors.NonHosts, MethodInvocation.ExecuteBefore)]
    public static void AssignSubrole(PlayerControl player, CustomRole role, bool sendToClient = false)
    {
        CustomRole instantiated = role.Instantiate(player);
        Game.MatchData.Roles.AddSubrole(player.PlayerId, instantiated);
        if (sendToClient) role.Assign();
    }
}