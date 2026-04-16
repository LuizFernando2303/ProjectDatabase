using System.Threading.Channels;

namespace DatabaseController.Channels
{
    public class ObjectQueue
    {
        public Channel<Models.ModelObject> Queue { get; } = 
            Channel.CreateBounded<Models.ModelObject>(new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.Wait, // Wait when full
            });
    }
}
