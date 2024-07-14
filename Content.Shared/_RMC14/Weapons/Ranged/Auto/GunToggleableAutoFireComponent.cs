﻿using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Auto;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GunToggleableAutoFireSystem))]
public sealed partial class GunToggleableAutoFireComponent : Component
{
    [DataField, AutoNetworkedField]
    public Box2 Range = new(0, 0, 7, 15);

    [DataField, AutoNetworkedField]
    public float BatteryDrain = 5;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionToggleAutoFire";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;
}
