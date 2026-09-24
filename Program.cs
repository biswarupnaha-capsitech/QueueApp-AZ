using Azure;
using Azure.Identity;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace QueueApp;

class Program
{
    static readonly string storageAccountName = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME") ??
        throw new InvalidOperationException("AZURE_STORAGE_ACCOUNT_NAME environment variable is not set.");

    static readonly string queueName = Environment.GetEnvironmentVariable("AZURE_STORAGE_QUEUE_NAME") ??
        throw new InvalidOperationException("AZURE_STORAGE_QUEUE_NAME environment variable is not set.");

    public static async Task Main(string[] args)
    {
        Console.WriteLine("QueueApp is running...");

        QueueClient queue = new(new Uri($"https://{storageAccountName}.queue.core.windows.net/{queueName}"), new DefaultAzureCredential());

        if (args.Length > 0)
        {
            string value = String.Join(" ", args);
            await InsertMessageAsync(queue, value);
            Console.WriteLine($"Sent: {value}");
        }
        else
        {
            string value = await RetrieveNextMessageAsync(queue);
            Console.WriteLine($"Received: {value}");
        }

        Console.Write("Press Enter...");
        Console.ReadLine();
    }



    /// <summary>
    /// Enqueue a message into the specified Azure Storage Queue. If the queue does not exist, it will be created.
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
        Console.WriteLine("Checking if the queue exists...");
        string? result = await CreateQueue(theQueue);
        if (!string.IsNullOrEmpty(result))
            Console.WriteLine(result);

        if (mustExpire)
            if (timeToLive is null)
                await theQueue.SendMessageAsync(newMessage);
            else
                await theQueue.SendMessageAsync(newMessage, timeToLive: timeToLive);
        else
            await theQueue.SendMessageAsync(newMessage, default, TimeSpan.FromSeconds(-1), default);
    }


    /// <summary>
    /// Dequeue the next message from the specified Azure Storage Queue. If a message is retrieved, it will be deleted from the queue. 
    /// Also, if the queue is empty, the user will be prompted to delete the queue.
    /// </summary>
    /// <param name="theQueue">The Azure Storage Queue client.</param>
    /// <returns>The retrieved message, or null if no messages are available.</returns>
    private static async Task<string> RetrieveNextMessageAsync(QueueClient theQueue)
    {
        if (await theQueue.ExistsAsync())
        {
            QueueProperties properties = await theQueue.GetPropertiesAsync();

            if (properties.ApproximateMessagesCount > 0)
            {
                QueueMessage[] retrievedMessage = await theQueue.ReceiveMessagesAsync(1);
                string theMessage = retrievedMessage[0].Body.ToString();
                await theQueue.DeleteMessageAsync(retrievedMessage[0].MessageId, retrievedMessage[0].PopReceipt);
                return theMessage;
            }
            else
                return await DeleteQueue(theQueue);
        }
        else
        {
            return "The queue does not exist. Add a message to create the queue and store the message.";
        }
    }

    private static async Task<string?> CreateQueue(QueueClient theQueue)
    {
        Console.Write("Attempt to create a new queue? (Y/N) ");
        string response = Console.ReadLine()!;

        try
        {
            if (response?.ToUpper() == "Y")
            {
                if (null != await theQueue.CreateIfNotExistsAsync())
                {
                    return "The queue was created.";
                }
                else
                    return null;
            }
            else
            {
                return "The operation was cancelled.";
            }
        }
        catch (RequestFailedException ex)
        {
            return $"Error creating the queue: {ex.Message}";
        }
    }

    private static async Task<string> DeleteQueue(QueueClient theQueue)
    {
        Console.Write("The queue is empty. Attempt to delete it? (Y/N) ");
        string response = Console.ReadLine()!;

        try
        {
            if (response?.ToUpper() == "Y")
            {
                await theQueue.DeleteIfExistsAsync();
                return "The queue was deleted.";
            }
            else
            {
                return "The operation was cancelled.";
            }
        }
        catch (RequestFailedException ex)
        {
            return $"Error deleting the queue: {ex.Message}";
        }
    }
}