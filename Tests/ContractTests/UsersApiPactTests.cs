using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;
using PactNet;
using PactNet.Output.Xunit;
using PactNet.Verifier;
using Xunit;
using Xunit.Abstractions;

namespace Tests.ContractTests;

[Trait("Category", "Contract")]
public class UsersApiPactTests : BaseApiPactTests
{
    public UsersApiPactTests(PactTestWebAppFactory factory, ITestOutputHelper output)
        : base(factory, output)
    { }

    [Fact]
    public void VerifyUsersMeContract()
    {
        var config = new PactVerifierConfig
        {
            LogLevel = PactLogLevel.Debug,
            Outputters = new[] { new XunitOutput(_output) }
        };

        var pactFile = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ApiPacts.PactsPath, ApiPacts.PactUsersApiJsonFile));
        if (File.Exists(pactFile))
            _output.WriteLine($"------------------------------> Pact File: {pactFile}");
        else
                    throw new FileNotFoundException("Pact file not found");

        var addresses = _factory.Server.Features.Get<IServerAddressesFeature>()?.Addresses;
        _output.WriteLine($"------------------------------> Server Addresses: {string.Join(", ", addresses ?? new[] { "none" })}");

        var baseUri = _factory.Server.BaseAddress; // TestHttpHelper.Client.BaseAddress; // new Uri(ProviderStates.BaseUrl);// _factory.Server.BaseAddress;
        var providerStatesUri = new Uri(new Uri(ProviderStates.BaseUrl), ProviderStates.ProviderStatesApi);

        _output.WriteLine($"-------------------------------> VerifyUsersMeContract baseUri={baseUri}");
        _output.WriteLine($"-------------------------------> VerifyUsersMeContract providerStatesUri={providerStatesUri}");

        var verifier = new PactVerifier("UsersAPI", config);
        verifier
            .WithHttpEndpoint(baseUri)
            .WithFileSource(new FileInfo(pactFile))
            .WithProviderStateUrl(providerStatesUri)
            .Verify();
    }
}