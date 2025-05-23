using System.Linq;
using AmongUs.GameOptions;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Options;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.API;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using Convert = System.Convert;

namespace Lotus.Roles.RoleGroups.Impostors;

// TODO: Redo Sniper
public class Sniper: Shapeshifter
{
    private bool preciseShooting = true;
    [UIComponent(UI.Cooldown)]
    private Cooldown loadBulletCooldown;
    private Cooldown killCooldown;

    private int totalBulletCount = 10;
    private int loadedBullets;
    private int maxLoadedBullets;
    private int sniperMode;
    private bool canBeVetted;

    private float realKillCooldown;
    private int currentBulletCount;
    private Vector2 lastDirection;

    [UIComponent(UI.Counter)]
    private string BulletCountCounter() => RoleUtils.Counter(currentBulletCount, totalBulletCount);

    [UIComponent(UI.Indicator)]
    private string LoadedBulletDisplay() => Color.red.Colorize("▫".Repeat(loadedBullets));

    protected override void Setup(PlayerControl player)
    {
        currentBulletCount = totalBulletCount;
        realKillCooldown = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
        killCooldown.Duration = realKillCooldown;
    }

    [RoleAction(RoleActionType.Attack)]
    private bool TryKill(PlayerControl target)
    {
        killCooldown.Start();
        return base.TryKill(target);
    }

    [RoleAction(RoleActionType.OnPet)]
    private void LoadBullet()
    {

        if (currentBulletCount == 0 || loadBulletCooldown.NotReady() || loadedBullets >= maxLoadedBullets || sniperMode == 0) return;
        loadedBullets++;
        currentBulletCount--;
        loadBulletCooldown.Start();
        GameOptionOverride[] killCooldown = { new(Override.KillCooldown, loadBulletCooldown.Duration * 2) };
        DesyncOptions.SendModifiedOptions(killCooldown, MyPlayer);
        MyPlayer.RpcMark();
    }

    [RoleAction(RoleActionType.FixedUpdate)]
    private void SniperDirectionUpdate()
    {
        if (MyPlayer.MyPhysics.Velocity.magnitude != 0)
            lastDirection = MyPlayer.MyPhysics.Velocity;
    }

    [RoleAction(RoleActionType.Shapeshift)]
    private bool FireBullet(ActionHandle handle)
    {
        VentLogger.Trace("Firing Bullet");
        handle.Cancel();
        if (sniperMode == 1)
        {
            if (loadedBullets == 0 || killCooldown.NotReady() || loadBulletCooldown.NotReady()) return false;
            loadedBullets--;
        }
        else
        {
            if (currentBulletCount <= 0 || killCooldown.NotReady()) return false;
            currentBulletCount--;
        }

        Vector2 dir = lastDirection != null ? lastDirection : MyPlayer.MyPhysics.Velocity;
        bool killed = false;

        foreach (PlayerControl target in Game.GetAllPlayers().Where(p => p.PlayerId != MyPlayer.PlayerId && p.Relationship(MyPlayer) is not Relation.FullAllies))
        {
            Vector3 targetPos = target.transform.position - MyPlayer.transform.position;
            Vector3 targetDirection = targetPos.normalized;
            float dotProduct = Vector3.Dot(dir, targetDirection);
            float error = !preciseShooting ? targetPos.magnitude : Vector3.Cross(dir, targetPos).magnitude;
            if (dotProduct < 0.98 || (error < 1.0 && preciseShooting)) continue;
            float distance = Vector2.Distance(MyPlayer.transform.position, target.transform.position);
            InteractionResult result = MyPlayer.InteractWith(target, new RangedInteraction(new FatalIntent(true), distance, this));
            if (result == InteractionResult.Halt) continue;
            MyPlayer.RpcMark();
            killed = true;
        }

        float refundCooldown = realKillCooldown * 0.5f;
        GameOptionOverride[] modifiedCooldown = { new(Override.KillCooldown, refundCooldown) };
        DesyncOptions.SendModifiedOptions(modifiedCooldown, MyPlayer);
        killCooldown.Start(refundCooldown * 0.5f);

        Async.Schedule(() => MyPlayer.RpcRevertShapeshift(true), 0.3f);
        Async.Schedule(this.SyncOptions, 1f);

        return killed;
    }


    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Sniper Bullet Count")
                .Bind(v => totalBulletCount = (int)v)
                .AddIntRange(1, 30, 5, 5)
                .Build())
            .SubOption(sub => sub
                .Name("Precise Shooting")
                .Bind(v => preciseShooting = (bool)v)
                .AddOnOffValues()
                .Build())
            .SubOption(sub => sub
                .Name("Can Be Vetted On Snipe")
                .Bind(v => canBeVetted = (bool)v)
                .AddOnOffValues()
                .Build())
            .SubOption(sub => sub
                .Name("Sniper Mode")
                .Bind(v => sniperMode = (int)v)
                .Value(v => v.Text("Normal Mode").Value(0).Build())
                .Value(v => v.Text("Load Bullet Mode").Value(1).Build())
                .ShowSubOptionPredicate(obj => SniperMode.LoadBullet == (SniperMode)obj)
                .SubOption(sub2 => sub2
                    .Name("Load Bullet Cooldown")
                    .Bind(v => loadBulletCooldown.Duration = Convert.ToSingle(v))
                    .AddFloatRange(5, 120, 2.5f, 5, GeneralOptionTranslations.SecondsSuffix)
                    .Build())
                .SubOption(sub2 => sub2
                    .Name("Max Loaded Bullets")
                    .Bind(v => maxLoadedBullets = (int)v)
                    .AddIntRange(1, 10, 1)
                    .Build())
                .Build());

    private enum SniperMode
    {
        Normal,
        LoadBullet,
    }
}