using Azure.Storage.Queues;

namespace QueueApp;

class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("QueueApp is running...");
    }

    static async Task InsertMessageAsync(QueueClient theQueue, string newMessage, bool mustExpire = true, TimeSpan? timeToLive = null)
    {
        if (null != await theQueue.CreateIfNotExistsAsync())
        {
            Console.WriteLine("The queue was created.");
        }

        if (mustExpire)
            if (timeToLive is null)
                await theQueue.SendMessageAsync(newMessage);
            else
                await theQueue.SendMessageAsync(newMessage, timeToLive: timeToLive);
        else
            await theQueue.SendMessageAsync(newMessage, default, TimeSpan.FromSeconds(-1), default);
    }
}