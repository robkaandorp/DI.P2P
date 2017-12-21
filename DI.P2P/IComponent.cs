namespace DI.P2P
{
    public interface IComponent : IRunnable
    {
        Module Owner { get; }
    }
}
