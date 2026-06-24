import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { StrawberryShake } from "@/src/icons/StrawberryShake";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Strawberry Shake for .NET, end to end",
  description:
    "Strawberry Shake is the strongly-typed GraphQL client for .NET. Author an operation, generate a typed client with MSBuild, call it from Blazor or MAUI.",
  keywords: [
    "Strawberry Shake",
    "GraphQL client",
    ".NET",
    "Blazor",
    "MAUI",
    "MSBuild code generation",
    "dotnet graphql",
    "reactive store",
    "subscriptions",
    "ChilliCream",
  ],
  openGraph: {
    title: "Strawberry Shake for .NET, end to end",
    description:
      "From a .graphql operation to a strongly-typed C# client in one MSBuild build. Reactive store, subscriptions, Blazor and MAUI ready.",
  },
  robots: { index: false, follow: false },
};

interface StepProps {
  readonly index: number;
  readonly eyebrow: string;
  readonly title: string;
  readonly description: string;
  readonly children: ReactNode;
}

function Step({ index, eyebrow, title, description, children }: StepProps) {
  return (
    <div className="relative grid gap-8 lg:grid-cols-12 lg:items-start">
      <div className="lg:col-span-5">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          {eyebrow}
        </div>
        <div className="flex items-baseline gap-4">
          <span
            aria-hidden
            className="font-heading text-cc-ink-dim text-4xl leading-none"
          >
            {String(index).padStart(2, "0")}
          </span>
          <h3 className="font-heading text-cc-heading text-h3 leading-tight">
            {title}
          </h3>
        </div>
        <p className="text-cc-ink lead mt-4 max-w-md text-base">
          {description}
        </p>
      </div>
      <div className="lg:col-span-7">{children}</div>
    </div>
  );
}

interface SnippetProps {
  readonly label: string;
  readonly language: string;
  readonly children: ReactNode;
}

function Snippet({ label, language, children }: SnippetProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border">
      <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-2">
        <span className="text-cc-ink-dim font-mono text-xs tracking-wider uppercase">
          {label}
        </span>
        <span className="text-cc-ink-dim font-mono text-[10px] tracking-wider uppercase">
          {language}
        </span>
      </div>
      <pre className="overflow-x-auto p-4 font-mono text-[12.5px] leading-relaxed">
        <code className="text-cc-ink">{children}</code>
      </pre>
    </div>
  );
}

interface FeatureProps {
  readonly title: string;
  readonly description: string;
  readonly bullets: readonly string[];
}

