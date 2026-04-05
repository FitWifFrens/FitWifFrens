using FitWifFrens.Data;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Web.Background
{
    public class WeighInReminderService
    {
        private readonly DataContext _dataContext;
        private readonly NotificationService _notificationService;
        private readonly AiSummaryService _aiSummaryService;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<WeighInReminderService> _logger;

        public WeighInReminderService(
            DataContext dataContext,
            NotificationService notificationService,
            AiSummaryService aiSummaryService,
            TelemetryClient telemetryClient,
            ILogger<WeighInReminderService> logger)
        {
            _dataContext = dataContext;
            _notificationService = notificationService;
            _aiSummaryService = aiSummaryService;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task SendWeighInReminders(CancellationToken cancellationToken)
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-3);

                var usersWithWithings = await _dataContext.Users
                    .AsNoTracking()
                    .Where(u => u.Logins.Any(l => l.LoginProvider == "Withings"))
                    .Where(u => !string.IsNullOrWhiteSpace(u.Nickname))
                    .Select(u => new
                    {
                        u.Id,
                        u.Nickname,
                        LastWeighIn = u.MetricProviderValues
                            .Where(v => v.MetricName == "Weight" && v.ProviderName == "Withings" && v.MetricType == MetricType.Value)
                            .OrderByDescending(v => v.Time)
                            .Select(v => (DateTime?)v.Time)
                            .FirstOrDefault()
                    })
                    .ToListAsync(cancellationToken);

                foreach (var user in usersWithWithings)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (user.LastWeighIn.HasValue && user.LastWeighIn.Value >= cutoff)
                    {
                        continue;
                    }

                    var daysSince = user.LastWeighIn.HasValue
                        ? (int)Math.Floor((DateTime.UtcNow - user.LastWeighIn.Value).TotalDays)
                        : (int?)null;

                    _logger.LogInformation(
                        "Sending weigh-in reminder. UserId={UserId}, Nickname={Nickname}, DaysSince={DaysSince}",
                        user.Id, user.Nickname, daysSince?.ToString() ?? "never");

                    var factsRaw = await _dataContext.UserFacts
                        .AsNoTracking()
                        .Where(f => f.UserId == user.Id)
                        .Select(f => f.Fact)
                        .ToListAsync(cancellationToken);
                    var userFacts = factsRaw.Count > 0
                        ? new Dictionary<string, List<string>> { { user.Nickname!, factsRaw } }
                        : null;

                    var message = await _aiSummaryService.GenerateWeighInReminder(
                        user.Nickname!, daysSince, cancellationToken, userFacts);

                    _ = _notificationService.Notify(message);
                }
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                throw;
            }
        }
    }
}
