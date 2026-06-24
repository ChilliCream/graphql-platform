import type { Metadata } from "next";
import NextLink from "next/link";
import type {
  ComponentPropsWithoutRef,
  ComponentType,
  CSSProperties,
} from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CookieCrumble } from "@/src/icons/CookieCrumble";
import { GalaxusLogo } from "@/src/icons/GalaxusLogo";
import { GitHubIcon } from "@/src/icons/GitHub";
import { GreenDonut } from "@/src/icons/GreenDonut";
import { HotChocolate } from "@/src/icons/HotChocolate";
import { MicrosoftLogo } from "@/src/icons/MicrosoftLogo";
import { Mocha } from "@/src/icons/Mocha";
import { Nitro } from "@/src/icons/Nitro";
import { SlackIcon } from "@/src/icons/Slack";
import { StrawberryShake } from "@/src/icons/StrawberryShake";
import { SwissLifeLogo } from "@/src/icons/SwissLifeLogo";
import { YouTubeIcon } from "@/src/icons/YouTube";

export const metadata: Metadata = {
  title: "About ChilliCream",
  description:
    "About ChilliCream: the GraphQL platform for .NET teams shown as a specimen cabinet of six open-source products, from the Hot Chocolate server to the Nitro control plane.",
  keywords: [
    "ChilliCream",
    "About ChilliCream",
    "GraphQL platform",
    "Hot Chocolate",
    "Nitro",
    "Strawberry Shake",
    ".NET GraphQL",
    "Fusion",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "About ChilliCream",
    description:
      "We build the end-to-end GraphQL platform for .NET teams, curated as a specimen cabinet of six open-source products, open source and in the open on GitHub.",
  },
};

type ProductIcon = ComponentType<{
  readonly className?: string;
  readonly style?: CSSProperties;
}>;

interface Specimen {
  readonly catalog: string;
  readonly name: string;
  readonly icon: ProductIcon;
  readonly genus: string;
  readonly species: string;
  readonly habitat: string;
  readonly diet: string;
  readonly fact: string;
  readonly href: string;
  readonly linkLabel: string;
  readonly external?: boolean;
}

const SPECIMENS: readonly Specimen[] = [
  {
    catalog: "HC-001",
    name: "Hot Chocolate",
    icon: HotChocolate,
    genus: "Server",
    species: "hotchocolate-net",
    habitat: ".NET 8+, ASP.NET Core",
    diet: "GraphQL operations",
    fact: "The source-generated GraphQL server at the heart of the platform, schema-first and code-first, with queries, mutations, and subscriptions.",
    href: "/products/hotchocolate",
    linkLabel: "Hot Chocolate",
  },
  {
    catalog: "SS-002",
    name: "Strawberry Shake",
    icon: StrawberryShake,
    genus: "Client",
    species: "strawberry-shake",
    habitat: ".NET 8+, MSBuild",
    diet: "GraphQL queries",
    fact: "A typed .NET client driven by MSBuild codegen: write a query, get a fully typed C# API without hand-written DTOs.",
    href: "/products/strawberryshake",
    linkLabel: "Strawberry Shake",
  },
  {
    catalog: "NT-003",
    name: "Nitro",
    icon: Nitro,
    genus: "Platform",
    species: "nitro-control-plane",
    habitat: "Cloud, self-hosted",
    diet: "Schemas, clients, telemetry",
    fact: "The control plane and GraphQL IDE: schema and client registry, CI checks, and observability once Nitro is configured for it.",
    href: "https://nitro.chillicream.com",
    linkLabel: "Nitro",
    external: true,
  },
  {
    catalog: "MC-004",
    name: "Mocha",
    icon: Mocha,
    genus: "Library",
    species: "mocha-mediator",
    habitat: ".NET 8+, in-process and cross-service",
    diet: "Messages and sagas",
    fact: "A source-generated mediator and messaging library with exactly-once processing and the same model in-process and across services.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Mocha",
    external: true,
  },
  {
    catalog: "GD-005",
    name: "Green Donut",
    icon: GreenDonut,
    genus: "Library",
    species: "green-donut",
    habitat: ".NET 8+, Hot Chocolate",
    diet: "Batched data access",
    fact: "The DataLoader behind Hot Chocolate: batches and caches data access so resolvers stay simple and the N+1 problem stays solved.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Green Donut",
    external: true,
  },
  {
    catalog: "CC-006",
    name: "Cookie Crumble",
    icon: CookieCrumble,
    genus: "Library",
    species: "cookie-crumble",
    habitat: ".NET 8+, test projects",
    diet: "Snapshots",
    fact: "GraphQL-aware snapshot testing with native support for execution results, HTTP responses, and Markdown snapshots.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Cookie Crumble",
    external: true,
  },
];

