extern alias JBAnnotations;
using System.Collections.Generic;
using System.Linq;
using JBAnnotations::JetBrains.Annotations;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Gamemodes.Standard.Lotteries;
using Lotus.Managers;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Roles.Subroles;
using Lotus.Extensions;
using Lotus.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.Gamemodes.Standard;

public class StandardRoleAssignmentLogic
{
    private static List<IAdditionalAssignmentLogic> _additionalAssignmentLogics = new();

    [UsedImplicitly]
    public static void AddAdditionalAssignmentLogic(IAdditionalAssignmentLogic logic) => _additionalAssignmentLogics.Add(logic);

    public static void AssignRoles(List<PlayerControl> allPlayers)
    {
        List<PlayerControl> unassignedPlayers = new(allPlayers);
        unassignedPlayers.Shuffle();
        
        RoleDistribution distribution = GeneralOptions.GameplayOptions.OptimizeRoleAssignment
            ? OptimizeRoleAlgorithm.OptimizeDistribution()
            : OptimizeRoleAlgorithm.NonOptimizedDistribution();


        int j = 0;
        while (GeneralOptions.DebugOptions.NameBasedRoleAssignment && j < unassignedPlayers.Count)
        {
            PlayerControl player = unassignedPlayers[j];
            CustomRole? role = CustomRoleManager.AllRoles.FirstOrDefault(r => r.RoleName.RemoveHtmlTags().ToLower().StartsWith(player.name.ToLower() ?? "HEHXD"));
            if (role != null && role.GetType() != typeof(Crewmate))
            {
                MatchData.AssignRole(player, role);
                unassignedPlayers.Pop(j);
            }
            else j++;
        }

        // ASSIGN IMPOSTOR ROLES
        ImpostorLottery impostorLottery = new();
        int impostorCount = 0;
        while (impostorCount < distribution.Impostors && unassignedPlayers.Count > 0)
        {
            CustomRole role = impostorLottery.Next();
            if (role.GetType() == typeof(Impostor) && impostorLottery.HasNext()) continue;
            MatchData.AssignRole(unassignedPlayers.PopRandom(), IVariableRole.PickAssignedRole(role));
            impostorCount++;
        }

        // =====================

        // ASSIGN NEUTRAL ROLES
        NeutralKillingLottery neutralKillingLottery = new();
        int nkRoles = 0;
        int loops = 0;
        while (unassignedPlayers.Count > 0)
        {
            if (nkRoles >= distribution.MinimumNeutralKilling) break;
            CustomRole role = neutralKillingLottery.Next();
            if (role is IllegalRole)
            {
                if (nkRoles > distribution.MaximumNeutralKilling || loops >= 10) break;
                loops++;
                if (!neutralKillingLottery.HasNext()) 
                    neutralKillingLottery = new NeutralKillingLottery(); // Refresh the lottery again to fulfill the minimum requirement
                continue;
            }
            MatchData.AssignRole(unassignedPlayers.PopRandom(), IVariableRole.PickAssignedRole(role));
            nkRoles++;
        }
        // --------------------------
        
        // ASSIGN NEUTRAL ROLES
        NeutralLottery neutralLottery = new();
        int neutralRoles = 0;
        loops = 0;
        while (unassignedPlayers.Count > 0)
        {
            if (neutralRoles >= distribution.MaximumNeutralPassive) break;
            CustomRole role = neutralLottery.Next();
            if (role is IllegalRole)
            {
                if (neutralRoles > distribution.MinimumNeutralPassive || loops >= 10) break;
                loops++;
                if (!neutralLottery.HasNext()) 
                    neutralLottery = new NeutralLottery(); // Refresh the lottery again to fulfill the minimum requirement
                continue;
            }
            MatchData.AssignRole(unassignedPlayers.PopRandom(), IVariableRole.PickAssignedRole(role));
            neutralRoles++;
        }
        // --------------------------
        
        // DO ADDITIONAL LOGIC (ADDONS)
        foreach (IAdditionalAssignmentLogic additionalAssignmentLogic in _additionalAssignmentLogics)
            additionalAssignmentLogic.AssignRoles(allPlayers, unassignedPlayers);
        // ===================

        // ASSIGN CREWMATE ROLES
        CrewmateLottery crewmateLottery = new();
        while (unassignedPlayers.Count > 0) MatchData.AssignRole(unassignedPlayers.PopRandom(), crewmateLottery.Next());
        // ====================
        
        // ASSIGN SUB-ROLES
        AssignSubroles();
        // ================

        Game.GetAllPlayers().Sorted(p => p.IsHost() ? 0 : 1).ForEach(p => p.GetCustomRole().Assign());
    }

    private static void AssignSubroles()
    {
        SubRoleLottery subRoleLottery = new();

        int evenDistribution = RoleOptions.SubroleOptions.EvenlyDistributeModifiers ? 0 : 9999;

        bool CanAssignTo(PlayerControl player)
        {
            int count = player.GetSubroles().Count;
            if (count > evenDistribution) return false;
            return RoleOptions.SubroleOptions.UncappedModifiers || count < RoleOptions.SubroleOptions.ModifierLimits;
        }
        
        foreach (CustomRole role in subRoleLottery)
        {
            if (role is IllegalRole) continue;
            CustomRole variant = role is Subrole sr ? IVariableSubrole.PickAssignedRole(sr) : IVariableRole.PickAssignedRole(role);
            List<PlayerControl> players = Game.GetAllPlayers().Where(CanAssignTo).ToList();
            if (players.Count == 0)
            {
                evenDistribution++;
                if (!RoleOptions.SubroleOptions.UncappedModifiers && evenDistribution >= RoleOptions.SubroleOptions.ModifierLimits) break;
                players = Game.GetAllPlayers().Where(p => p.GetSubroles().Count <= evenDistribution).ToList();;
                if (players.Count == 0) break;
            }

            bool assigned = false;
            while (players.Count > 0 && !assigned)
            {
                PlayerControl victim = players.PopRandom();
                if (victim.GetSubroles().Any(r => r.GetType() == variant.GetType())) continue;
                if (variant is Subrole subrole)
                {
                    if (!(assigned = subrole.IsAssignableTo(victim))) continue;
                    MatchData.AssignSubrole(victim, subrole);
                }
                else MatchData.AssignRole(victim, variant);
            }
        }
    }
}