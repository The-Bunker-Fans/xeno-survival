#nullable enable
using Content.Server.Storage.Components;
using Content.Shared.Body.Components;
using Content.Shared.Directions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Morgue;
using Content.Shared.Notification.Managers;
using Content.Shared.Physics;
using Content.Shared.Sound;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class MorgueEntityStorageComponent : EntityStorageComponent, IExamine
    {
        public override string Name => "MorgueEntityStorage";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("trayPrototype")]
        private string? _trayPrototypeId;

        [ViewVariables]
        private IEntity? _tray;

        [ViewVariables]
        public ContainerSlot? TrayContainer { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("doSoulBeep")]
        public bool DoSoulBeep = true;

        [DataField("occupantHasSoulAlarmSound")]
        private SoundSpecifier _occupantHasSoulAlarmSound = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");

        [ViewVariables]
        [ComponentDependency] protected readonly AppearanceComponent? Appearance = null;

        protected override void Initialize()
        {
            base.Initialize();
            Appearance?.SetData(MorgueVisuals.Open, false);
            TrayContainer = Owner.EnsureContainer<ContainerSlot>("morgue_tray", out _);
        }

        public override Vector2 ContentsDumpPosition()
        {
            if (_tray != null)
                return _tray.Transform.WorldPosition;
            return base.ContentsDumpPosition();
        }

        protected override bool AddToContents(IEntity entity)
        {
            if (entity.HasComponent<SharedBodyComponent>() && !EntitySystem.Get<StandingStateSystem>().IsDown(entity))
                return false;
            return base.AddToContents(entity);
        }

        public override bool CanOpen(IEntity user, bool silent = false)
        {
            if (!Owner.InRangeUnobstructed(
                Owner.Transform.Coordinates.Offset(Owner.Transform.LocalRotation.GetCardinalDir()),
                collisionMask: CollisionGroup.Impassable | CollisionGroup.VaultImpassable
            ))
            {
                if (!silent)
                    Owner.PopupMessage(user, Loc.GetString("morgue-entity-storage-component-cannot-open-no-space"));
                return false;
            }

            return base.CanOpen(user, silent);
        }

        protected override void OpenStorage()
        {
            Appearance?.SetData(MorgueVisuals.Open, true);
            Appearance?.SetData(MorgueVisuals.HasContents, false);
            Appearance?.SetData(MorgueVisuals.HasMob, false);
            Appearance?.SetData(MorgueVisuals.HasSoul, false);

            if (_tray == null)
            {
                _tray = Owner.EntityManager.SpawnEntity(_trayPrototypeId, Owner.Transform.Coordinates);
                var trayComp = _tray.EnsureComponent<MorgueTrayComponent>();
                trayComp.Morgue = Owner;
            }
            else
            {
                TrayContainer?.Remove(_tray);
            }

            _tray.Transform.WorldPosition = Owner.Transform.WorldPosition + Owner.Transform.LocalRotation.GetCardinalDir().ToVec();
            _tray.Transform.AttachParent(Owner);

            base.OpenStorage();
        }

        private void CheckContents()
        {
            var count = 0;
            var hasMob = false;
            var hasSoul = false;
            foreach (var entity in Contents.ContainedEntities)
            {
                count++;
                if (!hasMob && entity.HasComponent<SharedBodyComponent>())
                    hasMob = true;
                if (!hasSoul && entity.TryGetComponent<ActorComponent>(out var actor) && actor.PlayerSession != null)
                    hasSoul = true;
            }
            Appearance?.SetData(MorgueVisuals.HasContents, count > 0);
            Appearance?.SetData(MorgueVisuals.HasMob, hasMob);
            Appearance?.SetData(MorgueVisuals.HasSoul, hasSoul);
        }

        protected override void CloseStorage()
        {
            base.CloseStorage();

            Appearance?.SetData(MorgueVisuals.Open, false);
            CheckContents();

            if (_tray != null)
            {
                TrayContainer?.Insert(_tray);
            }
        }

        //Called every 10 seconds
        public void Update()
        {
            CheckContents();

            if (DoSoulBeep && Appearance != null && Appearance.TryGetData(MorgueVisuals.HasSoul, out bool hasSoul) && hasSoul &&
                _occupantHasSoulAlarmSound.TryGetSound(out var occupantHasSoulAlarmSound))
            {
                SoundSystem.Play(Filter.Pvs(Owner), occupantHasSoulAlarmSound, Owner);
            }                
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (Appearance == null) return;

            if (inDetailsRange)
            {
                if (Appearance.TryGetData(MorgueVisuals.HasSoul, out bool hasSoul) && hasSoul)
                {
                    message.AddMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-body-has-soul"));
                }
                else if (Appearance.TryGetData(MorgueVisuals.HasMob, out bool hasMob) && hasMob)
                {
                    message.AddMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-body-has-no-soul"));
                }
                else if (Appearance.TryGetData(MorgueVisuals.HasContents, out bool hasContents) && hasContents)
                {
                    message.AddMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-has-contents"));
                }
                else
                {
                    message.AddMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-empty"));
                }
            }
        }
    }
}
