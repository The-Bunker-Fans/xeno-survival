using Content.Shared.Item;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;


namespace Content.Shared._CM14.Attachable;

public sealed class SharedAttachableSizeModifierSystem : EntitySystem
{
    private readonly List<ItemSizePrototype> _sortedSizes = new List<ItemSizePrototype>();
    
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    
    
    public override void Initialize()
    {
        _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;
        
        SubscribeLocalEvent<AttachableSizeModifierComponent, AttachableAlteredEvent>(OnAttachableAltered);
        SubscribeLocalEvent<AttachableToggleableSizeModifierComponent, AttachableAlteredEvent>(OnAttachableAltered);
        InitItemSizes();
    }
    
    
    private void OnAttachableAltered(Entity<AttachableSizeModifierComponent> attachable, ref AttachableAlteredEvent args)
    {
        if(attachable.Comp.SizeModifier == 0)
            return;
        
        if(!_entityManager.TryGetComponent<ItemComponent>(args.HolderUid, out ItemComponent? itemComponent))
            return;
        
        switch(args.Alteration)
        {
            case AttachableAlteredType.Attached:
                IncrementSize(args.HolderUid, itemComponent, attachable.Comp.SizeModifier, out attachable.Comp.ResetIncrement);
                return;
            
            case AttachableAlteredType.Detached:
                ResetSize(args.HolderUid, itemComponent, attachable.Comp.ResetIncrement);
                return;
        }
    }
    
    private void OnAttachableAltered(Entity<AttachableToggleableSizeModifierComponent> attachable, ref AttachableAlteredEvent args)
    {
        if(attachable.Comp.ActiveSizeModifier == 0 && attachable.Comp.InactiveSizeModifier == 0)
            return;
        
        if(!_entityManager.TryGetComponent<ItemComponent>(args.HolderUid, out ItemComponent? itemComponent))
            return;
        
        if(!_entityManager.TryGetComponent<AttachableToggleableComponent>(attachable.Owner, out AttachableToggleableComponent? toggleableComponent))
            return;
        
        switch(args.Alteration)
        {
            case AttachableAlteredType.Attached:
                if(toggleableComponent.Active)
                {
                    IncrementSize(args.HolderUid, itemComponent, attachable.Comp.ActiveSizeModifier, out attachable.Comp.ResetIncrement);
                    return;
                }
                IncrementSize(args.HolderUid, itemComponent, attachable.Comp.InactiveSizeModifier, out attachable.Comp.ResetIncrement);
                return;
            
            case AttachableAlteredType.Detached:
                ResetSize(args.HolderUid, itemComponent, attachable.Comp.ResetIncrement);
                return;
            
            case AttachableAlteredType.Activated:
                ResetSize(args.HolderUid, itemComponent, attachable.Comp.ResetIncrement);
                IncrementSize(args.HolderUid, itemComponent, attachable.Comp.ActiveSizeModifier, out attachable.Comp.ResetIncrement);
                return;
            
            case AttachableAlteredType.Deactivated:
                ResetSize(args.HolderUid, itemComponent, attachable.Comp.ResetIncrement);
                IncrementSize(args.HolderUid, itemComponent, attachable.Comp.InactiveSizeModifier, out attachable.Comp.ResetIncrement);
                return;
        }
    }
    
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if(args.ByType.ContainsKey(typeof(ItemSizePrototype)) || (args.Removed?.ContainsKey(typeof(ItemSizePrototype)) ?? false))
            InitItemSizes();
    }
    
    private void InitItemSizes()
    {
        _sortedSizes.Clear();
        _sortedSizes.AddRange(_prototypeManager.EnumeratePrototypes<ItemSizePrototype>());
        _sortedSizes.Sort();
    }
    
    private void IncrementSize(EntityUid holderUid, ItemComponent itemComponent, int sizeIncrement, out int resetIncrement)
    {
        if(TryGetIncrementedSize(itemComponent.Size, sizeIncrement, out ProtoId<ItemSizePrototype>? newSize, out resetIncrement))
            _itemSystem.SetSize(holderUid, newSize.Value, itemComponent);
    }
    
    private void ResetSize(EntityUid holderUid, ItemComponent itemComponent, int resetIncrement)
    {
        if(TryGetIncrementedSize(itemComponent.Size, resetIncrement, out ProtoId<ItemSizePrototype>? newSize, out _))
            _itemSystem.SetSize(holderUid, newSize.Value, itemComponent);
    }
    
    private bool TryGetIncrementedSize(ProtoId<ItemSizePrototype> size, int increment, [NotNullWhen(true)] out ProtoId<ItemSizePrototype>? newSize, [NotNullWhen(true)] out int resetIncrement)
    {
        int sizeIndex = -1;
        
        for(int index = 0; index < _sortedSizes.Count; index++)
        {
            if(size.ToString().Equals(_sortedSizes[index].ID))
            {
                sizeIndex = index;
                break;
            }
        }
        
        if(sizeIndex == -1)
        {
            resetIncrement = 0;
            newSize = null;
            return false;
        }
        
        int newSizeIndex = MathHelper.Clamp(sizeIndex + increment, 0, _sortedSizes.Count - 1);
        resetIncrement = sizeIndex - newSizeIndex;
        
        newSize = _sortedSizes[newSizeIndex];
        return true;
    }
}
