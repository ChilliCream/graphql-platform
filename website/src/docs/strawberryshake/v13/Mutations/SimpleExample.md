# Step 1: Inject Client

On the razor-page, there you want to use the client, inject it via the `@inject`-syntax:

```csharp
@page "/"
@inject CryptoClient client;
```

# Step 2: Create a new query Document containing the mutation

The file name does not matter, you going to refer to the `CreateAlert` name of the mutation.

```graphql
mutation CreateAlert (
  $symbol: String!,
  $targetPrice: Float!,
  $currency: String!,
  $recurring: Boolean!,
) {
    createAlert(
        input: {
        symbol: $symbol,
        targetPrice: $targetPrice,
        currency: $currency,
        recurring: $recurring}
    ) {
        createdAlert {
            id
            username
            percentageChange
            currency
            targetPrice
            recurring
        }
    }
}
```

The file name does not matter, you going to refer to the `CreateAlert` name of the mutation.

# Step 3 Add some inputs to collect values for the mutation

```csharp
...
@inject CryptoClient client;
  
  <p> <input @bind="symbol" /> </p>
  <p> <input @bind="targetPrice" /> </p>
  <p> <input @bind="currency" /> </p>
  <p> <input type="checkbox" @bind="recurring" /> </p>

  <button @onclick=CreateAlertHandler>Create Alert</button>
```

The data-binding to feed the values to the variables is done by `@bind`, and the `@onclick`-handler contains the function that should be called when the button was pressed.

# Step 4 Add some (magic) logic for execution the the mutation

```csharp
@code {
    private string symbol = String.Empty;
    private float targetPrice = String.Empty;
    private string currency = String.Empty;
    private bool recurring = String.Empty;

    private async Task CreateAlertHandler()
    {
        Console.WriteLine("Create Alert Handler");

        await client.CreateAlert.ExecuteAsync(
            symbol,
            Convert.ToFloat(targetPrice),
            currency,
            recurring);
    }
}
```

The `@code` part contains the variable definitions as well as the private asyncronous function for handling the button pressing.

To execute the mutation the function `client.CreateAlert.ExecuteAsync` is used. Pay attention to conversions that are maybe necessary to execute the query and send the data to the server.


