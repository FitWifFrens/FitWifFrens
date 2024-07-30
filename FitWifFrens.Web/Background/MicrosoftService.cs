using FitWifFrens.Data;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FitWifFrens.Web.Background
{
    public class MicrosoftService
    {
        private readonly DataContext _dataContext;
        private readonly HttpClient _httpClient;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<MicrosoftService> _logger;

        private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

        public MicrosoftService(DataContext dataContext, IHttpClientFactory httpClientFactory, RefreshTokenService refreshTokenService, TelemetryClient telemetryClient, ILogger<MicrosoftService> logger)
        {
            _dataContext = dataContext;
            _refreshTokenService = refreshTokenService;
            _httpClient = httpClientFactory.CreateClient();
            _telemetryClient = telemetryClient;
            _logger = logger;

            _resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.FromSeconds(2),

                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>().HandleResult(r => !r.IsSuccessStatusCode)
                })
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.FromSeconds(2),

                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>().HandleResult(r => r.StatusCode == HttpStatusCode.Unauthorized),

                    OnRetry = async args =>
                    {
                        if (args.Context.Properties.TryGetValue(new ResiliencePropertyKey<string>("UserId"), out var userId))
                        {
                            await _refreshTokenService.RefreshMicrosoftToken(userId, args.Context.CancellationToken);
                        }
                        else
                        {
                            throw new Exception("70a8eebb-0af3-4797-acf4-fd5bd457bd75");
                        }
                    }
                })
                .AddTimeout(TimeSpan.FromSeconds(10))
                .Build();
        }

        public async Task UpdateProviderMetricValues(CancellationToken cancellationToken)
        {
            foreach (var user in await _dataContext.Users.Where(u => u.Logins.Any(l => l.LoginProvider == "Microsoft")).Include(u => u.Tokens).ToListAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tokens = user.Tokens.Where(t => t.LoginProvider == "Microsoft").ToList();

                if (tokens.Any())
                {
                    _telemetryClient.TrackTrace($"Updating Microsoft data for user {user.Id} with token {tokens.Single(t => t.Name == "access_token").Value}", SeverityLevel.Information);

                    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);
                    resilienceContext.Properties.Set(new ResiliencePropertyKey<string>("UserId"), user.Id);

                    using var response = await _resiliencePipeline.ExecuteAsync(async rc =>
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/todo/lists");
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _refreshTokenService.GetMicrosoftToken(user.Id, rc.CancellationToken));

                        return await _httpClient.SendAsync(request, cancellationToken);

                    }, resilienceContext);

                    ResilienceContextPool.Shared.Return(resilienceContext);

                    using var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                }
            }
        }
    }
}
