using Azure.Storage.Queues;

namespace QueueApp;

class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("QueueApp is running...");
    }



    /// <summary>
    /// Inserts a message into the specified Azure Storage Queue. If the queue does not exist, it will be created.
    /// The message can be set to expire after a specified time-to-live (TTL) duration, or it can be set to never expire.
    /// A queue message must be in a format compatible with an XML request using UTF-8 encoding.
    /// A message may be up to 64 KB in size. If a message contains binary data, Base64-encode the message.
    /// </summary>
    /// <param name="theQueue">The Azure Storage Queue client.</param>
    /// <param name="newMessage">The message to insert.</param>
    /// <param name="mustExpire">By default messages will expire</param>
    /// <param name="timeToLive">By default TTL is 7 days</param>
    /// <returns></returns>
    private static async Task InsertMessageAsync(QueueClient theQueue, string newMessage, bool mustExpire = true, TimeSpan? timeToLive = null)
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