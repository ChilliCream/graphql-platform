# ResolveAsync_Should_RefreshTheAccessToken_When_TheTokenExpired

```json
{
  "Credentials": [
    {
      "Kind": "AccessToken",
      "Value": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLTEiLCJzZXNzaW9uX2lkIjoic2Vzc2lvbi0xIiwiYXBpX3VybCI6Im5pdHJvLmV4YW1wbGUuY29tIn0.signature"
    },
    {
      "Kind": "AccessToken",
      "Value": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLTEiLCJzZXNzaW9uX2lkIjoic2Vzc2lvbi0xIiwiYXBpX3VybCI6Im5pdHJvLmV4YW1wbGUuY29tIn0.signature"
    }
  ],
  "Requests": [
    {
      "Method": "GET",
      "Path": "/.well-known/openid-configuration",
      "Body": ""
    },
    {
      "Method": "POST",
      "Path": "/connect/token",
      "Body": "grant_type=refresh_token&refresh_token=refresh-token&client_id=nitro-cli"
    }
  ],
  "PersistedTokens": {
    "AccessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLTEiLCJzZXNzaW9uX2lkIjoic2Vzc2lvbi0xIiwiYXBpX3VybCI6Im5pdHJvLmV4YW1wbGUuY29tIn0.signature",
    "IdToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJuaXRyby1jbGkifQ.signature",
    "RefreshToken": "refreshed-refresh-token",
    "ExpiresAt": "2026-07-29T13:00:00+00:00"
  }
}
```
