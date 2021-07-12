using Content.Server.Interaction.Components;
using Content.Server.Sound.Components;
using Content.Server.Throwing;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Player;

namespace Content.Server.Sound
{
    /// <summary>
    /// Will play a sound on various events if the affected entity has a component derived from BaseEmitSoundComponent
    /// </summary>
    [UsedImplicitly]
    public class EmitSoundSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmitSoundOnLandComponent, LandEvent>(HandleEmitSoundOnLand);
            SubscribeLocalEvent<EmitSoundOnUseComponent, UseInHandEvent>(HandleEmitSoundOnUseInHand);
            SubscribeLocalEvent<EmitSoundOnThrowComponent, ThrownEvent>(HandleEmitSoundOnThrown);
            SubscribeLocalEvent<EmitSoundOnActivateComponent, ActivateInWorldEvent>(HandleEmitSoundOnActivateInWorld);
        }

        private void HandleEmitSoundOnLand(EntityUid eUI, BaseEmitSoundComponent component, LandEvent arg)
        {
            TryEmitSound(component);
        }

        private void HandleEmitSoundOnUseInHand(EntityUid eUI, BaseEmitSoundComponent component, UseInHandEvent arg)
        {
            TryEmitSound(component);
        }

        private void HandleEmitSoundOnThrown(EntityUid eUI, BaseEmitSoundComponent component, ThrownEvent arg)
        {
            TryEmitSound(component);
        }

        private void HandleEmitSoundOnActivateInWorld(EntityUid eUI, BaseEmitSoundComponent component, ActivateInWorldEvent arg)
        {
            TryEmitSound(component);
        }

        private static void TryEmitSound(BaseEmitSoundComponent component)
        {
            if (!string.IsNullOrWhiteSpace(component.Sound.GetSound()))
            {
                SoundSystem.Play(Filter.Pvs(component.Owner), component.Sound.GetSound(), component.Owner, AudioHelpers.WithVariation(component.PitchVariation).WithVolume(-2f));
            }
            else
            {
                Logger.Warning($"{nameof(component)} Uid:{component.Owner.Uid} has no {nameof(component.Sound)} to play.");
            }
        }
    }
}

