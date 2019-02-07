const search = window.location.search;
export const parameters = {};

search
  .substr(1)
  .split("&")
  .forEach(function(entry) {
    var eq = entry.indexOf("=");
    if (eq >= 0) {
      parameters[decodeURIComponent(entry.slice(0, eq))] = decodeURIComponent(
        entry.slice(eq + 1),
      );
    }
  });

// if variables was provided, try to format it.
if (parameters.variables) {
  try {
    parameters.variables = JSON.stringify(
      JSON.parse(parameters.variables),
      null,
      2,
    );
  } catch (e) {
    // Do nothing, we want to display the invalid JSON as a string, rather
    // than present an error.
  }
}

export function onEditQuery(newQuery) {
  parameters.query = newQuery;
  updateURL();
}

export function onEditVariables(newVariables) {
  parameters.variables = newVariables;
  updateURL();
}

export function onEditOperationName(newOperationName) {
  parameters.operationName = newOperationName;
  updateURL();
}

function updateURL() {
  var newSearch =
    "?" +
    Object.keys(parameters)
      .filter(function(key) {
        return Boolean(parameters[key]);
      })
      .map(function(key) {
        return (
          encodeURIComponent(key) + "=" + encodeURIComponent(parameters[key])
        );
      })
      .join("&");
  history.replaceState(null, null, newSearch);
}
