extern alias JBAnnotations;
using System;
using System.Collections.Generic;
using JBAnnotations::JetBrains.Annotations;
using Lotus.API.Odyssey;
using Lotus.API.Vanilla.Meetings;
using Lotus.Patches.Meetings;
using VentLib.Utilities.Optionals;

// ReSharper disable InvalidXmlDocComment

namespace Lotus.Roles.Internals.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)] // Inherited = false because inheritance is managed through Subclassing, DO NOT WORRY!
public class RoleActionAttribute: Attribute
{
    public RoleActionType ActionType { get; }
    public bool WorksAfterDeath { get; }
    public Priority Priority { get; }
    public bool Blockable { set; get; }
    /// <summary>
    /// If provided, overrides any methods of the same action with the same name from any parent classes
    /// </summary>
    public String? Override;
    /// <summary>
    /// Dictates whether this action should be utilized in subclasses of the class declaring this method <b>Default: True</b>
    /// </summary>
    public bool Subclassing = true;

    public RoleActionAttribute(RoleActionType actionType, bool triggerAfterDeath = false, bool blockable = true, Priority priority = Priority.Normal)
    {
        this.ActionType = actionType;
        this.WorksAfterDeath = triggerAfterDeath || actionType is RoleActionType.MyDeath or RoleActionType.SelfExiled;
        this.Priority = priority;
        this.Blockable = blockable && actionType is not RoleActionType.MyVote;
    }

    public override string ToString() => $"RoleAction(type={ActionType}, Priority={Priority}, Blockable={Blockable}, Subclassing={Subclassing}, Override={Override})";
}

public enum Priority
{
    First = 0,
    VeryHigh = 200,
    High = 400,
    Normal = 600,
    Low = 800,
    VeryLow = 1000,
    Last = int.MaxValue
}

