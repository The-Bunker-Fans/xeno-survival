using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableToggleableSystem))]
public sealed partial class AttachableMovementLockedComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> AttachableList = new();
}

