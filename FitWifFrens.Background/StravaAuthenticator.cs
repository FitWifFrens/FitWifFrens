using RestSharp.Portable;
using RestSharp.Portable.OAuth2;
using System.Net;

namespace FitWifFrens.Background
{
    public class StravaAuthenticator : OAuth2Authenticator
    {
        private readonly OAuth2Client _client;
        public string AccessToken { get; private set; }

        public StravaAuthenticator(OAuth2Client client)
            : base(client)
        {
            _client = client;
            ArgumentNullException.ThrowIfNull(client, nameof(client));
        }

        public override bool CanPreAuthenticate(IRestClient client, IRestRequest request, ICredentials credentials)
        {
            return true;
        }

        public override bool CanPreAuthenticate(IHttpClient client, IHttpRequestMessage request, ICredentials credentials)
        {
            return true;
        }

        public override Task PreAuthenticate(IRestClient client, IRestRequest request, ICredentials credentials)
        {
            if (!string.IsNullOrEmpty(_client.AccessToken))
            {
                request.AddHeader("Authorization", "Bearer " + _client.AccessToken);
            }

            return Task.CompletedTask;
        }

        public override Task PreAuthenticate(IHttpClient client, IHttpRequestMessage request, ICredentials credentials)
        {
            if (!string.IsNullOrEmpty(_client.AccessToken))
            {
                request.Headers.Add("Authorization", "Bearer " + _client.AccessToken);
            }

            return Task.CompletedTask;
        }
    }
}
