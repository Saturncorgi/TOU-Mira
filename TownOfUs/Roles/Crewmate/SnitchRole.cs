﻿using System.Globalization;
using System.Text;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Modules.Wiki;
using TownOfUs.Options.Roles.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TownOfUs.Roles.Crewmate;

public sealed class SnitchRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    private Dictionary<byte, ArrowBehaviour>? _snitchArrows;
    public ArrowBehaviour? SnitchRevealArrow { get; private set; }
    public bool CompletedAllTasks { get; private set; }
    public bool OnLastTask { get; private set; }

    private void FixedUpdate()
    {
        if (Player == null || Player.Data.Role is not SnitchRole)
        {
            return;
        }

        if (SnitchRevealArrow != null && SnitchRevealArrow.target != SnitchRevealArrow.transform.parent.position)
        {
            SnitchRevealArrow.target = Player.transform.position;
        }

        if (_snitchArrows != null && _snitchArrows.Count > 0 && Player.AmOwner)
        {
            _snitchArrows.ToList().ForEach(arrow => arrow.Value.target = arrow.Value.transform.parent.position);
        }
    }

    public DoomableType DoomHintType => DoomableType.Insight;
    public string RoleName => "Snitch";
    public string RoleDescription => "Find the <color=#FF0000FF>Impostors</color>!";

    public string RoleLongDescription =>
        CompletedAllTasks ? "Find the Impostors!" : "Complete all your tasks to discover the Impostors.";

    public Color RoleColor => TownOfUsColors.Snitch;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateInvestigative;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouRoleIcons.Snitch,
        IntroSound = TouAudio.ToppatIntroSound
    };

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var alignment = RoleAlignment.ToDisplayString().Replace("Crewmate", "<color=#68ACF4FF>Crewmate");

        var stringB = new StringBuilder();
        stringB.AppendLine(CultureInfo.InvariantCulture,
            $"{RoleColor.ToTextColor()}You are a<b> {RoleName}.</b></color>");
        stringB.AppendLine(CultureInfo.InvariantCulture, $"<size=60%>Alignment: <b>{alignment}</color></b></size>");
        stringB.Append("<size=70%>");
        var desc = RoleLongDescription;
        if (PlayerControl.LocalPlayer.HasModifier<EgotistModifier>())
        {
            desc = CompletedAllTasks
                ? "Help the Impostors!"
                : "Complete all your tasks to discover & help the Impostors.";
        }

        stringB.AppendLine(CultureInfo.InvariantCulture, $"{desc}");

        return stringB;
    }

    public string GetAdvancedDescription()
    {
        return
            "The Snitch is a Crewmate Investigative role that can reveal the Impostors to themselves by finishing all their tasks. " +
            "Upon completing all tasks, the Impostors will be revealed to the Snitch with an arrow and their red name."
            + MiscUtils.AppendOptionsText(GetType());
    }

    public void CheckTaskRequirements()
    {
        var completedTasks = Player.myTasks.ToArray().Count(t => t.IsComplete);

        OnLastTask = Player.myTasks.Count - completedTasks <=
                     (int)OptionGroupSingleton<SnitchOptions>.Instance.TaskRemainingWhenRevealed;

        if (IsTargetOfSnitch(PlayerControl.LocalPlayer) && OnLastTask)
        {
            CreateRevealingArrow();
            Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.Snitch, alpha: 0.5f));
            var text = "The Snitch is getting closer to reveal you!";
            if (Player.HasModifier<EgotistModifier>())
            {
                text = "The Snitch is an Egotist, who will help you overthrow the crewmates!";
            }

            var notif1 = Helpers.CreateAndShowNotification(
                $"<b>{TownOfUsColors.Snitch.ToTextColor()}{text}</color></b>", Color.white,
                spr: TouRoleIcons.Snitch.LoadAsset());

            notif1.Text.SetOutlineThickness(0.35f);
            notif1.transform.localPosition = new Vector3(0f, 1f, -20f);
        }

        CompletedAllTasks = completedTasks == Player.myTasks.Count;

        if (OnLastTask && Player.AmOwner && !CompletedAllTasks)
        {
            Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.Snitch, alpha: 0.5f));
            var text = "The impostors know of your whereabouts!";
            if (Player.HasModifier<EgotistModifier>())
            {
                text = "The impostors know of your whereabouts, and know you're the Egotist!";
            }

            var notif1 = Helpers.CreateAndShowNotification(
                $"<b>{TownOfUsColors.Snitch.ToTextColor()}{text}</color></b>", Color.white,
                spr: TouRoleIcons.Snitch.LoadAsset());

            notif1.Text.SetOutlineThickness(0.35f);
            notif1.transform.localPosition = new Vector3(0f, 1f, -20f);
        }

        if (CompletedAllTasks && IsTargetOfSnitch(PlayerControl.LocalPlayer))
        {
            Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.Snitch, alpha: 0.5f));
            var text = "The Snitch knows what you are now!";
            if (Player.HasModifier<EgotistModifier>())
            {
                text = "The Snitch can now help you as the Egotist!";
            }

            var notif1 = Helpers.CreateAndShowNotification(
                $"<b>{TownOfUsColors.Snitch.ToTextColor()}{text}</color></b>", Color.white,
                spr: TouRoleIcons.Snitch.LoadAsset());

            notif1.Text.SetOutlineThickness(0.35f);
            notif1.transform.localPosition = new Vector3(0f, 1f, -20f);
        }

        if (CompletedAllTasks && Player.AmOwner)
        {
            CreateSnitchArrows();
            var text = "You have revealed the impostors!";
            if (Player.HasModifier<EgotistModifier>())
            {
                text = "You have revealed the impostors, who can help your win condition!";
            }

            var notif1 = Helpers.CreateAndShowNotification(
                $"<b>{TownOfUsColors.Snitch.ToTextColor()}{text}</color></b>", Color.white,
                spr: TouRoleIcons.Snitch.LoadAsset());

            notif1.Text.SetOutlineThickness(0.35f);
            notif1.transform.localPosition = new Vector3(0f, 1f, -20f);
        }
    }

    public static bool IsTargetOfSnitch(PlayerControl player)
    {
        if (player == null || player.Data == null || player.Data.Role == null)
        {
            return false;
        }

        return (player.IsImpostor() && !player.IsTraitor()) ||
               (player.IsTraitor() && OptionGroupSingleton<SnitchOptions>.Instance.SnitchSeesTraitor) ||
               (player.Is(RoleAlignment.NeutralKilling) &&
                OptionGroupSingleton<SnitchOptions>.Instance.SnitchNeutralRoles);
    }

    public static bool SnitchVisibilityFlag(PlayerControl player, bool showRole = false)
    {
        var snitchRevealed = PlayerControl.LocalPlayer.Data.Role is SnitchRole snitch && snitch.CompletedAllTasks;
        var showSnitch = IsTargetOfSnitch(PlayerControl.LocalPlayer) && player.Data.Role is SnitchRole snitch2 &&
                         snitch2.OnLastTask;

        if (MeetingHud.Instance && !OptionGroupSingleton<SnitchOptions>.Instance.SnitchSeesImpostorsMeetings)
        {
            snitchRevealed = false;
        }

        return (snitchRevealed && IsTargetOfSnitch(player) && !showRole) || showSnitch;
    }

    public override void OnDeath(DeathReason reason)
    {
        RoleBehaviourStubs.OnDeath(this, reason);

        ClearArrows();
    }

    public void RemoveArrowForPlayer(byte playerId)
    {
        if (_snitchArrows != null && _snitchArrows.TryGetValue(playerId, out var arrow))
        {
            arrow.gameObject.Destroy();
            _snitchArrows.Remove(playerId);
        }
    }

    public void ClearArrows()
    {
        if (_snitchArrows != null && _snitchArrows.Count > 0)
        {
            _snitchArrows.ToList().ForEach(arrow => arrow.Value.gameObject.Destroy());
            _snitchArrows.Clear();
        }

        if (SnitchRevealArrow != null)
        {
            SnitchRevealArrow.gameObject.Destroy();
        }
    }

    private void CreateRevealingArrow()
    {
        if (SnitchRevealArrow != null)
        {
            return;
        }

        PlayerNameColor.Set(Player);
        Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.Snitch, alpha: 0.5f));
        SnitchRevealArrow = MiscUtils.CreateArrow(Player.transform, TownOfUsColors.Snitch);
    }

    private void CreateSnitchArrows()
    {
        if (_snitchArrows != null)
        {
            return;
        }

        Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.Snitch, alpha: 0.5f));
        _snitchArrows = new Dictionary<byte, ArrowBehaviour>();
        var imps = Helpers.GetAlivePlayers().Where(plr => plr.Data.Role.IsImpostor && !plr.IsTraitor());
        var traitor = Helpers.GetAlivePlayers().FirstOrDefault(plr => plr.IsTraitor());
        imps.ToList().ForEach(imp =>
        {
            _snitchArrows.Add(imp.PlayerId, MiscUtils.CreateArrow(imp.transform, TownOfUsColors.Impostor));
            PlayerNameColor.Set(imp);
        });

        if (OptionGroupSingleton<SnitchOptions>.Instance.SnitchSeesTraitor && traitor != null)
        {
            _snitchArrows.Add(traitor.PlayerId, MiscUtils.CreateArrow(traitor.transform, TownOfUsColors.Impostor));
            PlayerNameColor.Set(traitor);
        }

        if (OptionGroupSingleton<SnitchOptions>.Instance.SnitchNeutralRoles)
        {
            var neutrals = MiscUtils.GetRoles(RoleAlignment.NeutralKilling)
                .Where(role => !role.Player.Data.IsDead && !role.Player.Data.Disconnected);
            neutrals.ToList().ForEach(neutral =>
            {
                _snitchArrows.Add(neutral.Player.PlayerId,
                    MiscUtils.CreateArrow(neutral.Player.transform, TownOfUsColors.Neutral));
                PlayerNameColor.Set(neutral.Player);
            });
        }
    }

    public void AddSnitchTraitorArrows()
    {
        var completedTasks = Player.myTasks.ToArray().Count(t => t.IsComplete);

        OnLastTask = Player.myTasks.Count - completedTasks <=
                     (int)OptionGroupSingleton<SnitchOptions>.Instance.TaskRemainingWhenRevealed;

        if (PlayerControl.LocalPlayer.IsTraitor() && OptionGroupSingleton<SnitchOptions>.Instance.SnitchSeesTraitor &&
            OnLastTask)
        {
            CreateRevealingArrow();
        }

        CompletedAllTasks = completedTasks == Player.myTasks.Count;

        if (CompletedAllTasks && Player.AmOwner)
        {
            var traitor = Helpers.GetAlivePlayers().FirstOrDefault(plr => plr.IsTraitor());
            if (_snitchArrows == null || traitor == null ||
                (_snitchArrows.TryGetValue(traitor.PlayerId, out var arrow) && arrow != null))
            {
                return;
            }

            if (OptionGroupSingleton<SnitchOptions>.Instance.SnitchSeesTraitor && traitor != null)
            {
                _snitchArrows.Add(traitor.PlayerId, MiscUtils.CreateArrow(traitor.transform, TownOfUsColors.Impostor));
                PlayerNameColor.Set(traitor);
            }
        }
    }
}