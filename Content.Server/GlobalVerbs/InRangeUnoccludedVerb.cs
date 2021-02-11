﻿using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Utility;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GlobalVerbs
{
    [GlobalVerb]
    public class InRangeUnoccludedVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Invisible;

            if (!user.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            var groupController = IoCManager.Resolve<IConGroupController>();
            if (!groupController.CanCommand(actor.playerSession, "inrangeunoccluded"))
            {
                return;
            }

            data.Visibility = VerbVisibility.Visible;
            data.Text = Loc.GetString("In Range Unoccluded");
            data.CategoryData = VerbCategories.Debug;
        }

        public override void Activate(IEntity user, IEntity target)
        {
            if (!user.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            var groupController = IoCManager.Resolve<IConGroupController>();
            if (!groupController.CanCommand(actor.playerSession, "inrangeunoccluded"))
            {
                return;
            }

            var message = user.InRangeUnOccluded(target)
                ? Loc.GetString("Not occluded")
                : Loc.GetString("Occluded");

            target.PopupMessage(user, message);
        }
    }
}
