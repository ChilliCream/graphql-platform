"use client";

import NextLink from "next/link";
import { motion, useReducedMotion } from "motion/react";
import type { ComponentType, CSSProperties, ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { LogoCloud } from "@/src/components/home/LogoCloud";
import { CookieCrumble } from "@/src/icons/CookieCrumble";
import { GreenDonut } from "@/src/icons/GreenDonut";
import { HotChocolate } from "@/src/icons/HotChocolate";
import { Mocha } from "@/src/icons/Mocha";
import { Nitro } from "@/src/icons/Nitro";
import { StrawberryShake } from "@/src/icons/StrawberryShake";

/* -------------------------------------------------------------------------- */
/*  Data (facts reused verbatim from about/preview/v1)                         */
/* -------------------------------------------------------------------------- */

interface DeckCard {
  readonly name: string;
  readonly tagline: string;
  readonly description: string;
  readonly href: string;
  readonly linkLabel: string;
  readonly external?: boolean;
  readonly icon: ComponentType<{
    readonly className?: string;
    readonly style?: CSSProperties;
  }>;
  /** Horizontal offset of the dealt card on the lg stage, in pixels. */
  readonly x: number;
  /** Vertical offset of the dealt card on the lg stage, in pixels. */
  readonly y: number;
  /** Resting rotation of the dealt card, in degrees. */
  readonly rotate: number;
}

/**
 * The six products fanned across the stage. Offsets follow a 24px vertical
 * rhythm and a rotation increment from -3deg to +5deg, so the cards read as a
 * single stack resolving into six artifacts. Hot Chocolate is the front card.
 */
const DECK: readonly DeckCard[] = [
  {
    name: "Hot Chocolate",
    tagline: "GraphQL server for .NET",
    description:
      "The schema-first and code-first GraphQL server at the heart of the platform. Built on ASP.NET Core, with first-class support for queries, mutations, subscriptions, and the modern GraphQL spec.",
    href: "/products/hotchocolate",
    linkLabel: "Hot Chocolate",
    icon: HotChocolate,
    x: 0,
    y: 0,
    rotate: -3,
  },
  {
    name: "Strawberry Shake",
    tagline: "Typed .NET client",
    description:
      "A typed GraphQL client for .NET with MSBuild codegen. Write a query, get a fully typed C# API, and ship apps that talk to any GraphQL endpoint without hand-written DTOs.",
    href: "/products/strawberryshake",
    linkLabel: "Strawberry Shake",
    icon: StrawberryShake,
    x: 152,
    y: 24,
    rotate: -1,
  },
  {
    name: "Nitro",
    tagline: "Control plane and IDE",
    description:
      "The control plane for your GraphQL APIs: schema and client registry, CI checks, observability, and the GraphQL IDE your team already uses to explore the graph.",
    href: "https://nitro.chillicream.com",
    linkLabel: "Nitro",
    external: true,
    icon: Nitro,
    x: 304,
    y: 48,
    rotate: 1,
  },
  {
    name: "Mocha",
    tagline: "Mediator and messaging",
    description:
      "A source-generated mediator and cross-service messaging library. No reflection on the hot path, predictable performance, and the same programming model in-process and across services.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Mocha",
    external: true,
    icon: Mocha,
    x: 456,
    y: 72,
    rotate: 3,
  },
  {
    name: "Green Donut",
    tagline: "DataLoader for .NET",
    description:
      "The DataLoader implementation behind Hot Chocolate. Batches and caches your data access so resolvers stay simple and you avoid the N+1 problem by default.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Green Donut",
    external: true,
    icon: GreenDonut,
    x: 608,
    y: 96,
    rotate: 4,
  },
  {
    name: "Cookie Crumble",
    tagline: "GraphQL-aware snapshot testing",
    description:
      "Snapshot testing built for GraphQL. Native support for execution results and HTTP responses, with Markdown snapshots for tests that capture multiple shapes of state.",
    href: "https://github.com/ChilliCream/graphql-platform",
    linkLabel: "Cookie Crumble",
    external: true,
    icon: CookieCrumble,
    x: 760,
    y: 120,
    rotate: 5,
  },
];

interface Principle {
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
}

const PRINCIPLES: readonly Principle[] = [
  {
    eyebrow: "01",
    title: "Open source first",
    body: "The entire platform lives on GitHub. You can read the code, file an issue, send a pull request, and ship the same bits we ship. Public roadmaps, public releases, public discussions.",
  },
  {
    eyebrow: "02",
    title: "Honest about what we ship",
    body: "We describe what the platform does, not what it might do one day. If a capability is in preview, we say so. If a tradeoff exists, we name it. No marketing language standing in for engineering.",
  },
  {
    eyebrow: "03",
    title: ".NET-native, end to end",
    body: "Server, client, mediator, DataLoader, and tests are designed together for .NET. OpenTelemetry-native observability, performance treated as a feature, and a developer experience that respects the language.",
  },
];

interface CommunityLink {
  readonly label: string;
  readonly href: string;
  readonly description: string;
}

const COMMUNITY: readonly CommunityLink[] = [
  {
    label: "Slack",
    href: "https://slack.chillicream.com/",
    description:
      "Ask questions, trade patterns, talk to the people who build the platform.",
  },
  {
    label: "GitHub",
    href: "https://github.com/ChilliCream/graphql-platform",
    description:
      "Read the code, file issues, send pull requests. The platform is built in the open.",
  },
  {
    label: "YouTube",
    href: "https://www.youtube.com/c/ChilliCream",
    description:
      "Talks, deep dives, and walkthroughs from the team and the wider community.",
  },
  {
    label: "Blog",
    href: "/blog",
    description:
      "Release notes, design decisions, and engineering writeups straight from the team.",
  },
  {
    label: "Nitro",
    href: "https://nitro.chillicream.com",
    description:
      "Sign in to the control plane: schema registry, CI checks, observability, IDE.",
  },
  {
    label: "X",
    href: "https://x.com/Chilli_Cream",
    description:
      "Short updates, releases, and conversation with the broader GraphQL community.",
  },
  {
    label: "LinkedIn",
    href: "https://www.linkedin.com/company/chillicream",
    description:
      "Follow the company for product news, talks, and hiring updates.",
  },
];

/* -------------------------------------------------------------------------- */
/*  Page                                                                       */
/* -------------------------------------------------------------------------- */

export function ClientPage() {
  return (
    <div className="mx-auto max-w-6xl">
      <Hero />
      <LogoCloud />
      <DeckSection />
      <HouseRules />
      <Community />
      <ClosingCard />
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero: 5/7 split with a breathing top-card preview                          */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <section className="grid items-center gap-12 py-16 sm:py-24 lg:grid-cols-12">
      <div className="lg:col-span-5">
        <p className="text-cc-nav-label mb-4 font-mono text-xs tracking-[0.2em] uppercase">
          About ChilliCream
        </p>
        <h1 className="font-heading text-cc-heading text-hero tracking-tight">
          Six products, dealt as{" "}
          <span className="text-cc-accent">one hand</span>.
        </h1>
        <p className="lead text-cc-ink mt-8 max-w-xl">
          We build an end-to-end GraphQL platform: the server, the typed client,
          the gateway, the control plane, and the tools to test and ship it. One
          platform, dealt as six open-source projects, all designed together and
          all in the open on GitHub.
        </p>
        <div className="mt-10 flex flex-wrap items-center gap-3">
          <SolidButton href="https://github.com/ChilliCream/graphql-platform">
            Explore the platform on GitHub
          </SolidButton>
          <OutlineButton href="/services">Work with us</OutlineButton>
        </div>
      </div>

      <div className="lg:col-span-7">
        <HeroDeckPreview />
      </div>
    </section>
  );
}

/**
 * The decorative "top card" preview. The Hot Chocolate card sits larger with a
 * faint stack of card-shaped silhouettes peeking out beneath it, hinting at the
 * deck below. The front card has a gentle infinite breath sway; all other
 * motion on the page is hover- or enter-once-driven.
 */
function HeroDeckPreview() {
  const reduced = useReducedMotion();

  return (
    <div className="relative mx-auto flex h-[360px] w-full max-w-md items-center justify-center sm:h-[420px]">
      {/* Two silhouette cards peeking from under the top card. */}
      <div
        aria-hidden="true"
        className="border-cc-card-border bg-cc-surface/40 absolute h-[260px] w-[200px] rounded-2xl border sm:h-[300px] sm:w-[230px]"
        style={{ transform: "translate(46px, 40px) rotate(6deg)" }}
      />
      <div
        aria-hidden="true"
        className="border-cc-card-border bg-cc-surface/60 absolute h-[270px] w-[208px] rounded-2xl border sm:h-[310px] sm:w-[238px]"
        style={{ transform: "translate(24px, 20px) rotate(3deg)" }}
      />

      {/* The breathing top card. */}
      <motion.div
        className="border-cc-card-border bg-cc-card-bg relative flex h-[280px] w-[216px] flex-col rounded-2xl border p-6 sm:h-[320px] sm:w-[248px]"
        animate={reduced ? undefined : { rotate: [-1, 1, -1] }}
        transition={
          reduced
            ? undefined
            : { duration: 6, ease: "easeInOut", repeat: Infinity }
        }
      >
        <div className="bg-cc-surface/60 border-cc-card-border flex h-16 w-16 items-center justify-center rounded-xl border">
          <HotChocolate className="h-12 w-12 object-contain" />
        </div>
        <h2 className="font-heading text-cc-heading text-h5 mt-5 tracking-tight">
          Hot Chocolate
        </h2>
        <p className="text-cc-nav-label mt-1 font-mono text-[11px] tracking-[0.18em] uppercase">
          GraphQL server for .NET
        </p>
        <p className="text-cc-ink mt-auto text-sm leading-relaxed">
          The card at the top of the deck. Everything else is dealt from here.
        </p>
      </motion.div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  The Deck: the centerpiece fanned stage                                     */
/* -------------------------------------------------------------------------- */

function DeckSection() {
  return (
    <section className="py-16 sm:py-20">
      <header className="grid items-end gap-6 lg:grid-cols-12">
        <div className="lg:col-span-7">
          <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.2em] uppercase">
            The platform we build
          </p>
          <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
            Six products. One platform.
          </h2>
        </div>
        <p className="text-cc-ink lg:col-span-5">
          Every piece is a real, shipping open-source project in the
          ChilliCream/graphql-platform repository. Pick one and adopt it on its
          own, or compose the full stack. Hover a card to lift it from the deck.
        </p>
      </header>

      <DeckStage />

      <div className="border-cc-card-border mt-12 border-t pt-6">
        <p className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
          Deck legend
        </p>
        <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed">
          Each card is one open-source project you can adopt on its own. Fusion,
          our distributed gateway, is built on Hot Chocolate and lives in the
          same repository; the gateway is always self-run, with composition done
          at planning time.
        </p>
      </div>
    </section>
  );
}

/**
 * The stage. On lg and up, the six cards are absolutely positioned and fanned
 * with translate/rotate values; hover lifts and straightens the focused card
 * while siblings dim and shrink (CSS group selectors only). On mobile the deck
 * collapses to a plain vertical stack with no rotation.
 */
function DeckStage() {
  return (
    <>
      {/* Mobile: vertical stack, no rotation, no stage chrome. */}
      <div className="mt-10 grid gap-5 lg:hidden">
        {DECK.map((card) => (
          <DeckCardFace key={card.name} card={card} />
        ))}
      </div>

      {/* Desktop: the felt-table stage with fanned, absolutely-placed cards. */}
      <motion.div
        className="group relative mt-12 hidden h-[640px] lg:block"
        initial={{ opacity: 0, y: 16 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, margin: "-15% 0px" }}
        transition={{ duration: 0.5, ease: "easeOut" }}
      >
        {/* Felt-table radial glow behind the cards. */}
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-0"
          style={{
            background:
              "radial-gradient(60% 55% at 42% 45%, rgba(94, 234, 212, 0.04), transparent 70%)",
          }}
        />
        {/* Table-edge hairline arc. */}
        <svg
          aria-hidden="true"
          className="pointer-events-none absolute inset-0 h-full w-full"
          viewBox="0 0 1000 640"
          preserveAspectRatio="none"
        >
          <ellipse
            cx="430"
            cy="360"
            rx="520"
            ry="300"
            fill="none"
            stroke="rgba(245, 241, 234, 0.12)"
            strokeWidth="1"
          />
        </svg>

        {DECK.map((card) => (
          <FannedCard key={card.name} card={card} />
        ))}
      </motion.div>
    </>
  );
}

interface FannedCardProps {
  readonly card: DeckCard;
}

/**
 * A single absolutely-positioned card on the desktop stage. The resting
 * transform fans the card out; on hover it lifts (translateY -16px) and
 * straightens to 0deg with a brighter border, while every sibling dims and
 * scales down via the parent `group-hover` selectors. Pure CSS, no JS state.
 */
function FannedCard({ card }: FannedCardProps) {
  const Icon = card.icon;

  const rest = `translate(${card.x}px, ${card.y}px) rotate(${card.rotate}deg)`;
  const lifted = `translate(${card.x}px, ${card.y - 16}px) rotate(0deg)`;

  return (
    <article
      className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover absolute top-0 left-0 flex h-[420px] w-[300px] flex-col rounded-2xl border p-7 opacity-100 transition-[transform,opacity,border-color] duration-300 ease-out group-hover:opacity-60 hover:z-50 hover:!opacity-100"
      style={{ transform: rest }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = lifted;
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = rest;
      }}
    >
      <DeckCardBody card={card} Icon={Icon} />
    </article>
  );
}

interface DeckCardFaceProps {
  readonly card: DeckCard;
}

/** Mobile card shell: same rounded-2xl border shell, no transform. */
function DeckCardFace({ card }: DeckCardFaceProps) {
  const Icon = card.icon;
  return (
    <article className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex flex-col rounded-2xl border p-6 transition-colors">
      <DeckCardBody card={card} Icon={Icon} />
    </article>
  );
}

interface DeckCardBodyProps {
  readonly card: DeckCard;
  readonly Icon: DeckCard["icon"];
}

function DeckCardBody({ card, Icon }: DeckCardBodyProps) {
  const arrow = <span aria-hidden="true">&rarr;</span>;
  const linkClasses =
    "text-cc-accent mt-5 inline-flex items-center gap-1.5 text-sm font-medium hover:underline";

  return (
    <>
      <div className="flex items-start gap-4">
        <div className="bg-cc-surface/60 border-cc-card-border flex h-16 w-16 shrink-0 items-center justify-center rounded-xl border">
          <Icon className="h-12 w-12 object-contain" />
        </div>
        <div className="min-w-0">
          <h3 className="font-heading text-cc-heading text-h5 leading-tight tracking-tight">
            {card.name}
          </h3>
          <p className="text-cc-nav-label mt-1 font-mono text-[11px] tracking-[0.18em] uppercase">
            {card.tagline}
          </p>
        </div>
      </div>

      <p className="text-cc-ink mt-5 text-sm leading-relaxed">
        {card.description}
      </p>

      {card.external ? (
        <a
          href={card.href}
          target="_blank"
          rel="noopener noreferrer"
          className={linkClasses}
        >
          Visit {card.linkLabel} {arrow}
        </a>
      ) : (
        <NextLink href={card.href} className={linkClasses}>
          Explore {card.linkLabel} {arrow}
        </NextLink>
      )}
    </>
  );
}

/* -------------------------------------------------------------------------- */
/*  House Rules: 2-col split with a tilted mini-deck of three                  */
/* -------------------------------------------------------------------------- */

function HouseRules() {
  return (
    <section className="grid items-center gap-12 py-16 sm:py-20 lg:grid-cols-2">
      <div>
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.2em] uppercase">
          House rules
        </p>
        <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
          How we play every release.
        </h2>
        <p className="text-cc-ink mt-5 max-w-md text-base sm:text-lg">
          Three rules sit under every card in the deck. They hold whether you
          adopt one product or compose the full stack.
        </p>
      </div>

      <div className="relative mx-auto h-[420px] w-full max-w-sm">
        {PRINCIPLES.map((p, i) => (
          <article
            key={p.title}
            className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover absolute right-0 left-0 rounded-2xl border p-7 transition-[transform,border-color] duration-300 ease-out hover:z-50"
            style={{
              top: `${i * 84}px`,
              transform: `rotate(${i * 2 - 2}deg)`,
              zIndex: i,
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.transform = "rotate(0deg)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.transform = `rotate(${i * 2 - 2}deg)`;
            }}
          >
            <div className="text-cc-accent font-mono text-xs tracking-[0.2em]">
              {p.eyebrow}
            </div>
            <h3 className="font-heading text-cc-heading text-h5 mt-4 tracking-tight">
              {p.title}
            </h3>
            <p className="text-cc-ink mt-3 text-sm leading-relaxed">{p.body}</p>
          </article>
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Community: a pinned strip of seven label cards                             */
/* -------------------------------------------------------------------------- */

function Community() {
  return (
    <section className="py-16 sm:py-20">
      <header className="grid items-end gap-6 lg:grid-cols-12">
        <div className="lg:col-span-7">
          <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.2em] uppercase">
            Where the work happens
          </p>
          <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
            The platform is built in public.
          </h2>
        </div>
        <p className="text-cc-ink lg:col-span-5">
          Follow along, ask questions, and contribute. These are the real
          channels where the work happens.
        </p>
      </header>

      <div className="-mx-5 mt-12 flex snap-x gap-4 overflow-x-auto px-5 pb-2 sm:mx-0 sm:grid sm:grid-cols-2 sm:overflow-visible sm:px-0 lg:grid-cols-7 lg:gap-3">
        {COMMUNITY.map((item) => (
          <CommunityLabelCard key={item.label} {...item} />
        ))}
      </div>
    </section>
  );
}

function CommunityLabelCard({ label, href, description }: CommunityLink) {
  const inner: ReactNode = (
    <>
      <div className="flex items-center justify-between">
        <span className="font-heading text-cc-heading text-h6 tracking-tight">
          {label}
        </span>
        <span
          aria-hidden="true"
          className="text-cc-ink-dim group-hover:text-cc-accent text-base transition-colors"
        >
          &rarr;
        </span>
      </div>
      <p className="text-cc-ink mt-2 text-sm leading-relaxed">{description}</p>
    </>
  );

  const classes =
    "bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover group block h-full w-64 shrink-0 snap-start rounded-2xl border p-5 transition-[transform,border-color] duration-300 ease-out hover:-translate-y-1 sm:w-auto";

  if (href.startsWith("/")) {
    return (
      <NextLink href={href} className={classes}>
        {inner}
      </NextLink>
    );
  }

  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className={classes}
    >
      {inner}
    </a>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing card: one tilted card that straightens on hover                    */
/* -------------------------------------------------------------------------- */

function ClosingCard() {
  return (
    <section className="py-16 sm:py-20">
      <article
        className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover mx-auto max-w-3xl rounded-3xl border p-10 text-center transition-[transform,border-color] duration-300 ease-out sm:p-14"
        style={{ transform: "rotate(-2deg)" }}
        onMouseEnter={(e) => {
          e.currentTarget.style.transform = "rotate(0deg)";
        }}
        onMouseLeave={(e) => {
          e.currentTarget.style.transform = "rotate(-2deg)";
        }}
      >
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.2em] uppercase">
          Work with ChilliCream
        </p>
        <h2 className="font-heading text-cc-heading text-h2 mx-auto max-w-2xl tracking-tight">
          Need GraphQL experts on your side?
        </h2>
        <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base sm:text-lg">
          From hourly advisory and architecture reviews to enterprise support
          with SLAs, we engage at the level your team needs.
        </p>
        <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/services/support/contact">Contact us</SolidButton>
          <OutlineButton href="/services">Browse services</OutlineButton>
          <OutlineButton href="/pricing">See pricing</OutlineButton>
        </div>
      </article>
    </section>
  );
}
