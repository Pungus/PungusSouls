namespace Core.Agent
{
    public interface ITaskExecutor
    {
        void Execute(ITask task, float dt);
    }
}