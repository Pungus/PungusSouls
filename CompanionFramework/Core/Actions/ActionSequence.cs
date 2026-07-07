using System.Collections.Generic;

namespace Core.Actions
{
    public class ActionSequence
    {
        private readonly Queue<IAction> _actions;

        public ActionSequence(IEnumerable<IAction> actions)
        {
            _actions = new Queue<IAction>(actions);
        }

        public bool Run(float dt)
        {
            if (_actions.Count == 0) return true;

            if (_actions.Peek().Execute(dt))
                _actions.Dequeue();

            return _actions.Count == 0;
        }
    }
}