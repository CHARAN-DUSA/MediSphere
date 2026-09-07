using System.Threading.Channels;

namespace MediSphere.Infrastructure.Services.Email;

public class EmailQueue
{
    private readonly Channel<EmailMessage> _emailChannel =
        Channel.CreateUnbounded<EmailMessage>(new UnboundedChannelOptions { SingleReader = true });

    private readonly Channel<SmsMessage> _smsChannel =
        Channel.CreateUnbounded<SmsMessage>(new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueEmailAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
        _emailChannel.Writer.WriteAsync(message, cancellationToken);

    public ValueTask EnqueueSmsAsync(SmsMessage message, CancellationToken cancellationToken = default) =>
        _smsChannel.Writer.WriteAsync(message, cancellationToken);

    public IAsyncEnumerable<EmailMessage> ReadEmailsAsync(CancellationToken cancellationToken) =>
        _emailChannel.Reader.ReadAllAsync(cancellationToken);

    public IAsyncEnumerable<SmsMessage> ReadSmsAsync(CancellationToken cancellationToken) =>
        _smsChannel.Reader.ReadAllAsync(cancellationToken);
}
