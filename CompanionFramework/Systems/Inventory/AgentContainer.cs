using Systems.Interaction;

namespace Systems.Inventory
{
    public class AgentContainer : IInteractable
    {
        private readonly AgentInventory _inventory;
        private readonly Container _container;

        public AgentContainer(Container container)
        {
            _container = container;
        }


        public string GetPrompt()
        {
            return "Open Inventory";
        }

        public bool Interact()
        {
            if (_container == null)
                return false;

            InventoryGui.instance.Show(_container);
            return true;
        }


        public AgentInventory GetInventory() => _inventory;
    }
}