interface Sighting {
  readonly logo: ComponentType<{
    readonly className?: string;
    readonly style?: CSSProperties;
  }>;
  readonly label: string;
  readonly coordinates: string;
  readonly logoClass: string;
}

const SIGHTINGS: readonly Sighting[] = [
  {
    logo: GalaxusLogo,
    label: "Galaxus",
    coordinates: "FS-01 / e-commerce",
    logoClass: "h-7 w-auto sm:h-8",
  },
  {
    logo: SwissLifeLogo,
    label: "Swiss Life",
    coordinates: "FS-02 / insurance",
    logoClass: "h-12 w-auto sm:h-14",
  },
  {
    logo: MicrosoftLogo,
    label: "Microsoft",
    coordinates: "FS-03 / software",
    logoClass: "h-7 w-auto sm:h-8",
  },
];

interface LivingSpecimen {
  readonly catalog: string;
  readonly icon: ComponentType<ComponentPropsWithoutRef<"svg">>;
  readonly label: string;
  readonly observe: string;
  readonly note: string;
  readonly href: string;
}

const LIVING: readonly LivingSpecimen[] = [
  {
    catalog: "LV-01",
    icon: GitHubIcon,
    label: "GitHub",
    observe: "source habitat",
    note: "Read the code, file issues, send pull requests. The platform is built in the open.",
    href: "https://github.com/ChilliCream/graphql-platform",
  },
  {
    catalog: "LV-02",
    icon: SlackIcon,
    label: "Slack",
    observe: "community habitat",
    note: "Ask questions, trade patterns, and talk to the people who build the platform.",
    href: "https://slack.chillicream.com/",
  },
  {
    catalog: "LV-03",
    icon: YouTubeIcon,
    label: "YouTube",
    observe: "field recordings",
    note: "Talks, deep dives, and walkthroughs from the team and the wider community.",
    href: "https://www.youtube.com/c/ChilliCream",
  },
];

const LEDGER_STYLE: CSSProperties = {
  backgroundImage:
    "radial-gradient(rgba(94,234,212,0.06) 1px, transparent 1px), linear-gradient(to bottom, transparent 95px, rgba(245,241,234,0.12) 95px, rgba(245,241,234,0.12) 96px)",
  backgroundSize: "24px 24px, 100% 96px",
};

const SECTION_RULE =
  "border-cc-card-border mt-16 border-t pt-12 sm:mt-20 sm:pt-16";

export default function AboutPreviewV9Page() {
  return (
    <>
      <style>{breathingKeyframes}</style>

      <div className="pointer-events-none fixed inset-0 -z-10 opacity-60">
        <div className="absolute inset-0" style={LEDGER_STYLE} />
      </div>

      <IndexStrip />
      <Hero />
      <Collection />
      <FieldSightings />
      <TaxonomyNote />
      <LivingSpecimens />
      <Colophon />
    </>
  );
}

function SectionCode({
  code,
  title,
}: {
  readonly code: string;
  readonly title: string;
}) {
  return (
    <p className="text-cc-nav-label font-mono text-[11px] tracking-[0.28em] uppercase">
      {code} <span className="text-cc-ink-dim">/ {title}</span>
    </p>
  );
}

function SpectrumBand() {
  return (
    <div
      aria-hidden="true"
      className="h-0.5 w-full"
      style={{
        background:
          "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
      }}
    />
  );
}

