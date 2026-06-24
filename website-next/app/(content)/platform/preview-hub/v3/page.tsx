import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroReel } from "@/src/nitro";

export const metadata: Metadata = {
  title: "The ChilliCream Platform",
  description:
    "Tour the ChilliCream GraphQL platform: eight product surfaces and the Nitro control plane that connects every API across your organization in one home.",
  keywords: [
    "ChilliCream platform",
    "GraphQL platform",
    "Nitro",
    "GraphQL observability",
    "GraphQL release safety",
    "GraphQL analytics",
    "GraphQL workflows",
    "GraphQL ecosystem",
    "continuous integration",
    "agentic coding",
  ],
  openGraph: {
    title: "The ChilliCream Platform",
    description:
      "One product, eight surfaces. Watch Nitro in motion, then jump into Build, Observability, Release Safety, Workflows, Analytics, CI, and the ecosystem.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Brand spectrum: used exactly once on this screen, in the headline.        */
/* -------------------------------------------------------------------------- */

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

/* -------------------------------------------------------------------------- */
/*  Capability surfaces. Every href is a real route under /platform/*.        */
/* -------------------------------------------------------------------------- */

interface Surface {
  readonly title: string;
  readonly tagline: string;
  readonly href: string;
  readonly icon: (props: { readonly className?: string }) => ReactNode;
}

const SURFACES: readonly Surface[] = [
  {
    title: "Build",
    tagline: "Author the API in the code that runs it.",
    href: "/platform/build",
    icon: BuildIcon,
  },
  {
    title: "Agentic Coding",
    tagline: "A feedback loop coding agents can read.",
    href: "/platform/agentic-coding",
    icon: AgentIcon,
  },
  {
    title: "Observability",
    tagline: "See every operation, field, and trace.",
    href: "/platform/observability",
    icon: ObservabilityIcon,
  },
  {
    title: "Workflows",
    tagline: "Durable work that survives the request.",
    href: "/platform/workflows",
    icon: WorkflowsIcon,
  },
  {
    title: "Release Safety",
    tagline: "Ship change without surprising clients.",
    href: "/platform/release-safety",
    icon: SafetyIcon,
  },
  {
    title: "Analytics",
    tagline: "Know which fields earn their keep.",
    href: "/platform/analytics",
    icon: AnalyticsIcon,
  },
  {
    title: "Continuous Integration",
    tagline: "Schema and composition checked on PR.",
    href: "/platform/continuous-integration",
    icon: CiIcon,
  },
  {
    title: "Ecosystem",
    tagline: "IDE, typed clients, DataLoaders, all in one.",
    href: "/platform/ecosystem",
    icon: EcosystemIcon,
  },
];

/* -------------------------------------------------------------------------- */
/*  Inline icons. Each one inherits currentColor so the cards stay coherent.  */
/* -------------------------------------------------------------------------- */

interface IconProps {
  readonly className?: string;
}

function BuildIcon({ className }: IconProps) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden>
      <path
        d="M4 7 L10 4 L20 8 L14 11 Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
      <path
        d="M4 7 L4 16 L14 20 L14 11"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
      <path
        d="M20 8 L20 17 L14 20"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function AgentIcon({ className }: IconProps) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden>
      <rect
        x="4"
        y="6"
        width="16"
        height="12"
        rx="3"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <circle cx="9" cy="12" r="1.4" fill="currentColor" />
      <circle cx="15" cy="12" r="1.4" fill="currentColor" />
      <path
        d="M12 3 L12 6"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
      <circle cx="12" cy="2.5" r="1" fill="currentColor" />
    </svg>
  );
}

function ObservabilityIcon({ className }: IconProps) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden>
      <path
        d="M3 17 L8 12 L12 14 L16 8 L21 11"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M3 20 L21 20"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        opacity="0.5"
      />
      <circle cx="16" cy="8" r="1.6" fill="currentColor" />
    </svg>
  );
}

function WorkflowsIcon({ className }: IconProps) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden>
      <circle cx="5" cy="12" r="2" fill="currentColor" />
      <circle
        cx="12"
        cy="12"
        r="2"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <circle
        cx="19"
        cy="12"
        r="2"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path
        d="M7 12 L10 12 M14 12 L17 12"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

