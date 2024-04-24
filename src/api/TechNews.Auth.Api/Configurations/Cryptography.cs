using TechNews.Auth.Api.Services.Cryptography;
using TechNews.Auth.Api.Services.KeyRetrievers;

namespace TechNews.Auth.Api.Configurations;

public static class Cryptography
{
    public static IServiceCollection ConfigureCryptographicKeys(this IServiceCollection services)
    {
        switch (EnvironmentVariables.CryptographicAlgorithm)
        {
            case "ECC":
                services.AddScoped<ICryptographicKeyFactory, EcdsaCryptographicKeyFactory>();
                services.AddScoped<ICryptographicKey, EcdsaCryptographicKey>();
                break;
            case "RSA":
            default:
                services.AddScoped<ICryptographicKeyFactory, RsaCryptographicKeyFactory>();
                services.AddScoped<ICryptographicKey, RsaCryptographicKey>();
                break;
        }

        services.AddSingleton<ICryptographicKeyRetriever, CryptographicKeyInMemoryRetriever>();

        return services;
    }
}
