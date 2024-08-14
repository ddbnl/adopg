using Azure.Core;
using Azure.Identity;

namespace AzureDevOpsPolicyGuard.Application.Services;

public class AzureCredential : IAzureCredential
{
    public ChainedTokenCredential Credential;

    public AzureCredential(IWebHostEnvironment hostEnvironment)
    {
        if (hostEnvironment.IsDevelopment())
        {
            Credential = new ChainedTokenCredential(
                new AzureCliCredential(),
                new InteractiveBrowserCredential()
            );
        }
        else
        {
            Credential = new ChainedTokenCredential(new WorkloadIdentityCredential());
        }
    }

    public ChainedTokenCredential GetCredential()
    {
        return Credential;
    }
}