function SafetyIcon({ className }: IconProps) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden>
      <path
        d="M12 3 L20 6 L20 12 C20 16.5 16.5 19.5 12 21 C7.5 19.5 4 16.5 4 12 L4 6 Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
      <path
        d="M8.5 12 L11 14.5 L15.5 10"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function AnalyticsIcon({ className }: IconProps) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden>
      <rect x="4" y="13" width="3" height="7" rx="1" fill="currentColor" />
      <rect x="10" y="9" width="3" height="11" rx="1" fill="currentColor" />
      <rect
        x="16"
        y="5"
        width="3"
        height="15"
        rx="1"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
    </svg>
  );
}

function CiIcon({ className }: IconProps) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden>
      <circle
        cx="12"
        cy="12"
        r="8"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path
        d="M12 4 A8 8 0 0 1 20 12"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
      <circle cx="12" cy="12" r="1.5" fill="currentColor" />
    </svg>
  );
}

function EcosystemIcon({ className }: IconProps) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden>
      <circle cx="12" cy="12" r="2.5" fill="currentColor" />
      <circle
        cx="5"
        cy="6"
        r="2"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <circle
        cx="19"
        cy="6"
        r="2"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <circle
        cx="5"
        cy="18"
        r="2"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <circle
        cx="19"
        cy="18"
        r="2"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <g
        stroke="currentColor"
        strokeWidth="1.2"
        strokeLinecap="round"
        opacity="0.55"
      >
        <path d="M6.5 7.5 L10 10.5" />
        <path d="M17.5 7.5 L14 10.5" />
        <path d="M6.5 16.5 L10 13.5" />
        <path d="M17.5 16.5 L14 13.5" />
      </g>
    </svg>
  );
}

/* -------------------------------------------------------------------------- */
/*  Chrome primitives.                                                        */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero. Confident headline, dual CTA, eight-surface counter strip.          */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <header className="flex flex-col items-center gap-7 pt-6 text-center">
      <Eyebrow>The ChilliCream Platform</Eyebrow>
      <h1 className="font-heading text-hero text-cc-heading max-w-4xl font-semibold tracking-tight">
        Every GraphQL API,{" "}
        <span
          className="bg-clip-text text-transparent"
          style={{ backgroundImage: SPECTRUM }}
        >
          one product
        </span>
        .
      </h1>
      <p className="text-cc-ink lead max-w-2xl">
        Build, run, and evolve your GraphQL surface from a single platform.
        Eight capabilities wired together by Nitro, the control plane that ties
        the schema, the traces, and the releases into one timeline.
      </p>
      <div className="flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="https://nitro.chillicream.com">
          Open Nitro
        </SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </header>
  );
}

/* -------------------------------------------------------------------------- */
/*  Product reel. The platform is the product, so we show it immediately.     */
/* -------------------------------------------------------------------------- */

