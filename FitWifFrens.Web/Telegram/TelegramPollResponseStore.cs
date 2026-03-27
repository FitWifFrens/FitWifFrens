using System.Collections.Concurrent;
using System.Threading;

namespace FitWifFrens.Web.Telegram
{
    public sealed record TelegramPollVote(
        long UserId,
        string? Username,
        string? DisplayName,
        IReadOnlyList<int> OptionIds,
        DateTimeOffset AnsweredAtUtc,
        long UpdateId);

    public sealed class TelegramPollResponseStore
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, TelegramPollVote>> _responsesByPollId = new();
        private long _lastUpdateId = -1;

        public void Upsert(string pollId, TelegramPollVote vote)
        {
            var pollVotes = _responsesByPollId.GetOrAdd(pollId, _ => new ConcurrentDictionary<long, TelegramPollVote>());
            pollVotes[vote.UserId] = vote;
            SetLastUpdateId(vote.UpdateId);
        }

        public IReadOnlyCollection<TelegramPollVote> GetResponses(string pollId)
        {
            if (!_responsesByPollId.TryGetValue(pollId, out var responses))
            {
                return [];
            }

            return responses.Values.OrderBy(v => v.UserId).ToArray();
        }

        public long GetLastUpdateId()
        {
            return Interlocked.Read(ref _lastUpdateId);
        }

        public void SetLastUpdateId(long updateId)
        {
            while (true)
            {
                var current = Interlocked.Read(ref _lastUpdateId);
                if (updateId <= current)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _lastUpdateId, updateId, current) == current)
                {
                    return;
                }
            }
        }
    }
}
