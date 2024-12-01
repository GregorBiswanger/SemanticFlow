using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace SemanticFlow.DemoConsoleApp;

public class AzureKeyVaultHelper(string keyVaultUrl)
{
    private readonly SecretClient _secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

    public async Task<string> GetSecretAsync(string secretName)
    {
        KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName);
        return secret.Value;
    }
}