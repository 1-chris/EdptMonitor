

https://learn.microsoft.com/en-us/dotnet/api/overview/azure/extensions.aspnetcore.configuration.secrets-readme?view=azure-dotnet

az keyvault create --name MyVault --resource-group MyResourceGroup --location westus
az keyvault secret set --vault-name MyVault --name MySecret --value "hVFkk965BuUv"

az role assignment create --role "Key Vault Reader" --assignee {i.e user@microsoft.com} --scope /subscriptions/{subscriptionid}/resourcegroups/{resource-group-name}
az role assignment create --role "Key Vault Secrets User" --assignee {i.e user@microsoft.com} --scope /subscriptions/{subscriptionid}/resourcegroups/{resource-group-name}

ConfigurationBuilder builder = new ConfigurationBuilder();
builder.AddAzureKeyVault(new Uri("<Vault URI>"), new DefaultAzureCredential());

IConfiguration configuration = builder.Build();
Console.WriteLine(configuration["MySecret"]);

