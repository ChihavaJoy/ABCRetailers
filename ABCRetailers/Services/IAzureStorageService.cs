using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using ABCRetailers.Models;

namespace ABCRetailers.Services
{
    public interface IAzureStorageService
    {
        // Azure Table Operations
        
        Task CreateTableAsync(string tableName);
        Task<T> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new();
        Task<IEnumerable<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new();
        Task AddEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new();
        Task UpdateEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new();
        Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey);

       
        // Azure Blob Operations
        
        Task<string> UploadBlobAsync(string containerName, IFormFile file);
        Task<BlobDownloadInfo> DownloadBlobAsync(string containerName, string blobName);
        Task DeleteBlobAsync(string containerName, string blobName);
        Task<IEnumerable<string>> ListBlobsAsync(string containerName);

        
        // 🔹 Azure Queue Operations
        
        Task CreateQueueAsync(string queueName);
        Task SendMessageAsync(string queueName, string message);
        Task<QueueMessage> ReceiveMessageAsync(string queueName);
        Task DeleteMessageAsync(string queueName, string messageId, string popReceipt);

       
        // 🔹 Azure File Share Operations
     
        Task UploadFileShareAsync(string shareName, string directoryName, IFormFile file);
        Task<IEnumerable<string>> ListFilesInShareAsync(string shareName, string directoryName);
        Task DownloadFileShareAsync(string shareName, string directoryName, string fileName, string destinationPath);

        Task InitializeStorageAsync();

    }
}




