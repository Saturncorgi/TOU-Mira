﻿using HarmonyLib;
using TownOfUs.Roles.Crewmate;

namespace TownOfUs.Patches.Roles;

[HarmonyPatch]
public static class ProsecutorPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.VoteForMe))]
    public static bool VotePatch(PlayerVoteArea __instance)
    {
        if (PlayerControl.LocalPlayer.Data.Role is not ProsecutorRole prosecutor)
        {
            return true;
        }

        if (__instance.Parent.state is MeetingHud.VoteStates.Proceeding or MeetingHud.VoteStates.Results)
        {
            return false;
        }

        if (__instance == prosecutor.ProsecuteButton && !prosecutor.SelectingProsecuteVictim)
        {
            prosecutor.SelectingProsecuteVictim = true;
            return false;
        }

        if (__instance != prosecutor.ProsecuteButton && __instance != MeetingHud.Instance.SkipVoteButton &&
            prosecutor.SelectingProsecuteVictim)
        {
            ProsecutorRole.RpcProsecute(PlayerControl.LocalPlayer);
        }

        if (__instance == MeetingHud.Instance.SkipVoteButton && prosecutor.SelectingProsecuteVictim)
        {
            prosecutor.SelectingProsecuteVictim = false;
        }

        return true;
    }
}