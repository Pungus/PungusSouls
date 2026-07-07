namespace Core.Agent
{
    public interface ITask
    {
        bool IsComplete { get; }
        void Start();
        void Update(float dt);
    }
}