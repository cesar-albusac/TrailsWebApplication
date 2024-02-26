using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Trails.Controllers;

namespace TrailsWebApplication.Helpers
{
    public class KeyVaultSecrets
    {
        private static KeyVaultSecrets _instance;
        private static readonly object _lock = new object();

        public string ApiUrl { get; private set; }
        public string BlobConnectionString { get; private set; }

        private KeyVaultSecrets()
        {
            // Load the secrets from Azure Key Vault
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            string keyVaultUrl = configuration["KeyVaultUrl"];
            var secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

            ApiUrl = GetSecretFromKeyVault(secretClient, "trailsapiurl");
            BlobConnectionString = GetSecretFromKeyVault(secretClient, "trails-blob-connectionString");
        }

        public static KeyVaultSecrets Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new KeyVaultSecrets();
                        }
                    }
                }
                return _instance;
            }
        }

        private string GetSecretFromKeyVault(SecretClient secretClient, string secretName)
        {
            try
            {
                KeyVaultSecret secret = secretClient.GetSecret(secretName);
                return secret.Value;
            }
            catch (Exception ex)
            {
                // Handle the exception
                return string.Empty;
            }
        }
    }


}
