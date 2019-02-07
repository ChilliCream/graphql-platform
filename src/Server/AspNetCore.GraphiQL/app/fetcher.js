import { SubscriptionClient } from "subscriptions-transport-ws";
import { graphQLFetcher as subscriptionGraphQLFetcher } from "graphiql-subscriptions-fetcher/dist/fetcher";

export default settings => {
  function graphQLFetcher(graphQLParams) {
    return fetch(settings.url, {
      method: "post",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
      body: JSON.stringify(graphQLParams),
      credentials: "include",
    })
      .then(function(response) {
        return response.text();
      })
      .then(function(responseBody) {
        try {
          return JSON.parse(responseBody);
        } catch (error) {
          return responseBody;
        }
      });
  }

  const subscriptionsClient = new SubscriptionClient(settings.subscriptionUrl, {
    reconnect: true,
  });

  const subscriptionsFetcher = subscriptionGraphQLFetcher(
    subscriptionsClient,
    graphQLFetcher,
  );

  return subscriptionsFetcher;
};
