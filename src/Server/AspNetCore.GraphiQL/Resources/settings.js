var protocol = window.location.protocol === "http:" ? "ws:" : "wss:";
var rootUri = protocol + "//" + window.location.host;

window.Settings = {
  url: rootUri,
  subscriptionUrl: rootUri + "/subscriptions"
};
