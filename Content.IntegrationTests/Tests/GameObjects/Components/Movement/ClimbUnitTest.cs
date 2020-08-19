﻿#nullable enable

using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.Physics;
using Robust.Shared.GameObjects.Components;

namespace Content.IntegrationTests.Tests.GameObjects.Components.Movement
{
    [TestFixture]
    [TestOf(typeof(ClimbableComponent))]
    [TestOf(typeof(ClimbingComponent))]
    public class ClimbUnitTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();

            IEntity human;
            IEntity table;
            IEntity carpet;
            ClimbableComponent climbable;
            ClimbingComponent climbing;

            server.Assert(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                // Spawn the entities
                human = entityManager.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);
                table = entityManager.SpawnEntity("Table", MapCoordinates.Nullspace);

                // Test for climb components existing
                // Players and tables should have these in their prototypes.
                Assert.True(human.TryGetComponent(out climbing), "Human has no climbing");
                Assert.True(table.TryGetComponent(out climbable), "Table has no climbable");

                // Now let's make the player enter a climbing transitioning state.
                climbing.IsClimbing = true;
                climbing.TryMoveTo(human.Transform.WorldPosition, table.Transform.WorldPosition);
                human.TryGetComponent(out ICollidableComponent body);

                Assert.True(body.HasController<ClimbController>(), "Player has no ClimbController");

                // Force the player out of climb state. It should immediately remove the ClimbController.
                climbing.IsClimbing = false;

                Assert.True(!body.HasController<ClimbController>(), "Player wrongly has a ClimbController");

            });

            await server.WaitIdleAsync();
        }
    }
}
