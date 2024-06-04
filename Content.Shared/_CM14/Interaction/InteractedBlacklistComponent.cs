﻿using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Interaction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMInteractionSystem))]
public sealed partial class InteractedBlacklistComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}
