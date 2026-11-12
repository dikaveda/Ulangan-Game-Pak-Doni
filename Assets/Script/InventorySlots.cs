using System;

[Serializable]
public class InventorySlot
{
    public ItemDetails itemDetails;
    public int stackSize;

    public InventorySlot(ItemDetails newItem, int amount)
    {
        itemDetails = newItem;
        stackSize = amount;
    }

    public void AddStack(int amount)
    {
        stackSize += amount;
    }

    public void RemoveStack(int amount)
    {
        stackSize -= amount;
    }

    public void ClearSlot()
    {
        itemDetails = null;
        stackSize = 0;
    }
}