﻿using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InfectableComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<Sex, SoundSpecifier> Sound = new()
    {
        [Sex.Male] = new SoundPathSpecifier("/Audio/_RMC14/Voice/Human/infected_male.ogg"),
        [Sex.Female] = new SoundPathSpecifier("/Audio/_RMC14/Voice/Human/infected_female.ogg")
    };
}