function Feature({ title, description, bullets }: FeatureProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover rounded-xl border p-6 transition-colors">
      <h3 className="font-heading text-cc-heading text-h5 leading-tight">
        {title}
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm">{description}</p>
      <ul className="mt-4 space-y-2">
        {bullets.map((b) => (
          <li
            key={b}
            className="text-cc-ink flex items-start gap-2 text-sm leading-snug"
          >
            <span className="text-cc-accent mt-1 inline-flex h-4 w-4 flex-none items-center justify-center">
              <CheckIcon />
            </span>
            <span>{b}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

export default function StrawberryShakeStoryPreview() {
  return (
    <>
      <section className="relative pt-20 pb-16 sm:pt-28 sm:pb-24">
        <div className="mx-auto max-w-5xl px-6 text-center">
          <div className="text-cc-nav-label mb-5 inline-flex items-center gap-3 font-mono text-xs font-semibold tracking-widest uppercase">
            <span
              aria-hidden
              className="inline-block h-px w-8 bg-gradient-to-r from-transparent to-current"
            />
            <span>Strawberry Shake for .NET</span>
            <span
              aria-hidden
              className="inline-block h-px w-8 bg-gradient-to-l from-transparent to-current"
            />
          </div>
          <h1 className="font-heading text-cc-heading text-hero mx-auto max-w-4xl leading-[1.05] tracking-tight">
            From C# server to typed C# client,{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{
                backgroundImage:
                  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
              }}
            >
              without a runtime parse
            </span>
            .
          </h1>
          <p className="text-cc-ink lead mx-auto mt-8 max-w-2xl">
            Write a .graphql operation, build your project, call it from Blazor
            or MAUI. Strawberry Shake generates the typed .NET client during
            MSBuild, then keeps results live in a reactive normalized store.
          </p>
          <div className="mt-10 flex flex-wrap justify-center gap-4">
            <SolidButton href="/docs/strawberryshake">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
          <div className="text-cc-ink-dim mt-8 flex flex-wrap items-center justify-center gap-x-6 gap-y-2 font-mono text-xs tracking-wider uppercase">
            <span>MIT licensed</span>
            <span aria-hidden className="opacity-50">
              /
            </span>
            <span>MSBuild codegen</span>
            <span aria-hidden className="opacity-50">
              /
            </span>
            <span>Blazor, MAUI, WPF, ASP.NET</span>
          </div>
          <StrawberryShake className="pointer-events-none absolute top-10 right-6 hidden h-48 w-48 opacity-30 lg:block" />
        </div>
      </section>

      <section className="py-12 sm:py-16">
        <div className="mx-auto max-w-6xl px-6">
          <div className="text-cc-nav-label mb-12 text-center font-mono text-xs font-semibold tracking-widest uppercase">
            The arc, three steps
          </div>
          <div className="space-y-20">
            <Step
              index={1}
              eyebrow="Author"
              title="Drop a .graphql file next to your code."
              description="Operations live in plain .graphql files, the same dialect your server speaks. No magic attributes, no hand-written DTOs, just GraphQL."
            >
              <Snippet
                label="Queries/GetSession.graphql"
                language="GraphQL"
              >{`query GetSession($id: ID!) {
  session(id: $id) {
    id
    title
    startsAt
    speakers {
      id
      name
      avatarUrl
    }
  }
}

subscription OnSessionUpdated($id: ID!) {
  onSessionUpdated(sessionId: $id) {
    id
    title
    startsAt
  }
}
`}</Snippet>
            </Step>

            <Step
              index={2}
              eyebrow="Generate"
              title="MSBuild emits the typed client at build time."
              description="The dotnet graphql CLI scaffolds the project; from then on every build runs MSBuild code generation. Types, interfaces, fragments, store updates, all C#, all yours to step into."
            >
              <div className="space-y-4">
                <Snippet
                  label="Terminal"
                  language="bash"
                >{`# one time, scaffolds .graphqlrc.json and pulls the schema
dotnet new tool-manifest
dotnet tool install StrawberryShake.Tools --local

dotnet graphql init https://localhost:5001/graphql -n ConferenceClient
dotnet graphql download https://localhost:5001/graphql

# every build from now on regenerates the client
dotnet build
`}</Snippet>
                <Snippet
                  label="obj/Generated/ConferenceClient.Client.g.cs"
                  language="C#"
                >{`// generated by MSBuild code generation at build time
public partial interface IConferenceClient
{
    IGetSessionQuery GetSession { get; }
    IOnSessionUpdatedSubscription OnSessionUpdated { get; }
}

public partial interface IGetSession_Session
{
    string Id { get; }
    string Title { get; }
    DateTimeOffset StartsAt { get; }
    IReadOnlyList<IGetSession_Session_Speakers> Speakers { get; }
}
`}</Snippet>
              </div>
            </Step>

            <Step
              index={3}
              eyebrow="Consume"
              title="Inject the client, watch results stream in."
              description="Strawberry Shake registers as a typed service. Queries return strongly-typed results; Watch keeps them reactive, refetching, merging into the normalized entity store, and notifying your Blazor or MAUI component."
            >
              <Snippet
                label="Components/SessionView.razor.cs"
                language="C#"
              >{`public partial class SessionView : ComponentBase, IDisposable
{
    [Inject] public required IConferenceClient Client { get; init; }
    [Parameter] public required string SessionId { get; set; }

    private IGetSession_Session? _session;
    private IDisposable? _subscription;

    protected override void OnInitialized()
    {
        // Cache-and-network: render cached data instantly,
        // then refresh from the server and re-render.
        _subscription = Client.GetSession
            .Watch(SessionId, ExecutionStrategy.CacheAndNetwork)
            .Where(r => !r.Errors.Any())
            .Subscribe(r =>
            {
                _session = r.Data?.Session;
                InvokeAsync(StateHasChanged);
            });
    }

    public void Dispose() => _subscription?.Dispose();
}
`}</Snippet>
            </Step>
          </div>
        </div>
      </section>

      <section className="py-16 sm:py-20">
        <div className="mx-auto max-w-5xl px-6">
          <div className="mb-10 text-center">
            <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
              Same operation, on the server
            </div>
            <h2 className="font-heading text-cc-heading text-h2 leading-tight">
              The other half of the round trip.
            </h2>
            <p className="text-cc-ink-dim lead mx-auto mt-4 max-w-2xl">
              Strawberry Shake is paired with Hot Chocolate, so the .graphql
              your client generates from is the same one your team explores in
              Nitro against the live server.
            </p>
          </div>
          <div className="border-cc-card-border bg-cc-card-bg mx-auto max-w-5xl overflow-hidden rounded-xl border">
            <NitroCompose />
          </div>
          <p className="text-cc-ink-dim mt-4 text-center font-mono text-xs tracking-wider uppercase">
            Nitro, the GraphQL IDE, running the same operation against your
            server.
          </p>
        </div>
      </section>

      <section className="py-16 sm:py-20">
        <div className="mx-auto max-w-6xl px-6">
          <div className="mb-10 text-center">
            <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
              What you get out of the box
            </div>
            <h2 className="font-heading text-cc-heading text-h2 leading-tight">
              Four pillars, no plumbing left to you.
            </h2>
          </div>
          <div className="grid gap-6 sm:grid-cols-2">
            <Feature
              title="Strongly-typed Client"
              description="Operations in .graphql files become first-class C# interfaces, records, and enums. The compiler catches drift between server schema and client code."
              bullets={[
                "One type per operation, fragment, and union case",
                "Nullable reference types respect schema nullability",
                "Works against any spec-compliant GraphQL server",
              ]}
            />
            <Feature
              title="Reactive Normalized Store"
              description="Results normalize into entities. Every query, mutation, and subscription updates the store, and every Watch re-renders the parts of the UI that depend on what changed."
              bullets={[
                "Relay and Apollo style entity vocabulary",
                "Cache-first, network-only, cache-and-network strategies",
                "Persisted state to SQLite for instant cold starts",
              ]}
            />
            <Feature
              title="Subscriptions, Reactive End to End"
              description="GraphQL subscriptions over WebSocket or Server-Sent Events flow through the same Watch API as queries. No second client, no second mental model."
              bullets={[
                "WebSocket and SSE transports out of the box",
                "Authenticate via connection_init payload",
                "Subscription updates patch the same normalized store",
              ]}
            />
            <Feature
              title="MSBuild Code Generation"
              description="The dotnet graphql CLI generates typed .NET clients at build time. Generation runs as an MSBuild step, so a clean build always produces the matching client."
              bullets={[
                "No IL weaving, no runtime parse of the document",
                "Persisted operations supported, hashes shipped to the server",
                "Generated files land under obj/, ready to step into",
              ]}
            />
          </div>
        </div>
      </section>

      <section className="py-16 sm:py-20">
        <div className="mx-auto max-w-5xl px-6">
          <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border">
            <div className="grid gap-8 p-8 sm:grid-cols-12 sm:items-center sm:p-10">
              <div className="sm:col-span-7">
                <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
                  Open source, MIT licensed
                </div>
                <h2 className="font-heading text-cc-heading text-h3 leading-tight">
                  Free for any project, commercial or otherwise.
                </h2>
                <p className="text-cc-ink-dim mt-3 max-w-xl text-base">
                  Strawberry Shake is part of the ChilliCream GraphQL platform.
                  No per-developer fee, no broker tax, no separate license for
                  production use. The source, the tests, the issues, all on
                  GitHub.
                </p>
              </div>
              <ul className="text-cc-ink space-y-2 text-sm sm:col-span-5">
                <li className="flex items-start gap-2">
                  <span className="text-cc-accent mt-1 inline-flex h-4 w-4 flex-none items-center justify-center">
                    <CheckIcon />
                  </span>
                  <span>MIT License, no contributor agreement gate</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-cc-accent mt-1 inline-flex h-4 w-4 flex-none items-center justify-center">
                    <CheckIcon />
                  </span>
                  <span>
                    Runs against any GraphQL server, not just Hot Chocolate
                  </span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-cc-accent mt-1 inline-flex h-4 w-4 flex-none items-center justify-center">
                    <CheckIcon />
                  </span>
                  <span>
                    Works in any .NET app: Blazor and Razor are first class
                  </span>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      <section className="py-20 sm:py-28">
        <div className="mx-auto max-w-3xl px-6 text-center">
          <div className="text-cc-nav-label mb-4 font-mono text-xs font-semibold tracking-widest uppercase">
            Ready when you are
          </div>
          <h2 className="font-heading text-cc-heading text-h2 leading-tight">
            One operation, one build, one typed client.
          </h2>
          <p className="text-cc-ink-dim lead mt-5">
            Start the tutorial, scaffold a client against your own schema, and
            see your first typed result light up in minutes.
          </p>
          <div className="mt-10 flex flex-wrap justify-center gap-4">
            <SolidButton href="/docs/strawberryshake">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </>
  );
}
