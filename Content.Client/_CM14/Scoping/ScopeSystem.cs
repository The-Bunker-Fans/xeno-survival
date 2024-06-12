﻿using System.Numerics;
using Content.Client.Movement.Systems;
using Content.Shared._CM14.Scoping;
using Content.Shared.Camera;

namespace Content.Client._CM14.Scoping;

public sealed class ScopeSystem : SharedScopeSystem
{
    [Dependency] private readonly ContentEyeSystem _eye = default!;

    protected override void StartScopingCamera(EntityUid user, ScopeComponent scopeComponent)
    {
    }

    protected override void StopScopingCamera(EntityUid user, ScopeComponent scopeComponent)
    {
        if (TryComp<CameraRecoilComponent>(user, out var cameraRecoilComponent))
        {
            _eye.ResetZoom(user);
            cameraRecoilComponent.BaseOffset = Vector2.Zero;
        }
    }
}
