using NyxGui;
using Sandbox.Items;

namespace Sandbox.UI.Inventory;

internal sealed class InventorySlotView
{
    public InventorySlotView(EquipmentSlot slot, NyxContainer frame, UISlot uiSlot)
    {
        Slot = slot;
        Frame = frame;
        UiSlot = uiSlot;
    }

    public EquipmentSlot Slot { get; }

    public NyxContainer Frame { get; }

    public UISlot UiSlot { get; }

    public void BindEquipment(PlayerEquipment equipment)
    {
        UiSlot.BoundEquipmentSlot = Slot;
        UiSlot.ReadItem = () => equipment[Slot];
        UiSlot.WriteItem = item =>
        {
            if (!equipment.TryEquip(Slot, item))
                UiSlot.Refresh();
        };
    }

    public void Refresh() => UiSlot.Refresh();
}
