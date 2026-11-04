using System.Threading.Channels;

namespace NordiskaPortal.API.Services
{
    public class TaxReportQueue
    {
        private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();  

        public void Enqueue(Guid reportId) => _channel.Writer.TryWrite(reportId);

        public async Task<Guid> DequeueAsync(CancellationToken cancellationToken) => await _channel.Reader.ReadAsync(cancellationToken);

    }
}
