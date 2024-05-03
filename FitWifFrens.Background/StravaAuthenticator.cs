using RestSharp.Portable;
using System.Net;

namespace FitWifFrens.Background
{
    public class StravaAuthenticator : IAuthenticator
    {
        private readonly string _accessToken;

        public StravaAuthenticator(string accessToken)
        {
            _accessToken = accessToken;
        }

        public bool CanPreAuthenticate(IRestClient client, IRestRequest request, ICredentials credentials)
        {
            return false;
        }

        public bool CanPreAuthenticate(IHttpClient client, IHttpRequestMessage request, ICredentials credentials)
        {
            return true;
        }

        public bool CanHandleChallenge(IHttpClient client, IHttpRequestMessage request, ICredentials credentials,
            IHttpResponseMessage response)
        {
            return false;
        }

        public Task PreAuthenticate(IRestClient client, IRestRequest request, ICredentials credentials)
        {
            throw new NotImplementedException();
        }

        public Task PreAuthenticate(IHttpClient client, IHttpRequestMessage request, ICredentials credentials)
        {
            request.Headers.Add("Authorization", "Bearer " + _accessToken);

            return Task.CompletedTask;
        }

        public Task HandleChallenge(IHttpClient client, IHttpRequestMessage request, ICredentials credentials,
            IHttpResponseMessage response)
        {
            throw new NotImplementedException();
        }
    }
}