function IndexStrip() {
  return (
    <section aria-label="Catalog index">
      <div className="border-cc-card-border bg-cc-surface/60 flex flex-wrap items-center justify-between gap-x-6 gap-y-2 rounded-t-xl border px-5 py-3">
        <p className="text-cc-nav-label font-mono text-[11px] tracking-[0.24em] uppercase">
          ChilliCream Specimen Cabinet
        </p>
        <p className="text-cc-ink-dim font-mono text-[11px] tracking-[0.24em] uppercase">
          Est. Open Source
        </p>
        <p className="text-cc-nav-label font-mono text-[11px] tracking-[0.24em] uppercase">
          Vol. I &middot; No. 00-006
        </p>
      </div>
      <SpectrumBand />
    </section>
  );
}

function Hero() {
  return (
    <section
      aria-labelledby="about-title"
      className="border-cc-card-border mt-8 rounded-2xl border p-6 sm:p-10"
    >
      <SectionCode code="SPC.001" title="Hero" />
      <div className="border-cc-card-border mt-6 border p-6 sm:p-10">
        <p className="text-cc-nav-label mb-4 font-mono text-xs tracking-[0.24em] uppercase">
          Curator&rsquo;s plaque
        </p>
        <h1
          id="about-title"
          className="font-heading text-cc-heading text-h1 max-w-3xl tracking-tight"
        >
          About <span className="text-cc-accent">ChilliCream</span>.
        </h1>
        <p className="text-cc-ink mt-6 max-w-2xl text-base leading-relaxed sm:text-lg">
          We curate a small collection: six open-source products that form an
          end-to-end GraphQL platform for .NET teams. The server, the typed
          client, the control plane, and the libraries to test and ship it are
          pinned here as specimens, each tagged with what it actually is. All of
          it lives in the open on GitHub.
        </p>
        <div className="mt-8 flex flex-wrap items-center gap-3">
          <SolidButton href="/products">View products</SolidButton>
          <OutlineButton href="/docs">Read the docs</OutlineButton>
        </div>
      </div>
    </section>
  );
}

function Collection() {
  return (
    <section aria-labelledby="collection-title" className={SECTION_RULE}>
      <SectionCode code="SPC.002" title="The Collection" />
      <h2
        id="collection-title"
        className="font-heading text-cc-heading text-h3 mt-4 max-w-2xl tracking-tight"
      >
        Six specimens. One platform.
      </h2>
      <p className="text-cc-ink mt-4 max-w-2xl text-base leading-relaxed">
        Every entry is a real, shipping project in the
        ChilliCream/graphql-platform repository. Adopt one on its own, or
        compose the full stack.
      </p>

      <div className="mt-10 grid grid-cols-1 gap-5 sm:grid-cols-2 md:grid-cols-3">
        {SPECIMENS.map((specimen, index) => (
          <SpecimenCard key={specimen.name} specimen={specimen} index={index} />
        ))}
      </div>

      <p className="text-cc-ink-dim mt-8 max-w-2xl font-mono text-xs leading-relaxed tracking-wide">
        Note: Fusion, our distributed gateway, is built on Hot Chocolate, lives
        in the same repository, and composes the graph at planning time while
        you run the gateway yourself.
      </p>
    </section>
  );
}

function RegistrationMarks({ index }: { readonly index: number }) {
  const delay = `${index * 0.8}s`;
  return (
    <div
      aria-hidden="true"
      className="text-cc-accent pointer-events-none absolute inset-2"
      style={{ animation: `spc-breathe 6s ease-in-out ${delay} infinite` }}
    >
      <span className="absolute top-0 left-0 block h-2.5 w-2.5 border-t border-l border-current" />
      <span className="absolute top-0 right-0 block h-2.5 w-2.5 border-t border-r border-current" />
      <span className="absolute bottom-0 left-0 block h-2.5 w-2.5 border-b border-l border-current" />
      <span className="absolute right-0 bottom-0 block h-2.5 w-2.5 border-r border-b border-current" />
    </div>
  );
}

