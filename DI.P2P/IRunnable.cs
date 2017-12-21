namespace DI.P2P
{
    using System.Threading.Tasks;

    public interface IRunnable
    {
        Task Start();
        Task Stop();
        bool IsRunning { get; }
    }
}
