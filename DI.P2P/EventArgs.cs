namespace DI.P2P
{
    public class EventArgs<T>
    {
        public EventArgs(T data)
        {
            Data = data;
        }

        public T Data { get; set; }
    }
}