function SpecimenCard({
  specimen,
  index,
}: {
  readonly specimen: Specimen;
  readonly index: number;
}) {
  const { icon: Icon } = specimen;
  const arrow = <span aria-hidden="true">&rarr;</span>;
  const link =
    "text-cc-accent mt-5 inline-flex items-center gap-1.5 font-mono text-[11px] tracking-[0.18em] uppercase hover:underline";

  return (
    <article className="border-cc-card-border group bg-cc-card-bg relative rounded-xl border p-2.5">
      <div className="border-cc-card-border group-hover:border-cc-card-border-hover relative flex h-full flex-col border p-5 transition-colors">
        <RegistrationMarks index={index} />

        <p className="text-cc-nav-label font-mono text-[10px] tracking-[0.24em] uppercase opacity-0 transition-opacity duration-300 group-hover:opacity-100">
          Catalog # {specimen.catalog}
        </p>

        <div className="mt-2 flex justify-center">
          <Icon className="h-16 w-16 object-contain" />
        </div>

        <h3 className="font-heading text-cc-heading text-h5 mt-4 text-center leading-tight tracking-tight">
          {specimen.name}
        </h3>

        <dl className="border-cc-card-border mt-4 grid grid-cols-[auto_1fr] gap-x-3 gap-y-1.5 border-t pt-4 font-mono text-[11px] tracking-[0.08em] uppercase">
          <ClassificationRow term="Genus" value={specimen.genus} />
          <ClassificationRow term="Species" value={specimen.species} />
          <ClassificationRow term="Habitat" value={specimen.habitat} />
          <ClassificationRow term="Discovered" value="Open Source" />
          <ClassificationRow term="Diet" value={specimen.diet} />
        </dl>

        <p className="text-cc-ink mt-4 text-sm leading-relaxed">
          {specimen.fact}
        </p>

        <div className="mt-auto">
          {specimen.external ? (
            <a
              href={specimen.href}
              target="_blank"
              rel="noopener noreferrer"
              className={link}
            >
              View specimen {arrow}
            </a>
          ) : (
            <NextLink href={specimen.href} className={link}>
              View specimen {arrow}
            </NextLink>
          )}
        </div>
      </div>
    </article>
  );
}

function ClassificationRow({
  term,
  value,
}: {
  readonly term: string;
  readonly value: string;
}) {
  return (
    <>
      <dt className="text-cc-nav-label">{term}</dt>
      <dd className="text-cc-ink-dim normal-case">{value}</dd>
    </>
  );
}

function FieldSightings() {
  return (
    <section aria-labelledby="sightings-title" className={SECTION_RULE}>
      <SectionCode code="SPC.003" title="Field Sightings" />
      <h2
        id="sightings-title"
        className="font-heading text-cc-heading text-h3 mt-4 max-w-2xl tracking-tight"
      >
        Sighted in production.
      </h2>
      <p className="text-cc-ink mt-4 max-w-2xl text-base leading-relaxed">
        The collection turns up in the wild. These teams run ChilliCream in
        production.
      </p>

      <div className="mt-10 grid grid-cols-1 gap-5 sm:grid-cols-3">
        {SIGHTINGS.map((sighting) => {
          const { logo: Logo } = sighting;
          return (
            <div
              key={sighting.label}
              className="border-cc-card-border bg-cc-card-bg rounded-xl border p-2.5"
            >
              <div className="border-cc-card-border flex flex-col items-center border px-5 py-7">
                <div className="text-cc-heading flex h-12 items-center justify-center">
                  <Logo className={sighting.logoClass} />
                </div>
                <p className="text-cc-nav-label mt-5 font-mono text-[10px] tracking-[0.22em] uppercase">
                  {sighting.coordinates}
                </p>
                <p className="text-cc-ink-dim mt-1 font-mono text-[10px] tracking-[0.22em] uppercase">
                  sighted in production
                </p>
              </div>
            </div>
          );
        })}
      </div>
    </section>
  );
}

