namespace ABCRetailers.Services
{
    public class StorageInitializer
    {
        public static async Task InitializeStorageAsync(IAzureStorageService storageService)
        {
            if (storageService == null)
                throw new ArgumentNullException(nameof(storageService));


            await storageService.CreateTableAsync("Customers");
            await storageService.CreateTableAsync("Products");
            await storageService.CreateTableAsync("Orders");

        }
    }
}
