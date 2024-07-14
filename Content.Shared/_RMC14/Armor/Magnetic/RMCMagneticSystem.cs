﻿using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Throwing;

namespace Content.Shared._RMC14.Armor.Magnetic;

public sealed class RMCMagneticSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMagneticItemComponent, DroppedEvent>(OnMagneticItemDropped);
        SubscribeLocalEvent<RMCMagneticItemComponent, ThrownEvent>(OnMagneticItemThrown);
        SubscribeLocalEvent<RMCMagneticArmorComponent, InventoryRelayedEvent<RMCMagnetizeItemEvent>>(OnMagnetizeItem);

        SubscribeLocalEvent<InventoryComponent, RMCMagnetizeItemEvent>(_inventory.RelayEvent);
    }

    private void OnMagneticItemDropped(Entity<RMCMagneticItemComponent> ent, ref DroppedEvent args)
    {
        TryReturn(ent, args.User);
    }

    private void OnMagneticItemThrown(Entity<RMCMagneticItemComponent> ent, ref ThrownEvent args)
    {
        if (args.User is not { } user)
            return;

        if (!TryReturn(ent, user))
            return;

        if (TryComp(ent, out ThrownItemComponent? thrown))
            _thrownItem.StopThrow(ent, thrown);
    }

    private void OnMagnetizeItem(Entity<RMCMagneticArmorComponent> ent, ref InventoryRelayedEvent<RMCMagnetizeItemEvent> args)
    {
        args.Args.Magnetizer = ent;
    }

    private bool TryReturn(Entity<RMCMagneticItemComponent> ent, EntityUid user)
    {
        var ev = new RMCMagnetizeItemEvent(SlotFlags.OUTERCLOTHING);
        RaiseLocalEvent(user, ref ev);

        if (ev.Magnetizer is not { } magnetizer)
            return false;

        var returnComp = EnsureComp<RMCReturnToInventoryComponent>(ent);
        returnComp.User = user;
        returnComp.Magnetizer = magnetizer;

        Dirty(ent, returnComp);
        return true;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCReturnToInventoryComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Returned)
                continue;

            var user = comp.User;
            var magnetizer = comp.Magnetizer;
            if (!TerminatingOrDeleted(user) && !TerminatingOrDeleted(magnetizer))
            {
                var slots = _inventory.GetSlotEnumerator(user, SlotFlags.SUITSTORAGE);
                while (slots.MoveNext(out var slot))
                {
                    if (_inventory.TryEquip(user, uid, slot.ID, force: true))
                    {
                        var popup = Loc.GetString("rmc-magnetize-return",
                            ("item", uid),
                            ("magnetizer", magnetizer));
                        _popup.PopupClient(popup, user, user, PopupType.Medium);

                        comp.Returned = true;
                        Dirty(uid, comp);
                        break;
                    }
                }
            }

            RemCompDeferred<RMCReturnToInventoryComponent>(uid);
        }
    }
}
