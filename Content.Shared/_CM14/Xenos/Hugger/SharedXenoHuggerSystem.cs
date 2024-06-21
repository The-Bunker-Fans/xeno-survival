using Content.Shared._CM14.Hands;
using Content.Shared._CM14.Xenos.Leap;
using Content.Shared._CM14.Xenos.Pheromones;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Hugger;

public abstract class SharedXenoHuggerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly CMHandsSystem _cmHands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HuggableComponent, ActivateInWorldEvent>(OnHuggableActivate);
        SubscribeLocalEvent<HuggableComponent, CanDropTargetEvent>(OnHuggableCanDropTarget);

        SubscribeLocalEvent<XenoHuggerComponent, XenoLeapHitEvent>(OnHuggerLeapHit);
        SubscribeLocalEvent<XenoHuggerComponent, AfterInteractEvent>(OnHuggerAfterInteract);
        SubscribeLocalEvent<XenoHuggerComponent, DoAfterAttemptEvent<AttachHuggerDoAfterEvent>>(OnHuggerAttachDoAfterAttempt);
        SubscribeLocalEvent<XenoHuggerComponent, AttachHuggerDoAfterEvent>(OnHuggerAttachDoAfter);
        SubscribeLocalEvent<XenoHuggerComponent, CanDragEvent>(OnHuggerCanDrag);
        SubscribeLocalEvent<XenoHuggerComponent, CanDropDraggedEvent>(OnHuggerCanDropDragged);
        SubscribeLocalEvent<XenoHuggerComponent, DragDropDraggedEvent>(OnHuggerDragDropDragged);

        SubscribeLocalEvent<HuggerSpentComponent, MapInitEvent>(OnHuggerSpentMapInit);
        SubscribeLocalEvent<HuggerSpentComponent, UpdateMobStateEvent>(OnHuggerSpentUpdateMobState,
            after: [typeof(MobThresholdSystem), typeof(SharedXenoPheromonesSystem)]);

        SubscribeLocalEvent<VictimHuggedComponent, MapInitEvent>(OnVictimHuggedMapInit);
        SubscribeLocalEvent<VictimHuggedComponent, ComponentRemove>(OnVictimHuggedRemoved);
        SubscribeLocalEvent<VictimHuggedComponent, CanSeeAttemptEvent>(OnVictimHuggedCancel);
        SubscribeLocalEvent<VictimHuggedComponent, ExaminedEvent>(OnVictimHuggedExamined);
        SubscribeLocalEvent<VictimHuggedComponent, RejuvenateEvent>(OnVictimHuggedRejuvenate);

        SubscribeLocalEvent<VictimBurstComponent, MapInitEvent>(OnVictimBurstMapInit);
        SubscribeLocalEvent<VictimBurstComponent, UpdateMobStateEvent>(OnVictimUpdateMobState,
            after: [typeof(MobThresholdSystem), typeof(SharedXenoPheromonesSystem)]);
        SubscribeLocalEvent<VictimBurstComponent, RejuvenateEvent>(OnVictimBurstRejuvenate);
    }

    private void OnHuggableActivate(Entity<HuggableComponent> ent, ref ActivateInWorldEvent args)
    {
        if (TryComp(args.User, out XenoHuggerComponent? hugger) &&
            StartHug((args.User, hugger), args.Target, args.User))
        {
            args.Handled = true;
        }
    }

    private void OnHuggableCanDropTarget(Entity<HuggableComponent> ent, ref CanDropTargetEvent args)
    {
        if (TryComp(args.Dragged, out XenoHuggerComponent? hugger) &&
            CanHugPopup((args.Dragged, hugger), ent, args.User, false))
        {
            args.CanDrop = true;
            args.Handled = true;
        }
    }

    private void OnHuggerLeapHit(Entity<XenoHuggerComponent> hugger, ref XenoLeapHitEvent args)
    {
        var coordinates = _transform.GetMoverCoordinates(hugger);
        if (_transform.InRange(coordinates, args.Leaping.Origin, hugger.Comp.HugRange))
            Hug(hugger, args.Hit, false);
    }

    private void OnHuggerAfterInteract(Entity<XenoHuggerComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (StartHug(ent, args.Target.Value, args.User))
            args.Handled = true;
    }

    private void OnHuggerAttachDoAfterAttempt(Entity<XenoHuggerComponent> ent, ref DoAfterAttemptEvent<AttachHuggerDoAfterEvent> args)
    {
        if (args.DoAfter.Args.Target is not { } target)
        {
            args.Cancel();
            return;
        }

        if (!CanHugPopup(ent, target, ent))
            args.Cancel();
    }

    private void OnHuggerAttachDoAfter(Entity<XenoHuggerComponent> ent, ref AttachHuggerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        if (Hug(ent, args.Target.Value))
            args.Handled = true;
    }

    private void OnHuggerCanDrag(Entity<XenoHuggerComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnHuggerCanDropDragged(Entity<XenoHuggerComponent> ent, ref CanDropDraggedEvent args)
    {
        if (args.User != ent.Owner && !_cmHands.IsPickupByAllowed(ent.Owner, args.User))
            return;

        if (!CanHugPopup(ent, args.Target, args.User, false))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnHuggerDragDropDragged(Entity<XenoHuggerComponent> ent, ref DragDropDraggedEvent args)
    {
        if (args.User != ent.Owner && !_cmHands.IsPickupByAllowed(ent.Owner, args.User))
            return;

        StartHug(ent, args.Target, args.User);
        args.Handled = true;
    }

    protected virtual void HuggerLeapHit(Entity<XenoHuggerComponent> hugger)
    {
    }

    private void OnHuggerSpentMapInit(Entity<HuggerSpentComponent> spent, ref MapInitEvent args)
    {
        if (TryComp(spent, out MobStateComponent? mobState))
            _mobState.UpdateMobState(spent, mobState);
    }

    private void OnHuggerSpentUpdateMobState(Entity<HuggerSpentComponent> spent, ref UpdateMobStateEvent args)
    {
        args.State = MobState.Dead;
    }

    private void OnVictimHuggedMapInit(Entity<VictimHuggedComponent> victim, ref MapInitEvent args)
    {
        victim.Comp.FallOffAt = _timing.CurTime + victim.Comp.FallOffDelay;
        victim.Comp.BurstAt = _timing.CurTime + victim.Comp.BurstDelay;

        _appearance.SetData(victim, victim.Comp.HuggedLayer, true);
    }

    private void OnVictimHuggedRemoved(Entity<VictimHuggedComponent> victim, ref ComponentRemove args)
    {
        _blindable.UpdateIsBlind(victim.Owner);
        _standing.Stand(victim);
    }

    private void OnVictimHuggedCancel<T>(Entity<VictimHuggedComponent> victim, ref T args) where T : CancellableEntityEventArgs
    {
        if (victim.Comp.LifeStage <= ComponentLifeStage.Running && !victim.Comp.Recovered)
            args.Cancel();
    }

    private void OnVictimHuggedExamined(Entity<VictimHuggedComponent> victim, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner) || (CompOrNull<GhostComponent>(args.Examiner)?.CanGhostInteract ?? false))
            args.PushMarkup("This creature is impregnated.");
    }

    private void OnVictimHuggedRejuvenate(Entity<VictimHuggedComponent> victim, ref RejuvenateEvent args)
    {
        RemCompDeferred<VictimHuggedComponent>(victim);
    }

    private void OnVictimBurstMapInit(Entity<VictimBurstComponent> burst, ref MapInitEvent args)
    {
        _appearance.SetData(burst, burst.Comp.BurstLayer, true);

        if (TryComp(burst, out MobStateComponent? mobState))
            _mobState.UpdateMobState(burst, mobState);
    }

    private void OnVictimUpdateMobState(Entity<VictimBurstComponent> burst, ref UpdateMobStateEvent args)
    {
        args.State = MobState.Dead;
    }

    private void OnVictimBurstRejuvenate(Entity<VictimBurstComponent> burst, ref RejuvenateEvent args)
    {
        RemCompDeferred<VictimBurstComponent>(burst);
    }

    private bool StartHug(Entity<XenoHuggerComponent> hugger, EntityUid victim, EntityUid user)
    {
        if (!CanHugPopup(hugger, victim, user))
            return false;

        var ev = new AttachHuggerDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, hugger.Comp.ManualAttachDelay, ev, hugger, victim)
        {
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };
        _doAfter.TryStartDoAfter(doAfter);

        return true;
    }

    private bool CanHugPopup(Entity<XenoHuggerComponent> hugger, EntityUid victim, EntityUid user, bool popup = true, bool force = false)
    {
        if (!HasComp<HuggableComponent>(victim) ||
            HasComp<HuggerSpentComponent>(hugger) ||
            HasComp<VictimHuggedComponent>(victim))
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("cm-xeno-failed-cant-facehug", ("target", victim)), victim, user, PopupType.MediumCaution);

            return false;
        }

        if (!force &&
            TryComp(victim, out StandingStateComponent? standing) &&
            !_standing.IsDown(victim, standing))
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("cm-xeno-failed-cant-reach", ("target", victim)), victim, user, PopupType.MediumCaution);

            return false;
        }

        if (_mobState.IsDead(victim))
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("cm-xeno-failed-target-dead"), victim, user, PopupType.MediumCaution);

            return false;
        }

        return true;
    }

    public bool Hug(Entity<XenoHuggerComponent> hugger, EntityUid victim, bool popup = true, bool force = false)
    {
        if (!CanHugPopup(hugger, victim, hugger, popup, force))
            return false;

        if (_inventory.TryGetContainerSlotEnumerator(victim, out var slots, SlotFlags.MASK))
        {
            var any = false;
            while (slots.MoveNext(out var slot))
            {
                if (slot.ContainedEntity != null)
                {
                    _inventory.TryUnequip(victim, victim, slot.ID, force: true);
                    any = true;
                }
            }

            if (any && _net.IsServer)
            {
                _popup.PopupEntity(Loc.GetString("cm-xeno-facehug-success", ("target", victim)), victim);
            }
        }

        if (_net.IsServer &&
            TryComp(victim, out HuggableComponent? huggable) &&
            TryComp(victim, out HumanoidAppearanceComponent? appearance) &&
            huggable.Sound.TryGetValue(appearance.Sex, out var sound))
        {
            var filter = Filter.Pvs(victim);
            _audio.PlayEntity(sound, filter, victim, true);
        }

        var time = _timing.CurTime;
        var victimComp = EnsureComp<VictimHuggedComponent>(victim);
        victimComp.AttachedAt = time;
        victimComp.RecoverAt = time + hugger.Comp.ParalyzeTime;
        victimComp.Hive = CompOrNull<XenoComponent>(hugger)?.Hive ?? default;
        _stun.TryParalyze(victim, hugger.Comp.ParalyzeTime, true);

        var container = _container.EnsureContainer<ContainerSlot>(victim, victimComp.ContainerId);
        _container.Insert(hugger.Owner, container);

        _blindable.UpdateIsBlind(victim);
        _appearance.SetData(hugger, victimComp.HuggedLayer, true);

        // TODO CM14 also do damage to the hugger
        EnsureComp<HuggerSpentComponent>(hugger);

        HuggerLeapHit(hugger);
        return true;
    }

    public void RefreshIncubationMultipliers(Entity<VictimHuggedComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new GetHuggedIncubationMultiplierEvent(1);
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.IncubationMultiplier = ev.Multiplier;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<VictimHuggedComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var hugged, out var xform))
        {
            if (hugged.FallOffAt < time && !hugged.FellOff)
            {
                hugged.FellOff = true;
                _appearance.SetData(uid, hugged.HuggedLayer, false);
                if (_container.TryGetContainer(uid, hugged.ContainerId, out var container))
                    _container.EmptyContainer(container);
            }

            if (hugged.RecoverAt < time && !hugged.Recovered)
            {
                hugged.Recovered = true;
                _blindable.UpdateIsBlind(uid);
            }

            if (_net.IsClient)
                continue;

            if (hugged.BurstAt > time)
            {
                // TODO CM14 make this less effective against late-stage infections, also make this support faster incubation
                if (hugged.IncubationMultiplier < 1)
                    hugged.BurstAt += TimeSpan.FromSeconds(1 - hugged.IncubationMultiplier) * frameTime;

                // Stages
                // Percentage of how far along we out to burst time times the number of stages, truncated. You can't go back a stage once you've reached one
                int stage = Math.Max((int) ((hugged.BurstDelay - (hugged.BurstAt - time)) / hugged.BurstDelay * hugged.FinalStage), hugged.CurrentStage);
                hugged.CurrentStage = stage;
                // Symptoms only start after the IntialSymptomStart is passed (by default, 2)
                if(stage >= hugged.FinalSymptomsStart)
                {
                    if (_random.Prob(hugged.MajorPainChance * frameTime))
                    {
                        var message = Loc.GetString("cm-xeno-infection-majorpain-" + _random.Pick(new List<string> { "chest", "breathing", "heart" }));
                        _popup.PopupEntity(message, uid, uid, PopupType.MediumCaution);
                        if (_random.Prob(0.5f))
                        {
                            var ev = new VictimHuggedEmoteEvent(hugged.ScreamId);
                            RaiseLocalEvent(uid, ev);
                        }
                    }

                    if (_random.Prob(hugged.ShakesChance * frameTime))
                        InfectionShakes(uid, hugged, hugged.BaseKnockdownTime * 3);
                }
                else if (stage >= hugged.MiddlingSymptomsStart)
                {
                    if (_random.Prob(hugged.ThroatPainChance * frameTime))
                    {
                        var message = Loc.GetString("cm-xeno-infection-throat-" + _random.Pick(new List<string> { "sore", "mucous" }));
                        _popup.PopupEntity(message, uid, uid, PopupType.MediumCaution);
                    }
                    // TODO 20% chance to take limb damage
                    else if (_random.Prob(hugged.MuscleAcheChance * frameTime))
                    {
                        _popup.PopupEntity(Loc.GetString("cm-xeno-infection-muscle-ache"), uid, PopupType.MediumCaution);
                        if (_random.Prob(0.2f))
                            _damage.TryChangeDamage(uid, hugged.InfectionDamage, true, false);
                    }
                    else if (_random.Prob(hugged.SneezeCoughChance * frameTime))
                    {
                        var emote = _random.Pick(new List<ProtoId<EmotePrototype>> { hugged.SneezeId, hugged.CoughId });
                        var ev = new VictimHuggedEmoteEvent(emote);
                        RaiseLocalEvent(uid, ev);
                    }

                    if (_random.Prob((hugged.ShakesChance * 5 / 6) * frameTime))
                        InfectionShakes(uid, hugged, hugged.BaseKnockdownTime * 2);
                }
                else if (stage >= hugged.InitialSymptomsStart)
                {
                    if (_random.Prob(hugged.MinorPainChance * frameTime))
                    {
                        var message = Loc.GetString("cm-xeno-infection-minorpain-" + _random.Pick(new List<string> { "stomach", "chest" }));
                        _popup.PopupEntity(message, uid, uid, PopupType.MediumCaution);
                    }

                    if (_random.Prob((hugged.ShakesChance * 2 / 3) * frameTime))
                        InfectionShakes(uid, hugged, hugged.BaseKnockdownTime);
                }
                continue;
            }

            RemCompDeferred<VictimHuggedComponent>(uid);

            var spawned = SpawnAtPosition(hugged.BurstSpawn, xform.Coordinates);
            _xeno.SetHive(spawned, hugged.Hive);

            EnsureComp<VictimBurstComponent>(uid);

            _audio.PlayPvs(hugged.BurstSound, uid);
        }
    }
    // Shakes chances decrease as symptom stages progress, and they get longer
    private void InfectionShakes(EntityUid victim, VictimHuggedComponent hugged, TimeSpan knockdownTime)
    {
        //TODO Minor limb damage and causes pain
        _stun.TryParalyze(victim, knockdownTime, false);
        _jitter.DoJitter(victim, hugged.JitterTime, false);
        _popup.PopupEntity(Loc.GetString("cm-xeno-infection-shakes-self"), victim, victim, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("cm-xeno-infection-shakes", ("victim", victim)), victim, Filter.PvsExcept(victim), true, PopupType.LargeCaution);
        _damage.TryChangeDamage(victim, hugged.InfectionDamage, true, false);
    }
}
