namespace StrawberryShake.Transport.WebSockets;

public class SynchronizedMessageWriterTests
{
    [Fact]
    public async Task WriteObject_EmptyBuffer_Object()
    {
        // arrange
        var socketClient = new SocketClientStub() { IsClosed = false, };
        await using var writer = new SynchronizedMessageWriter(socketClient);

        // act
        await writer.CommitAsync(x =>
            {
                x.WriteStartObject();
                x.WriteEndObject();
            },
            CancellationToken.None);

        // assert
        var elm = Assert.Single(socketClient.SentMessages);
        Assert.Equal("{}", elm);
    }

    [Fact]
    public async Task WriteObject_EmptyBuffer_ObjectParallel()
    {
        // arrange
        var socketClient = new SocketClientStub() { IsClosed = false, };
        await using var writer = new SynchronizedMessageWriter(socketClient);

        // act
        var canceled = writer.CommitAsync(x =>
            {
                x.WriteStartObject();
                x.WriteEndObject();
            },
            new(true));
        Assert.True(canceled.IsCanceled);

        List<Task> tasks = [];
        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (var m = 0; m < 100; m++)
                {
                    await writer.CommitAsync(x =>
                        {
                            x.WriteStartObject();
                            x.WriteEndObject();
                        },
                        CancellationToken.None);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // assert
        Assert.Equal(1000, socketClient.SentMessages.Count);
        foreach (var message in socketClient.SentMessages)
        {
            Assert.Equal("{}", message);
        }
    }
}
