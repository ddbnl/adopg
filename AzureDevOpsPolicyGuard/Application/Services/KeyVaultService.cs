using Azure.Identity;
using Azure.Security.KeyVault.Secrets;


namespace AzureDevOpsPolicyGuard.Application.Services;


public class KeyVaultService : IKeyVaultService
{
    private readonly IConfiguration _configuration;
    private readonly IAzureCredential _credential;
    private SecretClient _secretClient;

    public KeyVaultService(IConfiguration configuration, IAzureCredential credential)
    {
        _configuration = configuration;
        _credential = credential;
        _secretClient = new SecretClient(new Uri(_configuration["KeyVault:Uri"]), _credential.GetCredential());
    }

    public async Task<string> GetSecretAsync(string secretName)
    {
        var keyVaultUri = _configuration["Keyvault:Uri"];
        if (keyVaultUri == null) throw new ApplicationException("Keyvault:Uri is missing");
        var result = await _secretClient.GetSecretAsync(secretName);
        return result.Value.Value;
    }
}