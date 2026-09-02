using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class MailDigestTests
{
    [Fact]
    public void Render_Should_ReturnInboxPointer_When_NoMessagesAreShown()
    {
        // act
        var digest = MailDigest.Render("maya", [], 2);

        // assert
        digest.MatchInlineSnapshot(
            """
            You have 2 unread nitro messages. Run `nitro agent mail inbox --actor maya`.
            """);
    }

    [Fact]
    public void Render_Should_IncludeOneMessage_When_ItFitsTheDigest()
    {
        // arrange
        var message = Message("m-1", "One", "Hello", "2026-01-01T00:00:00Z");

        // act
        var digest = MailDigest.Render("maya", [message], 1);

        // assert
        digest.MatchInlineSnapshot(
            """
            You have 1 unread nitro message; 1 shown below as `nitro agent mail read --thread --output json` prints them. Reply with `nitro agent mail reply --message <id> --actor maya --body "..."` or ack with `nitro agent mail ack --message <id> --actor maya`; anything not shown is in `nitro agent mail inbox --unread --actor maya`.
            {
              "items": [
                {
                  "id": "m-1",
                  "threadId": "t-1",
                  "inReplyTo": null,
                  "from": "bob",
                  "to": [
                    "maya"
                  ],
                  "cc": [],
                  "subject": "One",
                  "body": "Hello",
                  "createdAt": "2026-01-01T00:00:00+00:00",
                  "read": false,
                  "archived": false
                }
              ]
            }
            """);
    }

    [Fact]
    public void Render_Should_OrderMessagesByCreationThenId_When_MultipleMessagesFit()
    {
        // arrange
        var later = Message("m-3", "Later", "third", "2026-01-03T00:00:00Z");
        var firstById = Message("m-1", "First", "first", "2026-01-02T00:00:00Z");
        var secondById = Message("m-2", "Second", "second", "2026-01-02T00:00:00Z");

        // act
        var digest = MailDigest.Render("maya", [later, secondById, firstById], 3);

        // assert
        digest.MatchInlineSnapshot(
            """
            You have 3 unread nitro messages; 3 shown below as `nitro agent mail read --thread --output json` prints them. Reply with `nitro agent mail reply --message <id> --actor maya --body "..."` or ack with `nitro agent mail ack --message <id> --actor maya`; anything not shown is in `nitro agent mail inbox --unread --actor maya`.
            {
              "items": [
                {
                  "id": "m-1",
                  "threadId": "t-1",
                  "inReplyTo": null,
                  "from": "bob",
                  "to": [
                    "maya"
                  ],
                  "cc": [],
                  "subject": "First",
                  "body": "first",
                  "createdAt": "2026-01-02T00:00:00+00:00",
                  "read": false,
                  "archived": false
                },
                {
                  "id": "m-2",
                  "threadId": "t-1",
                  "inReplyTo": null,
                  "from": "bob",
                  "to": [
                    "maya"
                  ],
                  "cc": [],
                  "subject": "Second",
                  "body": "second",
                  "createdAt": "2026-01-02T00:00:00+00:00",
                  "read": false,
                  "archived": false
                },
                {
                  "id": "m-3",
                  "threadId": "t-1",
                  "inReplyTo": null,
                  "from": "bob",
                  "to": [
                    "maya"
                  ],
                  "cc": [],
                  "subject": "Later",
                  "body": "third",
                  "createdAt": "2026-01-03T00:00:00+00:00",
                  "read": false,
                  "archived": false
                }
              ]
            }
            """);
    }

    [Fact]
    public void Render_Should_TruncateLongBodies_When_AMessageExceedsTheBodyLimit()
    {
        // arrange
        var body = new string('a', MailDigestPolicy.MaxBodyChars + 1);
        var message = Message("m-1", "One", body, "2026-01-01T00:00:00Z");

        // act
        var digest = MailDigest.Render("maya", [message], 1);

        // assert
        digest.Replace(new string('a', MailDigestPolicy.MaxBodyChars), "<body>").MatchInlineSnapshot(
            """
            You have 1 unread nitro message; 1 shown below as `nitro agent mail read --thread --output json` prints them. Reply with `nitro agent mail reply --message <id> --actor maya --body "..."` or ack with `nitro agent mail ack --message <id> --actor maya`; anything not shown is in `nitro agent mail inbox --unread --actor maya`.
            {
              "items": [
                {
                  "id": "m-1",
                  "threadId": "t-1",
                  "inReplyTo": null,
                  "from": "bob",
                  "to": [
                    "maya"
                  ],
                  "cc": [],
                  "subject": "One",
                  "body": "<body>\n[body truncated: nitro agent mail read --message m-1 --actor maya]",
                  "createdAt": "2026-01-01T00:00:00+00:00",
                  "read": false,
                  "archived": false
                }
              ]
            }
            """);
    }

    [Fact]
    public void Render_Should_DropNewestMessages_When_TheDigestExceedsTheTotalByteLimit()
    {
        // arrange
        var body = new string('a', MailDigestPolicy.MaxBodyChars);
        var messages = Enumerable.Range(1, 8)
            .Select(i => Message($"m-{i}", $"Subject {i}", body, $"2026-01-{i:00}T00:00:00Z"))
            .ToArray();

        // act
        var digest = MailDigest.Render("maya", messages, 8);

        // assert
        digest.Replace(body, "<body>").MatchInlineSnapshot(
            """
            You have 8 unread nitro messages; 7 shown below as `nitro agent mail read --thread --output json` prints them. Reply with `nitro agent mail reply --message <id> --actor maya --body "..."` or ack with `nitro agent mail ack --message <id> --actor maya`; anything not shown is in `nitro agent mail inbox --unread --actor maya`.
            {
              "items": [
                {
                  "id": "m-1",
                  "threadId": "t-1",
                  "inReplyTo": null,
                  "from": "bob",
                  "to": [
                    "maya"
                  ],
                  "cc": [],
                  "subject": "Subject 1",
                  "body": "<body>",
                  "createdAt": "2026-01-01T00:00:00+00:00",
                  "read": false,
                  "archived": false
                },
                {
                  "id": "m-2",
                  "threadId": "t-1",
                  "inReplyTo": null,
                  "from": "bob",
                  "to": [
                    "maya"
                  ],
                  "cc": [],
                  "subject": "Subject 2",
                  "body": "<body>",
                  "createdAt": "2026-01-02T00:00:00+00:00",
                  "read": false,
                  "archived": false
                },
                {
                  "id": "m-3",
                  "threadId": "t-1",
                  "inReplyTo": null,
                  "from": "bob",
                  "to": [
                    "maya"
                  ],
                  "cc": [],
                  "subject": "Subject 3",
                  "body": "<body>",
                  "createdAt": "2026-01-03T00:00:00+00:00",
                  "read": false,
                  "archived": false
                },
                {
                  "id": "m-4",
                  "threadId": "t-1",
                  "inReplyTo": null,
                  "from": "bob",
                  "to": [
                    "maya"
                  ],
                  "cc": [],
                  "subject": "Subject 4",
                  "body": "<body>",
                  "createdAt": "2026-01-04T00:00:00+00:00",
                  "read": false,
                  "archived": false
                },
                {
                  "id": "m-5",
                  "threadId": "t-1",
                  "inReplyTo": null,
                  "from": "bob",
                  "to": [
                    "maya"
                  ],
                  "cc": [],
                  "subject": "Subject 5",
                  "body": "<body>",
                  "createdAt": "2026-01-05T00:00:00+00:00",
                  "read": false,
                  "archived": false
                },
                {
                  "id": "m-6",
                  "threadId": "t-1",
                  "inReplyTo": null,
                  "from": "bob",
                  "to": [
                    "maya"
                  ],
                  "cc": [],
                  "subject": "Subject 6",
                  "body": "<body>",
                  "createdAt": "2026-01-06T00:00:00+00:00",
                  "read": false,
                  "archived": false
                },
                {
                  "id": "m-7",
                  "threadId": "t-1",
                  "inReplyTo": null,
                  "from": "bob",
                  "to": [
                    "maya"
                  ],
                  "cc": [],
                  "subject": "Subject 7",
                  "body": "<body>",
                  "createdAt": "2026-01-07T00:00:00+00:00",
                  "read": false,
                  "archived": false
                }
              ]
            }
            """);
    }

    [Fact]
    public void Render_Should_ReturnInboxPointer_When_NoMessageFitsTheTotalByteLimit()
    {
        // arrange
        var message = Message(
            "m-1",
            new string('a', MailDigestPolicy.MaxTotalBytes),
            "Hello",
            "2026-01-01T00:00:00Z");

        // act
        var digest = MailDigest.Render("maya", [message], 1);

        // assert
        digest.MatchInlineSnapshot(
            """
            You have 1 unread nitro message. Run `nitro agent mail inbox --actor maya`.
            """);
    }

    private static MailMessageDetailResult Message(
        string id,
        string subject,
        string body,
        string createdAt)
        => new()
        {
            Id = id,
            ThreadId = "t-1",
            InReplyTo = null,
            From = "bob",
            To = ["maya"],
            Cc = [],
            Subject = subject,
            Body = body,
            CreatedAt = DateTimeOffset.Parse(createdAt),
            Read = false,
            Archived = false
        };
}
