using Azure.Core;
using Azure.Identity;

namespace AzureDevOpsPolicyGuard.Application.Services;

public interface IAzureCredential
{
    public ChainedTokenCredential GetCredential();
}