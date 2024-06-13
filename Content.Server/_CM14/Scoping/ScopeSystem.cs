﻿using System.Numerics;
using Content.Server.Movement.Systems;
using Content.Shared._CM14.Scoping;
using Content.Shared.Camera;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._CM14.Scoping;

public sealed class ScopeSystem : SharedScopeSystem
{
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ContentEyeSystem _contentEye = default!;
    [Dependency] private readonly EyeSystem _eye = default!;

    protected override void StartScopingCamera(EntityUid user, ScopeComponent scopeComponent)
    {
        var xform = Transform(user);
        if (!HasComp<CameraRecoilComponent>(user))
            return;

        const float smallestViewpointSize = 15;

        var cardinalVector = xform.LocalRotation.GetCardinalDir().ToVec();
        var targetOffset = cardinalVector * ((smallestViewpointSize * scopeComponent.Zoom - 1) / 2);

        _eye.SetOffset(user, targetOffset); // apparently this is required for aim to work properly
        _contentEye.SetZoom(user, Vector2.One * scopeComponent.Zoom, true);

        var scopeToggleEvent = new CMScopeToggleEvent(GetNetEntity(user), targetOffset);
        RaiseNetworkEvent(scopeToggleEvent, user);

        if (TryComp(user, out ActorComponent? actorComp))
        {
            // add cardinal vector, until better pvs handling is introduced here
            var loaderId = Spawn(scopeComponent.PvsLoaderProto, _transformSystem.GetMapCoordinates(xform).Offset(targetOffset + cardinalVector * 2));
            scopeComponent.PvsLoader = loaderId;
            _viewSubscriber.AddViewSubscriber(loaderId, actorComp.PlayerSession);
        }
    }

    protected override void StopScopingCamera(EntityUid user, ScopeComponent scopeComponent)
    {
        if (!HasComp<CameraRecoilComponent>(user))
            return;

        _eye.SetOffset(user, Vector2.Zero);
        _contentEye.ResetZoom(user);

        var scopeToggleEvent = new CMScopeToggleEvent(GetNetEntity(user), Vector2.Zero);
        RaiseNetworkEvent(scopeToggleEvent, user);

        Del(scopeComponent.PvsLoader);
        scopeComponent.PvsLoader = null;
    }
}
