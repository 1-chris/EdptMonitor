

https://learn.microsoft.com/en-us/dotnet/api/overview/azure/extensions.aspnetcore.configuration.secrets-readme?view=azure-dotnet

az keyvault create --name MyVault --resource-group MyResourceGroup --location westus
az keyvault secret set --vault-name MyVault --name MySecret --value "hVFkk965BuUv"

az role assignment create --role "Key Vault Reader" --assignee {i.e user@microsoft.com} --scope /subscriptions/{subscriptionid}/resourcegroups/{resource-group-name}
az role assignment create --role "Key Vault Secrets User" --assignee {i.e user@microsoft.com} --scope /subscriptions/{subscriptionid}/resourcegroups/{resource-group-name}

ConfigurationBuilder builder = new ConfigurationBuilder();
builder.AddAzureKeyVault(new Uri("<Vault URI>"), new DefaultAzureCredential());

IConfiguration configuration = builder.Build();
Console.WriteLine(configuration["MySecret"]);




# Server set up

1. Create key vault
2. Create log analytics workspace
3. Create data collection endpoint
4. Create web app and assign system-assigned managed identity
5. Create 5x key vault entries:
LogIngestionEndpointUri, LogIngestionProcessMessageRuleId, LogIngestionProcessMessageStreamName, LogIngestionStatusMessageRuleId, LogIngestionStatusMessageStreamName (alternatively you can add these to appsettings.json)
6. Create 2x custom tables (DCR based) in log analytics workspace

Assign Monitoring Metrics Publisher access to managed identity for both DCR rules
Use "json view" in DCR rules to find the rule ID for key vault entries 