# QueueApp

QueueApp is a .NET console application that demonstrates Azure Storage Queue integration using the Azure Storage Queues SDK against a local Azure Storage-compatible environment provided by Floci.

The project is intended to be runnable locally without requiring an Azure subscription or an externally provisioned Azure Storage Queue.

The repository includes a `docker-compose.yml` configuration that provisions the local Azure-compatible storage environment required by the application.

## Architecture

The application consists of two components:

```text
┌──────────────────────┐
│      QueueApp        │
│   .NET Application   │
└──────────┬───────────┘
           │
           │ Azure Storage Queue SDK
           │
           ▼
┌──────────────────────┐
│        Floci         │
│ Local Azure Storage  │
│   compatible API     │
└──────────────────────┘
           │
           ▼
     Azure Queue
```

QueueApp uses the standard Azure Storage SDK:

```text
Azure.Storage.Queues
```

The application does not require the Azure CLI or an Azure Storage account in the cloud.

---

# Prerequisites

Install the following before running the project.

## 1. .NET SDK

The project requires the .NET SDK version targeted by the project file.

Verify the installed version:

```bash
dotnet --version
```

Verify the installed SDKs:

```bash
dotnet --list-sdks
```

## 2. Docker

Docker is required to run the local Azure-compatible storage environment.

Verify Docker:

```bash
docker --version
```

Verify Docker Compose:

```bash
docker compose version
```

Docker Desktop can also be used on Windows.

---

# Project Structure

A typical project structure is:

```text
QueueApp/
│
├── QueueApp.csproj
├── Program.cs
├── docker-compose.yml
└── README.md
```

The important files are:

| File                 | Purpose                                                       |
| -------------------- | ------------------------------------------------------------- |
| `Program.cs`         | Queue client, queue creation, message insertion and retrieval |
| `QueueApp.csproj`    | .NET project configuration and NuGet dependencies             |
| `docker-compose.yml` | Starts the local Floci Azure-compatible storage service       |
| `README.md`          | Project documentation                                         |

---

# Azure Storage Configuration

QueueApp uses the following configuration values.

| Variable                     | Default                 | Description              |
| ---------------------------- | ----------------------- | ------------------------ |
| `AZURE_STORAGE_ACCOUNT_NAME` | `devstoreaccount1`      | Storage account name     |
| `AZURE_STORAGE_QUEUE_NAME`   | `queueapp-queue`        | Azure Storage Queue name |
| `FLOCI_AZ_ENDPOINT`          | `http://localhost:4577` | Local Floci endpoint     |

The application contains fallback values, so environment variables are optional for the default local setup.

The effective configuration is:

```text
Storage Account:
devstoreaccount1

Queue:
queueapp-queue

Floci Endpoint:
http://localhost:4577
```

---

# Local Azure Storage Endpoint

Floci exposes the local Azure-compatible storage API through:

```text
http://localhost:4577
```

The Queue Storage URI used by the application follows this structure:

```text
http://localhost:4577/{storage-account}-queue/{queue-name}
```

For the default configuration:

```text
http://localhost:4577/devstoreaccount1-queue/queueapp-queue
```

The two path components have different purposes:

```text
devstoreaccount1-queue
        │
        └── Floci storage service/account route

queueapp-queue
        │
        └── Actual Azure Storage Queue
```

Do not confuse the Floci service route with the queue name.

---

# Start the Local Azure Environment

From the project root, run:

```bash
docker compose up -d
```

This starts the services defined in `docker-compose.yml`.

Verify that the containers are running:

```bash
docker compose ps
```

You should see the Floci container in a running state.

To view the container logs:

```bash
docker compose logs
```

To follow the logs:

```bash
docker compose logs -f
```

To stop the environment:

```bash
docker compose down
```

---

# Restore .NET Dependencies

From the project directory:

```bash
dotnet restore
```

Build the application:

```bash
dotnet build
```

If the build succeeds, the application is ready to run.

---

# Run the Application

Start the application without command-line arguments:

```bash
dotnet run
```

When no arguments are provided, QueueApp attempts to receive one message from the configured queue.

Example:

```text
QueueApp is booting...
Queue client devstoreaccount1 is connected.
Storage Account : devstoreaccount1
Queue Name      : queueapp-queue
Floci Endpoint  : http://localhost:4577
Queue URI       : http://localhost:4577/devstoreaccount1-queue/queueapp-queue
Received: Hello from QueueApp
```

After receiving a message, the application deletes the message from the queue using its message ID and pop receipt.

---

# Send a Message

Pass the message as command-line arguments.

For example:

```bash
dotnet run -- "Hello from QueueApp"
```

Multiple arguments are combined into a single message:

```bash
dotnet run -- this is a test message
```

The application uses:

