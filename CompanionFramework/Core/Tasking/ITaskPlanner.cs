namespace Core.Agent
{
    public interface ITaskPlanner
    {
        ITask Select(AgentContext context);
    }
}