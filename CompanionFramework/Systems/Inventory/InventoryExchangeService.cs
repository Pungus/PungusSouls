namespace Systems.Inventory
{
    public class InventoryExchangeService
    {
        public bool MoveItem(
            AgentInventory from,
            AgentInventory to,
            string item,
            int amount)
        {
            if (!from.Remove(item, amount))
                return false;

            to.Add(item, amount);
            return true;
        }
    }
}