```csharp
string value = String.Join(" ", args);
```

Therefore the resulting queue message is:

```text
this is a test message
```

Example output:

```text
QueueApp is booting...
Queue client devstoreaccount1 is connected.
Storage Account : devstoreaccount1
Queue Name      : queueapp-queue
Floci Endpoint  : http://localhost:4577
Queue URI       : http://localhost:4577/devstoreaccount1-queue/queueapp-queue
The queue queueapp-queue exists.
Sent: this is a test message
```

---

# Receive a Message

Run:

```bash
dotnet run
```

The application calls:

```csharp
await theQueue.ReceiveMessagesAsync(1);
```

If a message is available, it retrieves the first message and deletes it:

```csharp
QueueMessage msg = messages[0];

await theQueue.DeleteMessageAsync(
    msg.MessageId,
    msg.PopReceipt);
```

Example:

```text
Received: this is a test message
```

The message is removed from the queue after successful retrieval.

---

# Queue Creation Behavior

Before sending a message, the application checks whether the queue exists:

```csharp
if (await theQueue.ExistsAsync())
{
    return $"The queue {queueName} exists.";
}
```

If the queue does not exist, the application asks whether it should create it:

```text
Queue does not exist. Attempt to create it? (Y/N)
```

Enter:

```text
Y
```

to create the queue.

The application then calls:

```csharp
await theQueue.CreateAsync();
```

If the queue already exists, no creation prompt is displayed.

---

# Message Lifecycle

The application follows this lifecycle when sending a message:

```text
Application starts
       │
       ▼
Create QueueClient
       │
       ▼
Check queue existence
       │
       ├── Exists ────────────────┐
       │                          │
       └── Does not exist         │
                  │               │
                  ▼               │
             Ask user             │
                  │               │
             Create queue         │
                  │               │
                  └───────┬───────┘
                          ▼
                  Send message
                          │
                          ▼
                     Queue Storage
```

For receiving:

```text
Application starts
       │
       ▼
Create QueueClient
       │
       ▼
ReceiveMessagesAsync(1)
       │
       ├── Message available
       │        │
       │        ▼
       │   Read message
       │        │
       │        ▼
       │   Delete message
       │
       └── Queue empty
                │
                ▼
          Ask whether to
          delete the queue
```

---

# Environment Variables

The application can be configured without modifying the source code.

## Windows PowerShell

```powershell
$env:AZURE_STORAGE_ACCOUNT_NAME="devstoreaccount1"
$env:AZURE_STORAGE_QUEUE_NAME="queueapp-queue"
$env:FLOCI_AZ_ENDPOINT="http://localhost:4577"
```

Then:

```powershell
dotnet run
```

## Windows CMD

```cmd
set AZURE_STORAGE_ACCOUNT_NAME=devstoreaccount1
set AZURE_STORAGE_QUEUE_NAME=queueapp-queue
set FLOCI_AZ_ENDPOINT=http://localhost:4577
```

Then:

```cmd
dotnet run
```

## Linux/macOS

```bash
export AZURE_STORAGE_ACCOUNT_NAME=devstoreaccount1
export AZURE_STORAGE_QUEUE_NAME=queueapp-queue
export FLOCI_AZ_ENDPOINT=http://localhost:4577
```

Then:

```bash
dotnet run
```

---

# Azure Storage Authentication

The application uses `StorageSharedKeyCredential`:

```csharp
new StorageSharedKeyCredential(
    devstorageAccountName,
    DevAccountKey)
```

The local development account uses the standard development storage account:

```text
devstoreaccount1
```

The application uses the corresponding development storage account key.

This credential is intended for local development only.

Do not use the development account key as a production credential.

For production deployments, use an appropriate Azure Storage authentication mechanism such as managed identity, Azure RBAC, or a securely managed storage credential.

---

# Docker Compose Lifecycle

Start the local environment:

```bash
docker compose up -d
```

Check status:

```bash
docker compose ps
```

View logs:

```bash
docker compose logs -f
```

Stop containers:

```bash
docker compose down
```

Stop containers and remove associated volumes:

```bash
docker compose down -v
```

Use `-v` only when you intentionally want to remove persisted local data.

---

# Clean Start

If you want to start the environment from a clean state:

```bash
docker compose down -v
docker compose up -d
```

Then rebuild the application:

```bash
dotnet clean
dotnet restore
dotnet build
```

Run:

```bash
dotnet run
```

---

# Troubleshooting

## 1. Connection refused on port 4577

If the application cannot connect to:

```text
http://localhost:4577
```

check the Docker containers:

```bash
docker compose ps
```

Then inspect the logs:

```bash
docker compose logs -f
```

Also verify that port `4577` is exposed by the Floci service in `docker-compose.yml`.

---

## 2. QueueNotFound

