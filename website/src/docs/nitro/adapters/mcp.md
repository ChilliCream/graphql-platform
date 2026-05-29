---
title: "MCP"
---

The [Model Context Protocol](https://modelcontextprotocol.io/) (MCP) is an open standard that connects AI applications to external systems through a single uniform interface. The host (Claude, ChatGPT, a VS Code agent) speaks the protocol once, and any MCP-compatible server plugs in without bespoke glue code per product.

HotChocolate ships an MCP adapter that turns your GraphQL server into an MCP server. The same adapter works with a single HotChocolate server or with a Fusion gateway that composes multiple source schemas. The MCP endpoint lives on the GraphQL server itself, at `/graphql/mcp` by default, and any MCP client connects directly to that URL.

Nitro is the control plane around it. You author tools and prompts on disk, package them into a versioned feature collection, and use Nitro to store the collection, validate it, distribute it to the runtime, and surface telemetry.

What you get out of the box:

- **Storage**: each feature collection is a workspace-scoped container with tagged, immutable versions.
- **Versioning**: every upload produces a new tagged snapshot. Rollback is a republish of an earlier tag.
- **Multi-stage deployment**: publish a version to `dev`, validate it, then publish the same tag to `production`. Stages are independent.
- **Telemetry**: per-tool request counts, error rates, mean and P95/P99 latency, with traces and structured logs.
- **Validation**: GraphQL documents and prompt JSON are validated on upload and before publish, so broken collections never reach a stage.

The adapter speaks the MCP standard, so the same collection works in Claude, ChatGPT (Developer Mode), VS Code agents, and other MCP hosts.

> **Prerequisite**: Nitro distributes the feature collection, but the MCP endpoint itself is served by your runtime. Install and configure the MCP adapter on your GraphQL server or Fusion gateway before tools published from Nitro become reachable.
>
> - Hot Chocolate: [MCP Adapter](/docs/hotchocolate/v16/build/adapters/mcp)
> - Fusion: [MCP Adapter](/docs/fusion/v16/adapters/mcp)

# How it works

The mental model has four moving pieces:

1. **You author** tools (GraphQL operations) and prompts (JSON) inside your repository, in the layout shown below.
2. **The Nitro CLI uploads** a snapshot of those files as a tagged version of a feature collection.
3. **You publish** a version to a stage. Nitro distributes the collection to your HotChocolate runtime.
4. **Your HotChocolate server (or Fusion gateway) serves** the collection at its `/graphql/mcp` endpoint and executes each tool's GraphQL operation when an MCP client invokes it.

A feature collection contains two kinds of asset:

- **Tools**: GraphQL operations the model can execute. Each invocation runs against your GraphQL server and returns the result to the host. A tool can carry optional settings (title, icons, annotations) and an optional MCP Apps view (HTML the host renders inline).
- **Prompts**: templated user-facing workflows. The host shows them in its prompt picker and substitutes the user's arguments into a message.

# Author tools and prompts on disk

This section walks through the on-disk shape of a collection, then builds up from a minimal tool to a tool with an interactive view.

## Project layout

Put each tool and prompt in its own folder. Files inside a folder share the folder name as the basename:

```text
mcp/
├── prompts/
│   └── SearchProducts/
│       └── SearchProducts.json
└── tools/
    └── SearchProducts/
        ├── SearchProducts.graphql
        ├── SearchProducts.html
        └── SearchProducts.json
```

The CLI picks files up by glob:

- `--tool-pattern "./mcp/tools/**/*.graphql"` matches every tool operation.
- `--prompt-pattern "./mcp/prompts/**/*.json"` matches every prompt definition.

Optional sibling files (`.json` metadata and `.html` views) travel with their `.graphql` counterpart automatically.

## Author a tool

We'll build up a single tool, `SearchProducts`, in three steps: the bare-minimum operation, optional settings, then an optional interactive view.

### Step 1: the GraphQL operation

The minimum a tool needs is a `.graphql` file. The operation runs against your GraphQL server (HotChocolate or Fusion) and the result is what the model sees.

`mcp/tools/SearchProducts/SearchProducts.graphql`:

```graphql
query SearchProducts(
  $text: String!
  $minPrice: Float
  $maxPrice: Float
  $first: Int!
  $after: String
) {
  products(
    searchText: $text
    minPrice: $minPrice
    maxPrice: $maxPrice
    first: $first
    after: $after
  ) {
    nodes {
      id
      name
      price
      pictureUrl
    }
  }
}
```

GraphQL variables become MCP tool arguments. The file's basename (here, `SearchProducts`) becomes the tool name, and the GraphQL document inside defines what runs when the tool is invoked. With just this file in place, `SearchProducts` is already a working MCP tool.

### Step 2: optional settings

When you want a custom title, icons, or behavior hints, add a sibling `.json` file with the same basename.

`mcp/tools/SearchProducts/SearchProducts.json`:

```json
{
  "title": "Search Products",
  "icons": [
    {
      "source": "https://example.com/favicon-32x32.png",
      "sizes": ["32x32"],
      "mimeType": "image/png",
      "theme": "dark"
    }
  ],
  "annotations": {
    "openWorldHint": false
  }
}
```

The `title` and `icons` are surfaced in the host's tool picker. Annotations like `destructiveHint` and `idempotentHint` tell the model how to think about side effects and retry safety.

### Step 3: an optional interactive view

MCP Apps is an extension to MCP that lets a server return interactive HTML the host renders inside the chat, instead of a plain text reply. The host loads your HTML into a sandboxed iframe and bridges JSON-RPC over `postMessage` to it, so the view can read tool results, call other tools, and follow the host's theme.

> Apps views render only in hosts that implement the MCP Apps extension. Plain-text MCP hosts ignore the view and render the tool result as text.

To attach a view, drop a sibling `.html` file next to the `.graphql` file with the same basename. That alone is enough: a static page with no script renders as-is. To make the view interactive, include a small JavaScript module that connects to the host via the MCP Apps SDK.

You can also add a `view` block to the tool's settings to tweak how the host frames the iframe (for example, `prefersBorder`):

```json
{
  "view": {
    "prefersBorder": false
  }
}
```

`mcp/tools/SearchProducts/SearchProducts.html`:

```html
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <title>Search Products</title>
    <style>
      body {
        font-family: system-ui, sans-serif;
        margin: 0;
        padding: 16px;
      }
      ul {
        list-style: none;
        padding: 0;
      }
      li {
        padding: 8px 0;
        border-bottom: 1px solid #eee;
      }
    </style>
  </head>
  <body>
    <ul id="products"></ul>

    <script type="module">
      import {
        App,
        applyDocumentTheme,
        applyHostStyleVariables,
        applyHostFonts,
      } from "https://cdn.jsdelivr.net/npm/@modelcontextprotocol/ext-apps@1.7.1/dist/src/app-with-deps.js";

      const app = new App({ name: "SearchProducts", version: "1.0.0" });

      // Assign the result handler BEFORE connect() so the initial tool result is not missed.
      app.ontoolresult = (result) => {
        const products = result?.structuredContent?.data?.products?.nodes ?? [];
        const list = document.getElementById("products");
        list.innerHTML = "";
        for (const p of products) {
          const li = document.createElement("li");
          li.textContent = `${p.name} - $${p.price}`;
          list.appendChild(li);
        }
      };

      // Adopt the host's theme, CSS variables, and fonts so the view feels native.
      app.onhostcontextchanged = (ctx) => {
        if (ctx.theme) applyDocumentTheme(ctx.theme);
        if (ctx.styles?.variables)
          applyHostStyleVariables(ctx.styles.variables);
        if (ctx.styles?.css?.fonts) applyHostFonts(ctx.styles.css.fonts);
      };

      app.onteardown = async () => ({});

      await app.connect();

      // Host context may already be available, replay it through the same handler.
      const ctx = app.getHostContext();
      if (ctx) app.onhostcontextchanged(ctx);
    </script>
  </body>
</html>
```

<!-- spell-checker:ignore: ontoolresult onhostcontextchanged onteardown -->

> The example imports `@modelcontextprotocol/ext-apps` from jsDelivr to keep the HTML self-contained and easy to read. For larger views you would typically bundle the SDK with a tool like Vite and emit a single HTML file as the build output.

The script creates an `App` whose `name` matches the tool, registers handlers for incoming tool results, host theme changes, and teardown, then opens the bridge to the host with `app.connect()`. The `getHostContext()` replay at the end covers the case where the host's theme is already available before the handler is wired up.

For the full handler list, lifecycle details, and how to call other tools from a view, see the [MCP Apps SDK API reference](https://apps.extensions.modelcontextprotocol.io/api/).

## Author a prompt

Prompts are pure JSON. Each prompt has `arguments` (the inputs the user fills in) and `messages` (the templated conversation that gets handed to the model).

`mcp/prompts/SearchProducts/SearchProducts.json`:

```json
{
  "title": "Search Products",
  "description": "Search for products in the catalog based on a search query.",
  "icons": [
    {
      "source": "https://example.com/favicon-32x32.png",
      "sizes": ["32x32"],
      "mimeType": "image/png",
      "theme": "dark"
    }
  ],
  "arguments": [
    {
      "name": "searchQuery",
      "title": "Search Query",
      "description": "The search query to find relevant products.",
      "required": true
    }
  ],
  "messages": [
    {
      "role": "user",
      "content": {
        "type": "text",
        "text": "Find products related to \"{searchQuery}\" in the product catalog."
      }
    }
  ]
}
```

The `{searchQuery}` placeholder in the message text is interpolated from the matching argument by name. Any argument you declare in `arguments` is available as `{name}` inside `messages`.

## Settings reference

The full set of fields supported by the tool and prompt settings files.

### Tool settings

`{tool-name}.json` is an object. Every property is optional.

| Property      | Type          | Description                                                 |
| ------------- | ------------- | ----------------------------------------------------------- |
| `title`       | `string`      | Human-readable title shown in the host's tool picker.       |
| `icons`       | `Icon[]`      | Icons displayed alongside the title. See **Icon** below.    |
| `annotations` | `Annotations` | Behavior hints for the model. See **Annotations** below.    |
| `view`        | `View`        | Render configuration for the Apps view. See **View** below. |
| `visibility`  | `string[]`    | Who can call the tool. See **Visibility** below.            |

**Icon**

| Property   | Type                | Description                                                                                          |
| ---------- | ------------------- | ---------------------------------------------------------------------------------------------------- |
| `source`   | `string` (required) | URI of the icon. HTTPS URL or `data:` URI.                                                           |
| `mimeType` | `string`            | MIME type, for example `image/png`, `image/svg+xml`. Overrides the server's MIME type when supplied. |
| `sizes`    | `string[]`          | One or more size specifiers like `"32x32"` or `"any"`.                                               |
| `theme`    | `"light" \| "dark"` | The UI theme this icon is designed for.                                                              |

**Annotations**

All values are booleans. Each is a hint surfaced to the model.

| Property          | Description                                                           |
| ----------------- | --------------------------------------------------------------------- |
| `destructiveHint` | The operation may cause destructive side effects.                     |
| `idempotentHint`  | The operation is safe to retry.                                       |
| `openWorldHint`   | The operation interacts with an open-world system (no closed schema). |

**View**

| Property        | Type          | Description                                                       |
| --------------- | ------------- | ----------------------------------------------------------------- |
| `prefersBorder` | `boolean`     | Render the iframe with a visible border.                          |
| `domain`        | `string`      | Dedicated origin for the view sandbox.                            |
| `permissions`   | `Permissions` | Browser permissions the view requests. See **Permissions** below. |
| `csp`           | `Csp`         | Content Security Policy domain allowlists. See **CSP** below.     |

**Permissions**

All values are booleans. Each maps to a Permission Policy feature requested for the view's iframe.

| Property         | Description                     |
| ---------------- | ------------------------------- |
| `camera`         | Request camera access.          |
| `microphone`     | Request microphone access.      |
| `geolocation`    | Request geolocation access.     |
| `clipboardWrite` | Request clipboard write access. |

**CSP**

Each property is an array of origin strings.

| Property          | Description                                                |
| ----------------- | ---------------------------------------------------------- |
| `baseUriDomains`  | Allowed values for the `base-uri` directive.               |
| `connectDomains`  | Origins for fetch, XHR, and WebSocket requests.            |
| `frameDomains`    | Origins allowed in nested iframes (`frame-src` directive). |
| `resourceDomains` | Origins for scripts, images, styles, and fonts.            |

**Visibility**

A list of one or more values controlling who can call the tool. Use this to expose a helper tool only to other tools' Apps views without surfacing it to the agent.

| Value     | Description                                             |
| --------- | ------------------------------------------------------- |
| `"model"` | The tool is visible to and callable by the agent.       |
| `"app"`   | The tool is callable by Apps views on this server only. |

### Prompt settings

`{prompt-name}.json` is an object. `messages` is required, everything else is optional.

| Property      | Type                   | Description                                                            |
| ------------- | ---------------------- | ---------------------------------------------------------------------- |
| `messages`    | `Message[]` (required) | The templated conversation handed to the model. See **Message** below. |
| `title`       | `string`               | Human-readable title shown in the host's prompt picker.                |
| `description` | `string`               | Human-readable description.                                            |
| `arguments`   | `Argument[]`           | User-fillable arguments. See **Argument** below.                       |
| `icons`       | `Icon[]`               | Same shape as the tool `Icon` above.                                   |

**Argument**

| Property      | Type                | Description                                                      |
| ------------- | ------------------- | ---------------------------------------------------------------- |
| `name`        | `string` (required) | Argument name. Referenced as `{name}` inside message text.       |
| `title`       | `string`            | Display title in the host's prompt form.                         |
| `description` | `string`            | Help text shown alongside the input.                             |
| `required`    | `boolean`           | Whether the user must provide a value before running the prompt. |

**Message**

| Property  | Type                               | Description                          |
| --------- | ---------------------------------- | ------------------------------------ |
| `role`    | `"user" \| "assistant"` (required) | Role attached to the message.        |
| `content` | `Content` (required)               | Message body. See **Content** below. |

**Content**

Only text content is supported.

| Property | Type                | Description                                                                                |
| -------- | ------------------- | ------------------------------------------------------------------------------------------ |
| `type`   | `"text"` (required) | Always `"text"`.                                                                           |
| `text`   | `string` (required) | The message text. `{argName}` placeholders are interpolated from the prompt's `arguments`. |

# Publish with the Nitro CLI

The CLI does the heavy lifting: archive, validate, upload, publish.

## 1. Log in

```shell
nitro login
```

You only need to do this once per machine. CI environments authenticate with `--api-key` instead. See [Global Options](/docs/nitro/cli/global-options).

## 2. Create a feature collection

A collection is a named container for your tools and prompts. Create one for your API.

```shell
nitro mcp create \
  --name "<name>" \
  --api-id "<api-id>"
```

Get the API ID from `nitro api list` or the Nitro UI. The command prints the new collection's ID. Save it. Every subsequent command needs it.

See [`nitro mcp create`](/docs/nitro/cli/mcp#nitro-mcp-create) for the full reference.

## 3. Upload a version

Each upload is a complete snapshot tagged with a name (a release tag, a Git commit SHA, anything you want).

```shell
nitro mcp upload \
  --mcp-feature-collection-id "<collection-id>" \
  --tag "v1" \
  --tool-pattern "./mcp/tools/**/*.graphql" \
  --prompt-pattern "./mcp/prompts/**/*.json"
```

The CLI walks the glob patterns, finds the sibling `.json` and `.html` files automatically, packages everything into a ZIP archive, and uploads it. Nitro validates the archive on the server before storing it.

See [`nitro mcp upload`](/docs/nitro/cli/mcp#nitro-mcp-upload) for all options.

## 4. Publish to a stage

Uploading does not expose anything to clients. You must explicitly publish a tagged version to a stage.

```shell
nitro mcp publish \
  --mcp-feature-collection-id "<collection-id>" \
  --tag "v1" \
  --stage "dev"
```

Stages are independent: publishing to `dev` does not touch `production`. To roll back, publish an earlier tag to the same stage.

See [`nitro mcp publish`](/docs/nitro/cli/mcp#nitro-mcp-publish) for gated stages and approval flows.

## Optional: validate in CI

If you want to gate a deploy step in a separate pipeline job, run validation explicitly:

```shell
nitro mcp validate \
  --mcp-feature-collection-id "<collection-id>" \
  --stage "dev" \
  --tool-pattern "./mcp/tools/**/*.graphql" \
  --prompt-pattern "./mcp/prompts/**/*.json"
```

> Validation also runs automatically inside `nitro mcp publish`. Use the standalone command only when CI needs a separate gate.

For the full reference, see [Nitro CLI MCP commands](/docs/nitro/cli/mcp).

## Find your server URL

The MCP endpoint is hosted by your HotChocolate server, not by Nitro. Once a version is published, the runtime picks it up and serves it at the adapter's default route:

```text
https://<your-graphql-host>/graphql/mcp
```

Replace `<your-graphql-host>` with the public URL of your HotChocolate server or Fusion gateway. The path is configurable, but `/graphql/mcp` is the default and what most deployments use. Give that URL to your MCP clients.

## Test with MCP Inspector

Before wiring the server into a chat client, smoke-test it with [MCP Inspector](https://github.com/modelcontextprotocol/inspector), the official MCP debugging tool. It connects to your server URL, lists every tool and prompt, and lets you invoke them with arbitrary arguments.

```shell
npx @modelcontextprotocol/inspector
```

Open the printed URL in a browser, point it at `https://<your-graphql-host>/graphql/mcp`, and exercise your tools. Use this whenever you change a tool or prompt to confirm the server returns what you expect, without round-tripping through a chat host.

# Connect from an MCP client

Any MCP client can connect to the URL from the previous section. The exact UI for adding a remote MCP server differs per client and the menus tend to shift over time. The links below point at each vendor's current setup documentation.

- **ChatGPT**: [Connect from ChatGPT (Apps SDK)](https://developers.openai.com/apps-sdk/deploy/connect-chatgpt)
- **Claude**: [Get started with custom connectors using remote MCP](https://support.claude.com/en/articles/11175166-get-started-with-custom-connectors-using-remote-mcp)
- **VS Code**: [Add and manage MCP servers in VS Code](https://code.visualstudio.com/docs/copilot/customization/mcp-servers)
- **Other clients**: [MCP clients directory](https://modelcontextprotocol.io/clients)

In every client the inputs are the same: a name, a description, the MCP server URL, and authentication settings. Tools that ship an HTML resource render as interactive MCP Apps views in clients that support the extension. Plain-text clients render the tool result as text and ignore the view.

# Storage and telemetry

Nitro provides operational infrastructure around the collection so you do not have to build it yourself.

**Storage.** Each feature collection is workspace-scoped and tied to one API. Versions are immutable, tagged snapshots: every upload creates a new version, and new versions replace rather than merge with the previous one. Rollback is a republish of an earlier tag.

**Telemetry.** Every published tool tracks request count, error count, mean duration, P95 and P99 latency, operations per minute, error rate, and an impact score. Distributed traces are emitted so you can correlate tool calls with the rest of your GraphQL server. Error insights aggregate per error type with errors-per-minute and last-seen timestamps. All metrics are visible per stage in the Nitro UI.

**Validation.** Tool GraphQL documents are validated against your schema. Prompt JSON is validated for structure. The validator also checks for conflicts with other tools already published to the target stage. Failures block the publish.

**Stages and permissions.** Stages are independent: there is no automatic promotion from `dev` to `production`. Permissions are stage-scoped, so a user who can publish to `dev` cannot publish to `production` without the matching permission there.

# Next steps

- Full CLI reference: [Nitro CLI MCP commands](/docs/nitro/cli/mcp).
- The MCP specification: [modelcontextprotocol.io](https://modelcontextprotocol.io/).
- The MCP Apps SDK: [API reference](https://apps.extensions.modelcontextprotocol.io/api/).
