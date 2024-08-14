namespace AzureDevOpsPolicyGuard.Application.Services;

public interface IKeyVaultService
{
    public Task<string> GetSecretAsync(string secretName);
}