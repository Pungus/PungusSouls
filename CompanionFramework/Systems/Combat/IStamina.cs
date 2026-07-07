namespace Systems.Combat
{
    public interface IStamina
    {
        float Current { get; }
        float Max { get; }

        bool Has(float amount);
        void Consume(float amount);
    }
}