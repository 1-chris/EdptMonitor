using Azure.Identity;
using Azure.Monitor.Ingestion;
using EndpointMtr.Server.Hubs;
using EndpointMtr.Server.Services;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(options =>
{
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});
builder.Services.AddSingleton<EdptDataManager>();
builder.Services.AddHostedService<EdptDataExporter>();

if (builder.Configuration["AzKeyVaultEndpointUri"] is not null)
{
    builder.Configuration.AddAzureKeyVault(new Uri(builder.Configuration["AzKeyVaultEndpointUri"]), new DefaultAzureCredential(true));
}

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddLogsIngestionClient(new Uri(builder.Configuration["LogIngestionEndpointUri"]));
    clientBuilder.UseCredential(new DefaultAzureCredential(true));
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapHub<EdptHub>("/EdptHub");

app.Run();