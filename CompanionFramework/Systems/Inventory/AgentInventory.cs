using ItemManager;
using System.Collections.Generic;
using System.Linq;

namespace Systems.Inventory
{
    public class AgentInventory
    {
        private readonly List<Item> _items = new List<Item>();

        public IEnumerable<Item> Items => _items;

        public bool Add(string name, int amount)
        {
            var existing = _items.FirstOrDefault(i => i.Name == name);

            if (existing != null)
            {
                existing.Stack += amount;
            }
            else
            {
                _items.Add(new Item
                {
                    Name = name,
                    Stack = amount
                });
            }

            return true;
        }

        public bool Remove(string name, int amount)
        {
            var item = _items.FirstOrDefault(i => i.Name == name);

            if (item == null || item.Stack < amount)
                return false;

            item.Stack -= amount;

            if (item.Stack <= 0)
                _items.Remove(item);

            return true;
        }
    }
}