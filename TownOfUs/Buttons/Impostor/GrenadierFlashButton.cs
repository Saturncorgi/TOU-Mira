﻿using BepInEx.Unity.IL2CPP.Utils.Collections;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using TownOfUs.Modifiers.Impostor;
using TownOfUs.Options.Roles.Impostor;
using TownOfUs.Roles.Impostor;
using UnityEngine;

namespace TownOfUs.Buttons.Impostor;

public sealed class GrenadierFlashButton : TownOfUsRoleButton<GrenadierRole>, IAftermathableButton
{
    public override string Name => "Flash";
    public override string Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TownOfUsColors.Impostor;
    public override float Cooldown => OptionGroupSingleton<GrenadierOptions>.Instance.GrenadeCooldown + MapCooldown;
    public override float EffectDuration => OptionGroupSingleton<GrenadierOptions>.Instance.GrenadeDuration;
    public override int MaxUses => (int)OptionGroupSingleton<GrenadierOptions>.Instance.MaxFlashes;
    public override LoadableAsset<Sprite> Sprite => TouImpAssets.FlashSprite;

    protected override void OnClick()
    {
        var flashRadius = OptionGroupSingleton<GrenadierOptions>.Instance.FlashRadius;
        var flashedPlayers =
            Helpers.GetClosestPlayers(PlayerControl.LocalPlayer, flashRadius * ShipStatus.Instance.MaxLightRadius);

        foreach (var player in flashedPlayers)
        {
            player.RpcAddModifier<GrenadierFlashModifier>(PlayerControl.LocalPlayer);
        }

        PlayerControl.LocalPlayer.RpcAddModifier<GrenadierFlashModifier>(PlayerControl.LocalPlayer);

        Coroutines.Start(
            Effects.Shake(HudManager.Instance.PlayerCam.transform, 0.2f, 0.1f, true, true).WrapToManaged());

        SoundManager.Instance.PlaySound(TouAudio.GrenadeSound.LoadAsset(), false);
    }
}