namespace Core.Agent
{
    public class TaskExecutor : ITaskExecutor
    {
        public void Execute(ITask task, float dt)
        {
            task?.Update(dt);
        }
    }
}