If the application reports:

```text
The queue does not exist.
```

verify the configured queue name:

```text
AZURE_STORAGE_QUEUE_NAME
```

For the default configuration it should be:

```text
queueapp-queue
```

You can also check the values printed by the application:

```text
Storage Account : devstoreaccount1
Queue Name      : queueapp-queue
Floci Endpoint  : http://localhost:4577
Queue URI       : http://localhost:4577/devstoreaccount1-queue/queueapp-queue
```

---

## 3. The queue exists but the displayed name looks incorrect

When using a custom storage emulator endpoint, avoid relying on `QueueClient.Name` for diagnosing the complete Floci route.

The actual application configuration is represented by:

```text
Storage Account : devstoreaccount1
Queue Name      : queueapp-queue
```

while the endpoint contains:

```text
/devstoreaccount1-queue/
```

These represent different parts of the local storage endpoint.

For application-level logging, prefer the configured queue name:

```csharp
devqueueName
```

rather than using the SDK's parsed `QueueClient.Name` when diagnosing emulator-specific routing.

---

## 4. Message is not received

Run:

```bash
dotnet run
```

The receive operation only retrieves messages currently available in the queue.

If the queue is empty, the application executes the empty-queue handling logic.

Send a message first:

```bash
dotnet run -- "Test message"
```

Then receive it:

```bash
dotnet run
```

---

## 5. Docker container is running but the application cannot connect

Check whether port `4577` is published:

```bash
docker compose ps
```

You should see a port mapping corresponding to:

```text
4577
```

You can also test the endpoint independently:

```bash
curl http://localhost:4577
```

If the service is reachable but QueueApp still fails, verify:

```text
FLOCI_AZ_ENDPOINT
```

The application expects:

```text
http://localhost:4577
```

when running directly on the host.

---

# Running QueueApp and Floci in Docker

If QueueApp is later containerized and added to the same Docker Compose network, do not use:

```text
http://localhost:4577
```

from inside the QueueApp container.

Inside a Docker container, `localhost` refers to the current container.

Instead, use the Floci Compose service name and its internal port, for example:

```text
http://<floci-service-name>:4577
```

The exact hostname should match the service name defined in `docker-compose.yml`.

This distinction is important:

```text
Host application
    │
    └── localhost:4577
              │
              ▼
         Docker published port
              │
              ▼
           Floci

Docker application
    │
    └── floci-service:4577
              │
              ▼
         Docker network
              │
              ▼
           Floci
```

---

# Development Workflow

A typical development workflow is:

## Step 1: Start local storage

```bash
docker compose up -d
```

## Step 2: Verify the container

```bash
docker compose ps
```

## Step 3: Restore dependencies

```bash
dotnet restore
```

## Step 4: Build

```bash
dotnet build
```

## Step 5: Send a message

```bash
dotnet run -- "Hello Azure Queue"
```

## Step 6: Receive the message

```bash
dotnet run
```

Expected:

```text
Received: Hello Azure Queue
```

## Step 7: Stop the local environment

```bash
docker compose down
```

---

# Dependencies

The application uses the Azure Storage Queue SDK.

The primary namespace is:

```csharp
using Azure.Storage.Queues;
```

The application also uses:

```csharp
using Azure;
using Azure.Storage;
using Azure.Storage.Queues.Models;
```

The project should reference the corresponding Azure Storage Queues NuGet package in `QueueApp.csproj`.

Restore packages with:

```bash
dotnet restore
```

---

# Production Considerations

This project is intended for local development and demonstration.

The following values should not be used as production configuration:

```text
devstoreaccount1
```

and the development storage account key.

For production Azure Storage:

* Use Azure-managed authentication where possible.
* Do not hard-code storage account keys.
* Store secrets in an appropriate secret-management system.
* Use environment-specific configuration.
* Configure Azure Storage through dependency injection.
* Use Azure RBAC and managed identities where applicable.
* Configure appropriate queue visibility timeouts and message TTLs.
* Implement retry and transient-fault handling.
* Add structured logging and telemetry.
* Avoid deleting messages until the application has successfully processed them.

Note: there is production grade code published into feature/queue-storage branch for reference.
---

# Summary

The complete local setup is:

```text
Docker
  │
  ▼
docker-compose.yml
  │
  ▼
Floci
  │
  │ http://localhost:4577
  ▼
Azure-compatible Queue Storage
  │
  │ queue: queueapp-queue
  ▼
QueueApp
  │
  ├── Send message
  │
  └── Receive + delete message
```

The minimum commands required to run the project are:

```bash
docker compose up -d
dotnet restore
dotnet build
dotnet run -- "Hello from QueueApp"
dotnet run
```

The first `dotnet run` sends the message, and the second receives and deletes it from the local queue.