public enum RoleActionType
{
    /// <summary>
    /// Represents no action
    /// </summary>
    None,
    /// <summary>
    /// Any action specifically taken by a player
    /// Parameters: (PlayerControl source, RoleAction action, object[] parameters)
    /// </summary>
    AnyPlayerAction,
    /// <summary>
    /// Triggers when any player pets
    /// </summary>
    AnyPet,
    OnPet,
    /// <summary>
    /// Triggers when the pet button is held down. This gets sent every 0.4 seconds if the button is held down. The
    /// times parameter indicates how many times the button has been held down during the current span.
    /// <br/>
    /// Example: if times = 3 then the button has been held down for 1.2 seconds because 3 x 0.4 = 1.2
    /// </summary>
    /// <param name="times">the number of times the button has been detected in the down state (+1 every 0.4 seconds)</param>
    OnHoldPet,
    /// <summary>
    /// Triggers when the pet button has been held then released. Similar to <see cref="OnHoldPet"/>, the
    /// times parameter indicates how many times the button has been held down during the current span.
    /// </summary>
    /// <param name="times">the number of times the button has been detected in the down state (+1 every 0.4 seconds)</param>
    OnPetRelease,
    /// <summary>
    /// Triggers whenever the player enters a vent (this INCLUDES vent activation)
    /// Parameters: (Vent vent)
    /// </summary>
    MyEnterVent,
    /// <summary>
    /// Triggered when a player ACTUALLY enters a vent (not just Vent activation)
    /// Parameters: (Vent vent, PlayerControl venter)
    /// </summary>
    AnyEnterVent,
    VentExit,
    SuccessfulAngelProtect,
    SabotageStarted,
    /// <summary>
    /// Triggered when any one player fixes any part of a sabotage (I.E MiraHQ Comms) <br></br>
    /// Parameters: (SabotageType type, PlayerControl fixer, byte fixBit)
    /// </summary>
    SabotagePartialFix,
    SabotageFixed,
    AnyShapeshift,
    Shapeshift,
    AnyUnshapeshift,
    Unshapeshift,
    /// <summary>
    /// Triggered when my player attacks another player<br/>
    /// Parameters: (PlayerControl target)
    /// </summary>
    Attack,
    /// <summary>
    /// Triggered when my player dies. This action <b>CANNOT</b> be canceled. <br/>
    /// </summary>
    /// <param name="killer"><see cref="PlayerControl"/> the killer</param>
    /// <param name="realKiller"><see cref="Optional{T}"/> the OPTIONAl real killer (exists if killed indirectly)</param>
    MyDeath,
    SelfExiled,
    /// <summary>
    /// Triggers when any player gets exiled (by being voted out)
    /// </summary>
    /// <param name="exiled"><see cref="GameData.PlayerInfo"/> the exiled player</param>
    AnyExiled,
    /// <summary>
    /// Triggers on Round Start (end of meetings, and start of game)
    /// Parameters: (bool isRoundOne)
    /// </summary>
    RoundStart,
    RoundEnd,
    SelfReportBody,
    /// <summary>
    /// Triggers when any player reports a body. <br></br>Parameters: (PlayerControl reporter, PlayerInfo reported)
    /// </summary>
    AnyReportedBody,
    /// <summary>
    /// Triggers when any player completes a task. This cannot be canceled (Currently)
    /// </summary>
    /// <param name="player"><see cref="PlayerControl"/> the player completing the task</param>
    /// <param name="task"><see cref="Optional"/> an optional of <see cref="PlayerTask"/>, containing the task that was done</param>
    /// <param name="taskLength"><see cref="NormalPlayerTask.TaskLength"/> the length of the completed task</param>
    TaskComplete,
    FixedUpdate,
    /// <summary>
    /// Triggers when any player dies. This cannot be canceled
    /// </summary>
    /// <param name="victim"><see cref="PlayerControl"/> the dead player</param>
    /// <param name="killer"><see cref="PlayerControl"/> the killing player</param>
    /// <param name="deathEvent"><see cref="Lotus.Managers.History.Events.IDeathEvent"/> the related death event </param>
    AnyDeath,
    /// <summary>
    /// Triggers when my player votes for someone (or skips)
    /// </summary>
    /// <param name="voted"><see cref="PlayerControl"/> the player voted for, or null if skipped</param>
    /// <param name="delegate"><see cref="MeetingDelegate"/> the meeting delegate for the current meeting</param>
    MyVote,
    /// <summary>
    /// Triggers when any player votes for someone (or skips)
    /// </summary>
    /// <param name="voter"><see cref="PlayerControl"/> the player voting</param>
    /// <param name="voted"><see cref="PlayerControl"/> the player voted for, or null if skipped</param>
    /// <param name="delegate"><see cref="MeetingDelegate"/> the meeting delegate for the current meeting</param>
    AnyVote,
    /// <summary>
    /// Triggers whenever another player interacts with THIS role
    /// </summary>
    /// <param name="interactor"><see cref="PlayerControl"/> the player starting the interaction</param>
    /// <param name="interaction"><see cref="Interaction"/> the interaction</param>
    Interaction,
    /// <summary>
    /// Triggers whenever another player interacts with any other player
    /// </summary>
    /// <param name="interactor"><see cref="PlayerControl"/> the player starting the interaction</param>
    /// <param name="target"><see cref="PlayerControl"/> the player being interacted with</param>
    /// <param name="interaction"><see cref="Interaction"/> the interaction</param>
    AnyInteraction,
    /// <summary>
    /// Triggers whenever a player sends a chat message. This action cannot be canceled.
    /// </summary>
    /// <param name="sender"><see cref="PlayerControl"/> the player who sent the chat message</param>
    /// <param name="message"><see cref="string"/> the message sent</param>
    /// <param name="state"><see cref="GameState"/> the current state of the game (for checking in meeting)</param>
    /// <param name="isAlive"><see cref="bool"/> if the chatting player is alive</param>
    Chat,
    /// <summary>
    /// Triggers whenever a player leaves the game. This action cannot be canceled
    /// </summary>
    /// <param name="player"><see cref="PlayerControl"/> the player who disconnected</param>
    Disconnect,
    /// <summary>
    /// Triggers when voting session ends. This action cannot be canceled.
    /// <b>IMPORTANT</b><br/>
    /// You CAN modify the meeting delegate at this time to change the results of the meeting. HOWEVER,
    /// modifying the votes will only change what is displayed during the meeting. You MUST also update the exiled player to change
    /// the exiled player, as the votes WILL NOT be recalculated automatically at this point. <see cref="MeetingDelegate.CalculateExiledPlayer"/>
    /// </summary>
    /// <param name="meetingDelegate"><see cref="MeetingDelegate"/> the meeting delegate for the current meeting</param>
    VotingComplete,
    /// <summary>
    /// Triggers when the meeting ends, this does not pass the meeting delegate as at this point everything has been finalized.
    /// <param name="Exiled Player">><see cref="Optional{T}"/> the optional exiled player</param>
    /// <param name="isTie"><see cref="bool"/> a boolean representing if the meeting tied</param>
    /// <param name="player vote counts"<see cref="Dictionary{TKey,TValue}"/> a dictionary containing (byte, int) representing the amount of votes a player got</param>
    /// <param name="playerVoteStatus"><see cref="Dictionary{TKey,TValue}"/> a dictionary containing (byte, List[Optional[byte]] containing the voting statuses of all players)
    /// </summary>
    MeetingEnd,
    /// <summary>
    /// Triggers when a meeting is called
    /// </summary>
    /// <param name="player"><see cref="PlayerControl"/> the player who called the meeting</param>
    /// <param name="deadBody"><see cref="Optional{T}"/> optional <see cref="GameData.PlayerInfo"/> which exists if the meeting was called byt reporting a body</param>
    MeetingCalled
}

