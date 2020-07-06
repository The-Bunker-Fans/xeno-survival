﻿// Hey look,
// Antag Datums.

using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Server.Mobs
{
    /// <summary>
    ///     The Role is a basic building block for,
    ///     well, IC roles.
    ///     This can be anything and is not necessarily limited to antagonists.
    /// </summary>
    public abstract class Role
    {
        /// <summary>
        ///     The mind owning this role instance.
        /// </summary>
        public Mind Mind { get; }

        /// <summary>
        ///     A friendly name for this role type.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        ///     Whether this role should be considered antagonistic or not.
        /// </summary>
        public abstract bool Antagonist { get; }

        protected Role(Mind mind)
        {
            Mind = mind;
        }

        /// <summary>
        ///     Called when a mind (player) first gets this role, to greet them.
        /// </summary>
        public virtual void Greet()
        {

        }
    }
}
