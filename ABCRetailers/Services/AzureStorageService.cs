using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ABCRetailers.Services
{
    public class AzureStorageService : IAzureStorageService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueServiceClient _queueServiceClient;
        private readonly ShareServiceClient _shareServiceClient;
        private readonly ILogger<AzureStorageService> _logger;

        public AzureStorageService(IConfiguration config, ILogger<AzureStorageService> logger)
        {
            string connectionString = config.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException("Azure Storage connection string missing");

            _tableServiceClient = new TableServiceClient(connectionString);
            _blobServiceClient = new BlobServiceClient(connectionString);
            _queueServiceClient = new QueueServiceClient(connectionString);
            _shareServiceClient = new ShareServiceClient(connectionString);
            _logger = logger;
        }

        // ---------------- Table Operations ----------------
        public async Task CreateTableAsync(string tableName)
        {
            await _tableServiceClient.CreateTableIfNotExistsAsync(tableName);
        }

        public async Task InitializeStorageAsync()
        {
            // Create all required tables
            await CreateTableAsync("Products");
            await CreateTableAsync("Orders");
            await CreateTableAsync("Customers");
            await CreateTableAsync("Inventory");
            _logger.LogInformation("Azure Tables initialized successfully.");
        }

        public async Task<T> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            var client = _tableServiceClient.GetTableClient(tableName);
            try
            {
                var response = await client.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null!;
            }
        }

        public async Task<IEnumerable<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new()
        {
            var client = _tableServiceClient.GetTableClient(tableName);
            var result = new List<T>();
            await foreach (var entity in client.QueryAsync<T>())
            {
                result.Add(entity);
            }
            return result;
        }

        public async Task AddEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new()
        {
            var client = _tableServiceClient.GetTableClient(tableName);
            await client.AddEntityAsync(entity);
        }

        public async Task UpdateEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new()
        {
            var client = _tableServiceClient.GetTableClient(tableName);
            await client.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
        }

        public async Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey)
        {
            var client = _tableServiceClient.GetTableClient(tableName);
            await client.DeleteEntityAsync(partitionKey, rowKey);
        }

        // ---------------- Blob Operations ----------------
        public async Task<string> UploadBlobAsync(string containerName, IFormFile file)
        {
            var container = _blobServiceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = container.GetBlobClient($"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, true);

            return blobClient.Uri.ToString();
        }

        public async Task<BlobDownloadInfo> DownloadBlobAsync(string containerName, string blobName)
        {
            var container = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = container.GetBlobClient(blobName);
            return await blobClient.DownloadAsync();
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            var container = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = container.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<IEnumerable<string>> ListBlobsAsync(string containerName)
        {
            var container = _blobServiceClient.GetBlobContainerClient(containerName);
            var list = new List<string>();

            await foreach (var blob in container.GetBlobsAsync())
            {
                list.Add(blob.Name);
            }

            return list;
        }

        // ---------------- Queue Operations ----------------
        public async Task CreateQueueAsync(string queueName)
        {
            var queue = _queueServiceClient.GetQueueClient(queueName);
            await queue.CreateIfNotExistsAsync();
        }

        public async Task SendMessageAsync(string queueName, string message)
        {
            var queue = _queueServiceClient.GetQueueClient(queueName);
            await queue.SendMessageAsync(message);
        }

        public async Task<QueueMessage> ReceiveMessageAsync(string queueName)
        {
            var queue = _queueServiceClient.GetQueueClient(queueName);
            var msg = await queue.ReceiveMessageAsync();
            if (msg.Value != null)
            {
                await queue.DeleteMessageAsync(msg.Value.MessageId, msg.Value.PopReceipt);
            }
            return msg.Value!;
        }

        public async Task DeleteMessageAsync(string queueName, string messageId, string popReceipt)
        {
            var queue = _queueServiceClient.GetQueueClient(queueName);
            await queue.DeleteMessageAsync(messageId, popReceipt);
        }

        // ---------------- File Share Operations ----------------
        public async Task UploadFileShareAsync(string shareName, string directoryName, IFormFile file)
        {
            var share = _shareServiceClient.GetShareClient(shareName);
            await share.CreateIfNotExistsAsync();

            var dir = string.IsNullOrEmpty(directoryName) ? share.GetRootDirectoryClient() : share.GetDirectoryClient(directoryName);
            await dir.CreateIfNotExistsAsync();

            var fileClient = dir.GetFileClient(file.FileName);
            using var stream = file.OpenReadStream();
            await fileClient.CreateAsync(stream.Length);
            await fileClient.UploadAsync(stream);
        }

        public async Task<IEnumerable<string>> ListFilesInShareAsync(string shareName, string directoryName)
        {
            var share = _shareServiceClient.GetShareClient(shareName);
            var dir = string.IsNullOrEmpty(directoryName) ? share.GetRootDirectoryClient() : share.GetDirectoryClient(directoryName);

            var files = new List<string>();
            await foreach (var item in dir.GetFilesAndDirectoriesAsync())
            {
                files.Add(item.Name);
            }

            return files;
        }

        public async Task DownloadFileShareAsync(string shareName, string directoryName, string fileName, string destinationPath)
        {
            var share = _shareServiceClient.GetShareClient(shareName);
            var dir = string.IsNullOrEmpty(directoryName) ? share.GetRootDirectoryClient() : share.GetDirectoryClient(directoryName);

            var fileClient = dir.GetFileClient(fileName);
            var response = await fileClient.DownloadAsync();

            using var stream = File.OpenWrite(destinationPath);
            await response.Value.Content.CopyToAsync(stream);
        }
    }
}
