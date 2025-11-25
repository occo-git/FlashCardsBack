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
        if (!File.Exists(pactFile))
            throw new FileNotFoundException("Pact file not found");

        var baseUri = _factory.Server.BaseAddress;// _httpHelper.Client.BaseAddress;
        var porviderStatesUri = new Uri(new Uri(ProviderStates.BaseUrl), ProviderStates.ProviderStatesApi);

        _output.WriteLine($"-------------------------------> VerifyUsersMeContract baseUri={baseUri}");
        _output.WriteLine($"-------------------------------> VerifyUsersMeContract porviderStatesUri={porviderStatesUri}");

        var verifier = new PactVerifier("UsersAPI", config);
        verifier
            .WithHttpEndpoint(baseUri)
            .WithFileSource(new FileInfo(pactFile))
            .WithProviderStateUrl(porviderStatesUri)
            .Verify();
    }
}