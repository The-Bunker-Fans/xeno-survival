using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Spit.Slowing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoSpitSystem))]
public sealed partial class XenoSlowingSpitProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Slow = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Knockdown = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ArmorResistsKnockdown = true;
}
