using ABCRetailers.Services;

namespace ABCRetailers
{
    public class Program
    {
        public static async Task Main(string[] args)  // Make Main async
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register Azure Storage service for dependency injection
            builder.Services.AddSingleton<IAzureStorageService, AzureStorageService>();

            var app = builder.Build();

            // Initialize Azure Tables
            using (var scope = app.Services.CreateScope())
            {
                var storageService = scope.ServiceProvider.GetRequiredService<IAzureStorageService>();
                await StorageInitializer.InitializeStorageAsync(storageService);  // Correct call
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
