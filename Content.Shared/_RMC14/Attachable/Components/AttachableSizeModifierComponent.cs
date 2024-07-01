using Content.Shared._RMC14.Attachable.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableModifiersSystem))]
public sealed partial class AttachableSizeModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<AttachableSizeModifierSet> Modifiers = new();

    public int ResetIncrement = 0;
}