public static class RoleActionTypeMethods
{
    // ReSharper disable once CollectionNeverUpdated.Global
    public static readonly HashSet<RoleActionType> PlayerActions = new();

    public static bool IsPlayerAction(this RoleActionType actionType)
    {
        return actionType switch
        {
            RoleActionType.None => false,
            RoleActionType.AnyPlayerAction => false,
            RoleActionType.OnPet => true,
            RoleActionType.MyEnterVent => true,
            RoleActionType.AnyEnterVent => true,
            RoleActionType.VentExit => true,
            RoleActionType.SuccessfulAngelProtect => false,
            RoleActionType.SabotageStarted => true,
            RoleActionType.SabotagePartialFix => true,
            RoleActionType.SabotageFixed => true,
            RoleActionType.Shapeshift => true,
            RoleActionType.Unshapeshift => true,
            RoleActionType.Attack => true,
            RoleActionType.MyDeath => false,
            RoleActionType.SelfExiled => false,
            RoleActionType.AnyExiled => false,
            RoleActionType.RoundStart => false,
            RoleActionType.RoundEnd => false,
            RoleActionType.SelfReportBody => true,
            RoleActionType.AnyReportedBody => false,
            RoleActionType.TaskComplete => false,
            RoleActionType.FixedUpdate => false,
            RoleActionType.AnyDeath => false,
            RoleActionType.MyVote => true,
            RoleActionType.AnyVote => false,
            RoleActionType.Interaction => false,
            RoleActionType.AnyInteraction => false,
            RoleActionType.OnHoldPet => false,
            RoleActionType.OnPetRelease => false,
            RoleActionType.AnyShapeshift => false,
            RoleActionType.AnyUnshapeshift => false,
            RoleActionType.Chat => false,
            RoleActionType.Disconnect => false,
            RoleActionType.VotingComplete => false,
            RoleActionType.MeetingCalled => true,
            _ => PlayerActions.Contains(actionType)
        };
    }
}