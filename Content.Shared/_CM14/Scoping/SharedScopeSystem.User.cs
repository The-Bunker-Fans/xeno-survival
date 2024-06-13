﻿using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Events;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Shared._CM14.Scoping;

public abstract partial class SharedScopeSystem
{
    public void InitializeUser()
    {
        SubscribeLocalEvent<ScopeUserComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<ScopeUserComponent, PullStartedMessage>(OnPullStarted);
        SubscribeLocalEvent<ScopeUserComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<ScopeUserComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<ScopeUserComponent, EntityTerminatingEvent>(OnEntityTerminating);

        SubscribeLocalEvent<ScopeUserComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnMoveInput(Entity<ScopeUserComponent> ent, ref MoveInputEvent args)
    {
        UserStopScoping(ent.Owner, ent.Comp);
    }

    private void OnPullStarted(Entity<ScopeUserComponent> ent, ref PullStartedMessage args)
    {
        if(args.PulledUid != ent.Owner)
            return;

        UserStopScoping(ent.Owner, ent.Comp);
    }

    private void OnParentChanged(Entity<ScopeUserComponent> ent, ref EntParentChangedMessage args)
    {
        UserStopScoping(ent.Owner, ent.Comp);
    }

    private void OnInsertAttempt(Entity<ScopeUserComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        UserStopScoping(ent.Owner, ent.Comp);
    }

    private void OnEntityTerminating(Entity<ScopeUserComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!TryComp(ent.Comp.ScopingItem, out ScopeComponent? scopeComponent))
            return;

        StopScopingHelper(ent.Comp.ScopingItem.Value, scopeComponent, ent.Owner);
    }

    private void OnPlayerDetached(Entity<ScopeUserComponent> ent, ref PlayerDetachedEvent args)
    {
        UserStopScoping(ent.Owner, ent.Comp);
    }

    private void UserStopScoping(EntityUid uid, ScopeUserComponent component)
    {
        if (TryComp(component.ScopingItem, out ScopeComponent? scopeComponent) && scopeComponent.IsScoping)
            StopScoping(component.ScopingItem.Value, scopeComponent, uid);
    }
}