function ProductReel() {
  return (
    <section className="flex flex-col gap-5">
      <div className="flex flex-col items-center gap-2 text-center">
        <Eyebrow>The platform in motion</Eyebrow>
        <h2 className="font-heading text-h3 text-cc-heading max-w-2xl font-semibold tracking-tight">
          Author, observe, diagnose, schema, fusion.
        </h2>
        <p className="text-cc-ink-dim max-w-2xl text-[0.95rem] leading-relaxed">
          Five surfaces of Nitro running on a real schema. Same data model, same
          UI, the eight platform surfaces share the same data and schema beneath
          the UI.
        </p>
      </div>
      <div className="border-cc-card-border bg-cc-surface mx-auto w-full max-w-5xl overflow-hidden rounded-xl border">
        <NitroReel />
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Capability link grid. Compact 4x2 of every platform surface.              */
/* -------------------------------------------------------------------------- */

interface SurfaceCardProps {
  readonly surface: Surface;
}

function SurfaceCard({ surface }: SurfaceCardProps) {
  const Icon = surface.icon;
  return (
    <Link
      href={surface.href}
      className="group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col gap-3 rounded-xl border p-5 no-underline backdrop-blur-sm transition-colors"
    >
      <div className="flex items-center justify-between">
        <span className="text-cc-accent flex h-9 w-9 items-center justify-center">
          <Icon className="h-6 w-6" />
        </span>
        <span className="text-cc-ink-dim font-mono text-[0.62rem] tracking-tight opacity-70">
          {surface.href}
        </span>
      </div>
      <h3 className="font-heading text-h6 text-cc-heading group-hover:text-cc-accent font-semibold tracking-tight transition-colors">
        {surface.title}
      </h3>
      <p className="text-cc-ink-dim text-[0.85rem] leading-snug">
        {surface.tagline}
      </p>
      <span className="text-cc-accent mt-auto pt-2 text-[0.78rem] font-medium">
        Open {surface.title} →
      </span>
    </Link>
  );
}

function CapabilityGrid() {
  return (
    <section className="flex flex-col gap-7">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="flex flex-col gap-2">
          <Eyebrow>Eight surfaces</Eyebrow>
          <h2 className="font-heading text-h2 text-cc-heading font-semibold tracking-tight">
            Pick the surface closest to your work.
          </h2>
        </div>
        <p className="text-cc-ink-dim max-w-md text-[0.92rem] leading-relaxed">
          Every tile is a real page. Open the one that matches today&apos;s
          problem, or wander the map and see how the pieces lock together.
        </p>
      </div>
      <div className="grid auto-rows-fr gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {SURFACES.map((surface) => (
          <SurfaceCard key={surface.href} surface={surface} />
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Run anywhere band. Nitro callout with hosted and self-host pointers.      */
/* -------------------------------------------------------------------------- */

const RUN_ANYWHERE: readonly string[] = [
  "Hosted at nitro.chillicream.com, ready to connect",
  "Schema registry, release checks, and analytics in one home",
  "OpenTelemetry traces flow through to your stack",
];

function RunAnywhere() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border p-8 md:p-10">
      <span
        className="pointer-events-none absolute -top-24 -right-24 h-64 w-64 rounded-full blur-3xl"
        style={{
          background:
            "radial-gradient(closest-side, rgba(94,234,212,0.35), transparent)",
        }}
        aria-hidden
      />
      <div className="relative flex flex-col gap-6 md:flex-row md:items-center md:justify-between md:gap-10">
        <div className="flex flex-col gap-3 md:max-w-xl">
          <Eyebrow>Run anywhere with Nitro</Eyebrow>
          <h2 className="font-heading text-h3 text-cc-heading font-semibold tracking-tight">
            The control plane that powers the platform.
          </h2>
          <p className="text-cc-ink leading-relaxed">
            Nitro is where the eight surfaces meet. Connect a service, push a
            schema, and the registry, the checks, the traces, and the analytics
            line up under one product. Use the hosted version or run it next to
            your services.
          </p>
          <ul className="text-cc-ink-dim mt-2 flex flex-col gap-1.5">
            {RUN_ANYWHERE.map((line) => (
              <li
                key={line}
                className="flex items-start gap-2 text-[0.88rem] leading-snug"
              >
                <span className="text-cc-accent mt-1 flex h-3 w-3 shrink-0 items-center justify-center">
                  <CheckIcon size={12} />
                </span>
                <span>{line}</span>
              </li>
            ))}
          </ul>
        </div>
        <div className="flex flex-col gap-3 md:items-end">
          <SolidButton href="https://nitro.chillicream.com">
            Open Nitro
          </SolidButton>
          <OutlineButton href="/products/nitro">About Nitro</OutlineButton>
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing CTA.                                                              */
/* -------------------------------------------------------------------------- */

function ClosingCta() {
  return (
    <section className="flex flex-col items-center gap-6 py-6 text-center">
      <Eyebrow>One platform, every API</Eyebrow>
      <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
        Start a project, fold in the rest as you go.
      </h2>
      <p className="text-cc-ink-dim max-w-2xl text-[0.95rem] leading-relaxed">
        Spin up a server with Hot Chocolate, point Nitro at it, and the rest of
        the platform meets you where you are. No big-bang migration, no separate
        tools to wire up.
      </p>
      <div className="flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export default function PlatformProductHubPage() {
  return (
    <div className="flex flex-col gap-20 py-6">
      <Hero />
      <ProductReel />
      <CapabilityGrid />
      <RunAnywhere />
      <ClosingCta />
    </div>
  );
}
