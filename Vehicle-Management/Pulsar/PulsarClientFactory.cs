using System.Net.Sockets;
using DotPulsar;
using DotPulsar.Abstractions;

namespace VehicleManagementApi.Pulsar;

public static class PulsarClientFactory
{
    private static readonly string[] LocalhostCandidates = ["::1", "127.0.0.1"];

    public static IPulsarClient Create(PulsarOptions options, ILogger logger)
    {
        var serviceUrl = ResolveServiceUrl(options.ServiceUrl, logger);
        return PulsarClient
            .Builder()
            .ServiceUrl(serviceUrl)
            .Build();
    }

    private static Uri ResolveServiceUrl(string configuredServiceUrl, ILogger logger)
    {
        var configuredUri = new Uri(configuredServiceUrl);

        if (!configuredUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return configuredUri;

        foreach (var candidateHost in LocalhostCandidates)
        {
            var candidate = BuildUriWithHost(configuredUri, candidateHost);
            if (CanConnect(candidate.Host, candidate.Port, TimeSpan.FromSeconds(2)))
            {
                logger.LogInformation(
                    "Resolved Pulsar ServiceUrl from {Configured} to {Resolved}.",
                    configuredServiceUrl,
                    candidate);

                return candidate;
            }
        }

        logger.LogWarning(
            "Could not resolve a reachable loopback endpoint for Pulsar ServiceUrl={Configured}. Using configured value.",
            configuredServiceUrl);

        return configuredUri;
    }

    private static Uri BuildUriWithHost(Uri baseUri, string host)
    {
        var builder = new UriBuilder(baseUri)
        {
            Host = host
        };

        return builder.Uri;
    }

    private static bool CanConnect(string host, int port, TimeSpan timeout)
    {
        try
        {
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync(host, port);
            return connectTask.Wait(timeout) && tcp.Connected;
        }
        catch
        {
            return false;
        }
    }
}
