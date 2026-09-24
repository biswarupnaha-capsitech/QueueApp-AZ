using Azure;
using Azure.Storage;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace QueueApp;

class Program
{
    static readonly string devstorageAccountName =
        Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME") ?? "devstoreaccount1";

    static readonly string devqueueName =
        Environment.GetEnvironmentVariable("AZURE_STORAGE_QUEUE_NAME") ?? "queueapp-queue";

    static readonly string flociEndpoint =
        Environment.GetEnvironmentVariable("FLOCI_AZ_ENDPOINT") ?? "http://localhost:4577";

    const string DevAccountKey =
        "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

    public static async Task Main(string[] args)
    {
        Console.WriteLine("QueueApp is booting...");
        QueueClient queue;
        try
        {
            var options = new QueueClientOptions(QueueClientOptions.ServiceVersion.V2024_11_04);

            var queueUri = new Uri($"{flociEndpoint}/{devstorageAccountName}-queue/{devqueueName}");
            queue = new QueueClient(
                queueUri,
                new StorageSharedKeyCredential(devstorageAccountName, DevAccountKey),
                options);

            if (queue is null)
            {
                Console.WriteLine("Queue client is null. Please check the connection string and queue name.");
                return;
            }
            else
            {
                Console.WriteLine($"Queue client {queue.AccountName} is connected.");
            }

            Console.WriteLine($"Storage Account : {devstorageAccountName}");
            Console.WriteLine($"Queue Name      : {devqueueName}");
            Console.WriteLine($"Floci Endpoint  : {flociEndpoint}");
            Console.WriteLine($"Queue URI       : {queueUri}");
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "QueueNotFound")
        {
            Console.WriteLine("The queue does not exist. Add a message to create the queue and store the message.");
            return;
        }

        if (args.Length > 0)
        {
            string value = String.Join(" ", args);
            await InsertMessageDevAsync(queue, value);
            Console.WriteLine($"Sent: {value}");
        }
        else
        {
            string value = await RetrieveNextMessageDevAsync(queue);
            Console.WriteLine($"Received: {value}");
        }

        Console.Write("Press Enter...");
        Console.ReadLine();
    }



    /// <summary>
    /// Inserts a message into the specified queue. If the queue does not exist, it will attempt to create it first.
    /// </summary>
    /// <param name="theQueue"></param>
    /// <param name="newMessage"></param>
    /// <param name="mustExpire"></param>
    /// <param name="timeToLive"></param>
    /// <returns></returns>
    private static async Task InsertMessageDevAsync(QueueClient theQueue, string newMessage, bool mustExpire = true, TimeSpan? timeToLive = null)
    {
        string? result = await CreateQueue(theQueue);
        if (!string.IsNullOrEmpty(result))
            Console.WriteLine(result);
        else
            return;

        if (mustExpire)
            if (timeToLive is null)
                await theQueue.SendMessageAsync(newMessage);
            else
                await theQueue.SendMessageAsync(newMessage, timeToLive: timeToLive);
        else
            await theQueue.SendMessageAsync(newMessage, default, TimeSpan.FromSeconds(-1), default);
    }



    /// <summary>
    /// Retrieves the next message from the specified queue. If the queue is empty, it will attempt to delete the queue.
    /// </summary>
    /// <param name="theQueue"></param>
    /// <returns></returns>
    private static async Task<string> RetrieveNextMessageDevAsync(QueueClient theQueue)
    {
        try
        {
            QueueMessage[] messages = await theQueue.ReceiveMessagesAsync(1);

            if (messages.Length == 0)
                return await DeleteQueue(theQueue);

            QueueMessage msg = messages[0];
            await theQueue.DeleteMessageAsync(msg.MessageId, msg.PopReceipt);
            return msg.Body.ToString();
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "QueueNotFound")
        {
            return "The queue does not exist. Add a message to create the queue and store the message.";
        }
    }


    /// <summary>
    /// Creates the specified queue if it does not already exist. Prompts the user for confirmation before creating the queue.
    /// </summary>
    /// <param name="theQueue"></param>
    /// <returns></returns>
    private static async Task<string?> CreateQueue(QueueClient theQueue)
    {
        try
        {
            if (await theQueue.ExistsAsync())
                return $"The queue {theQueue.Name} exists.";

            Console.Write("Queue does not exist. Attempt to create it? (Y/N) ");
            string? response = Console.ReadLine();

            if (response?.ToUpper() != "Y")
                return "The operation was cancelled.";


            await theQueue.CreateAsync();

            return "The queue was created.";
        }
        catch (RequestFailedException ex)
        {
            return $"Error creating queue: {ex.Message}";
        }
    }


    /// <summary>
    /// Deletes the specified queue if it is empty. Prompts the user for confirmation before deleting the queue.
    /// </summary>
    /// <param name="theQueue"></param>
    /// <returns></returns>
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