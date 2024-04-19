using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._CM14.Marines.Orders;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MoveOrderComponent : Component, IOrderComponent
{
    [DataField, AutoNetworkedField]
    public List<(FixedPoint2 Multiplier, TimeSpan ExpiresAt)> Received { get; set; } = new();

    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new Rsi(new ResPath("/Textures/_CM14/Interface/marine_orders.rsi"), "move");

    [DataField, AutoNetworkedField]
    public FixedPoint2 MoveSpeedModifier = 0.1;

    // TODO CM14 Actually make this do something once we got melee dodging
    [DataField, AutoNetworkedField]
    public FixedPoint2 DodgeModifier = 0.1;
}
