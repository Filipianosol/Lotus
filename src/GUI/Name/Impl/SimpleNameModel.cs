using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.GUI.Name.Interfaces;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.RPC;
using UnityEngine;
using VentLib.Logging;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Debug.Profiling;
using VentLib.Utilities.Extensions;

namespace Lotus.GUI.Name.Impl;

public class SimpleNameModel : INameModel
{
    private string unalteredName;
    private int spacing = 0;

    private string cacheString = "";

    public NameHolder NameHolder = new NameHolder();
    public IndicatorHolder IndicatorHolder = new IndicatorHolder();
    public RoleHolder RoleHolder = new RoleHolder(1);
    public SubroleHolder SubroleHolder = new SubroleHolder(1);
    public CounterHolder CounterHolder = new CounterHolder(1);
    public CooldownHolder CooldownHolder = new CooldownHolder(2);
    public TextHolder TextHolder = new TextHolder(2);

    private List<IComponentHolder> componentHolders;
    private PlayerControl player;

    private void SetHolders()
    {
        componentHolders = new()
        {
            NameHolder, IndicatorHolder,
            RoleHolder, SubroleHolder, CounterHolder,
            CooldownHolder, TextHolder
        };
    }

    public SimpleNameModel(PlayerControl player)
    {
        this.player = player;
        SetHolders();
        this.unalteredName = player.name;
        NameHolder.Add(new NameComponent(new LiveString(unalteredName, Color.white), new[] { GameState.Roaming, GameState.InMeeting}));
    }

    public string Unaltered() => unalteredName;

    public PlayerControl MyPlayer() => player;

    public string Render(GameState? state = null, bool sendToPlayer = true, bool force = false) => this.RenderFor(MyPlayer(), state, sendToPlayer, force);

    public string RenderFor(PlayerControl rPlayer, GameState? state = null, bool sendToPlayer = true, bool force = false)
    {
        if (!AmongUsClient.Instance.AmHost) return "";
        uint id = Profilers.Global.Sampler.Start();

        state ??= Game.State;
        List<List<string>> renders = new();
        bool updated = false;
        foreach (IComponentHolder componentHolder in ComponentHolders())
        {
            for (int i = 0; i < componentHolder.Line() + 1 - renders.Count; i++) renders.Add(new List<string>());
            renders[componentHolder.Line()].Add(componentHolder.Render(rPlayer, state.Value));
            updated = updated || componentHolder.Updated(rPlayer.PlayerId);
        }

        if (!updated && !force)
        {
            Profilers.Global.Sampler.Stop(id);
            return cacheString;
        }

        cacheString = renders.Select(s => s.Join(delimiter: " ".Repeat(spacing - 1))).Join(delimiter: "\n").TrimStart('\n').TrimEnd('\n').Replace("\n\n", "\n");
        if (sendToPlayer)
        {
            if (rPlayer.IsHost()) Api.Local.SetName(player, cacheString);
            else
            {
                int clientId = rPlayer.GetClientId();
                if (clientId != -1) RpcV3.Immediate(player.NetId, RpcCalls.SetName).Write(cacheString).Send(clientId);
                if (!player.IsAlive())
                {
                    player.Data.PlayerName = player.name;
                    Players.SendPlayerData(player.Data, clientId, autoSetName: false);
                    DevLogger.Log($"Sending Player Name: {player.Data.PlayerName}");
                    HostRpc.RpcDebug("Hahahahahha");
                }
            }
        }
        Profilers.Global.Sampler.Stop(id);
        return cacheString;
    }

    public List<IComponentHolder> ComponentHolders() => componentHolders;

    public T GetComponentHolder<T>() where T : IComponentHolder
    {
        return (T)componentHolders.First(f => f is T);
    }

    public T GCH<T>() where T : IComponentHolder => GetComponentHolder<T>();
}