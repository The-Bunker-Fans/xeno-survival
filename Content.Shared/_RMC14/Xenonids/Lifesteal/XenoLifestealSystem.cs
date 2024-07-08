﻿using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Marines;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Lifesteal;

public sealed class XenoLifestealSystem : EntitySystem
{
    [Dependency] private readonly CMDamageableSystem _cmDamageable = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private readonly HashSet<Entity<MarineComponent>> _targets = new();

    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<MarineComponent> _marineQuery;

    public override void Initialize()
    {
        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _marineQuery = GetEntityQuery<MarineComponent>();

        SubscribeLocalEvent<XenoLifestealComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<XenoLifestealComponent> xeno, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var found = false;
        foreach (var hit in args.HitEntities)
        {
            if (!_marineQuery.HasComp(hit))
                continue;

            found = true;
            break;
        }

        if (!found)
            return;

        if (!_damageableQuery.TryComp(xeno, out var damageable))
            return;

        var total = damageable.TotalDamage;
        if (total == FixedPoint2.Zero)
            return;

        _targets.Clear();
        _entityLookup.GetEntitiesInRange(xeno.Owner.ToCoordinates(), xeno.Comp.TargetRange, _targets);

        var lifesteal = xeno.Comp.BasePercentage;
        foreach (var hit in _targets)
        {
            if (!_marineQuery.HasComp(hit))
                continue;

            lifesteal += xeno.Comp.TargetIncreasePercentage;
            if (lifesteal >= xeno.Comp.MaxPercentage)
            {
                lifesteal = xeno.Comp.MaxPercentage;
                break;
            }
        }

        var amount = -FixedPoint2.Clamp(total * lifesteal, xeno.Comp.MinHeal, xeno.Comp.MaxHeal);
        var heal = _cmDamageable.DistributeTypes(xeno.Owner, amount);
        _damageable.TryChangeDamage(xeno, heal, true);

        if (lifesteal >= xeno.Comp.MaxPercentage)
        {
            var marines = Filter.PvsExcept(xeno).RemoveWhereAttachedEntity(e => !_marineQuery.HasComp(e));
            var marineMsg = Loc.GetString("rmc-lifesteal-more-marine", ("xeno", xeno.Owner));
            _popup.PopupEntity(marineMsg, xeno, marines, true, PopupType.SmallCaution);

            var selfMsg = Loc.GetString("rmc-lifesteal-more-self");
            _popup.PopupClient(selfMsg, xeno, xeno);
        }
    }
}