function TaxonomyNote() {
  return (
    <section aria-labelledby="taxonomy-title" className={SECTION_RULE}>
      <SectionCode code="SPC.004" title="Taxonomy Note" />
      <h2
        id="taxonomy-title"
        className="font-heading text-cc-heading text-h3 mt-4 max-w-2xl tracking-tight"
      >
        A note from the curator.
      </h2>
      <div className="text-cc-ink mt-6 grid gap-6 text-sm leading-relaxed sm:grid-cols-2 sm:text-base">
        <p>
          Precision matters in a cabinet, so here is what each specimen actually
          is. Strawberry Shake generates its typed client through MSBuild
          codegen, not runtime reflection. Hot Chocolate is source-generated, so
          your schema is wired up at build time.
        </p>
        <p>
          Mocha processes messages exactly once and validates its sagas before
          traffic reaches them, not at compile time. Fusion composes the graph
          at planning time and then steps aside: you always run the gateway
          yourself. We describe what ships, and we name the tradeoffs.
        </p>
      </div>
    </section>
  );
}

function LivingSpecimens() {
  return (
    <section aria-labelledby="living-title" className={SECTION_RULE}>
      <SectionCode code="SPC.005" title="Living Specimens" />
      <h2
        id="living-title"
        className="font-heading text-cc-heading text-h3 mt-4 max-w-2xl tracking-tight"
      >
        Where to observe in the wild.
      </h2>
      <p className="text-cc-ink mt-4 max-w-2xl text-base leading-relaxed">
        The work happens in public. These are the live habitats where you can
        watch the platform grow.
      </p>

      <div className="mt-10 grid grid-cols-1 gap-5 sm:grid-cols-3">
        {LIVING.map((channel, index) => {
          const { icon: Icon } = channel;
          return (
            <a
              key={channel.label}
              href={channel.href}
              target="_blank"
              rel="noopener noreferrer"
              className="border-cc-card-border bg-cc-card-bg group block rounded-xl border p-2.5"
            >
              <div className="border-cc-card-border group-hover:border-cc-card-border-hover relative flex h-full flex-col border p-5 transition-colors">
                <RegistrationMarks index={index} />
                <div className="flex items-center justify-between">
                  <span className="text-cc-heading group-hover:text-cc-accent h-7 w-7 transition-colors">
                    <Icon className="h-7 w-7 fill-current" />
                  </span>
                  <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.22em] uppercase">
                    {channel.catalog}
                  </span>
                </div>
                <h3 className="font-heading text-cc-heading text-h6 mt-4 tracking-tight">
                  {channel.label}
                </h3>
                <p className="text-cc-nav-label mt-1 font-mono text-[10px] tracking-[0.22em] uppercase">
                  {channel.observe}
                </p>
                <p className="text-cc-ink mt-3 text-sm leading-relaxed">
                  {channel.note}
                </p>
              </div>
            </a>
          );
        })}
      </div>
    </section>
  );
}

function Colophon() {
  return (
    <section aria-labelledby="colophon-title" className={SECTION_RULE}>
      <SectionCode code="SPC.006" title="Colophon" />
      <div className="border-cc-card-border bg-cc-surface/60 mt-6 rounded-xl border p-6 sm:p-10">
        <p className="text-cc-nav-label font-mono text-[11px] leading-loose tracking-[0.22em] uppercase">
          Curated in the open
          <br />
          Source on GitHub
          <br />
          License MIT
        </p>
        <h2
          id="colophon-title"
          className="font-heading text-cc-heading text-h4 mt-6 max-w-2xl tracking-tight"
        >
          Take a closer look at the collection.
        </h2>
        <div className="mt-8 flex flex-wrap items-center gap-3">
          <SolidButton href="/products">View products</SolidButton>
          <OutlineButton href="/docs">Read the docs</OutlineButton>
        </div>
      </div>
    </section>
  );
}

const breathingKeyframes = `@keyframes spc-breathe {
  0%, 100% { opacity: 0.35; }
  50% { opacity: 0.7; }
}
@media (prefers-reduced-motion: reduce) {
  [style*="spc-breathe"] { animation: none !important; opacity: 0.5 !important; }
}`;
