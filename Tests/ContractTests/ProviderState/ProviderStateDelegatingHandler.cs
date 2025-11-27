using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tests.ContractTests.ProviderState
{
    public class ProviderStateDelegatingHandler : DelegatingHandler
    {
        private readonly IProviderStateHandler _stateHandler;

        public ProviderStateDelegatingHandler(IProviderStateHandler stateHandler)
        {
            _stateHandler = stateHandler;
            InnerHandler = new SocketsHttpHandler();
        }

        public HttpClient CreateClient()
        {
            var client = new HttpClient(this);
            client.BaseAddress = _stateHandler.HttpHelper.Client.BaseAddress;
            return client;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri?.AbsolutePath == ProviderStates.ProviderStatesApi)
            {
                if (request.Content != null)
                {
                    var body = await request.Content.ReadAsStringAsync(ct);
                    var stateRequest = JsonSerializer.Deserialize<ProviderStateRequest>(body);
                    if (stateRequest != null)
                    {
                        await _stateHandler.HandleProviderStateAsync(stateRequest.State);
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                }
            }
            return await base.SendAsync(request, ct);
        }
    }
}
