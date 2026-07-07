using UnityEngine;

namespace Systems.Combat
{
    public class StaminaSystem : IStamina
    {
        public float Current { get; private set; } = 100f;
        public float Max { get; private set; } = 100f;

        public bool Has(float amount) => Current >= amount;

        public void Consume(float amount)
        {
            Current = Mathf.Max(0, Current - amount);
        }
    }
}