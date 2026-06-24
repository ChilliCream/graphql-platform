import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "ChilliCream Company Hub | Brand, Contact, Legal",
  description:
    "The ChilliCream company hub: get in touch, follow our channels, read the brand guide, browse legal terms and the license, and grab merch from the shop.",
  keywords: [
    "ChilliCream",
    "Hot Chocolate",
    "Nitro",
    "ChilliCream company",
    "ChilliCream brand",
    "ChilliCream contact",
    "ChilliCream legal",
    "ChilliCream license",
    "ChilliCream community",
    "ChilliCream merch",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "ChilliCream Company Hub",
    description:
      "Get in touch, follow our channels, read the brand guide, browse legal terms and the license, and grab merch from the ChilliCream shop.",
  },
};

/* ------------------------------------------------------------------ *
 * Inline primitives                                                   *
 * ------------------------------------------------------------------ */

interface EyebrowProps {
  readonly children: ReactNode;
  readonly className?: string;
}

function Eyebrow({ children, className }: EyebrowProps) {
  return (
    <span
      className={`text-cc-nav-label font-mono text-[0.7rem] tracking-[0.28em] uppercase ${className ?? ""}`}
    >
      {children}
    </span>
  );
}

interface BandProps {
  readonly id: string;
  readonly ordinal: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly intro?: ReactNode;
  readonly children: ReactNode;
}

function Band({ id, ordinal, eyebrow, title, intro, children }: BandProps) {
  return (
    <section id={id} className="scroll-mt-28 py-16 sm:py-20">
      <div className="grid gap-10 lg:grid-cols-[10rem_1fr] lg:gap-12">
        <div className="flex flex-col gap-3">
          <div className="text-cc-ink-faint font-heading text-6xl leading-none font-semibold tabular-nums">
            {ordinal}
          </div>
          <Eyebrow>{eyebrow}</Eyebrow>
        </div>
        <div className="flex flex-col gap-6">
          <h2 className="text-cc-heading text-h4 font-heading sm:text-h3 font-semibold tracking-tight">
            {title}
          </h2>
          {intro ? (
            <p className="text-cc-prose max-w-2xl text-base sm:text-lg">
              {intro}
            </p>
          ) : null}
          <div>{children}</div>
        </div>
      </div>
    </section>
  );
}

interface BandDividerProps {
  readonly accent?: boolean;
}

function BandDivider({ accent }: BandDividerProps) {
  return (
    <div className="relative h-px w-full" aria-hidden>
      <div className="bg-cc-card-border absolute inset-0" />
      {accent ? (
        <div className="bg-cc-accent absolute top-0 left-0 h-px w-24" />
      ) : null}
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Inline SVG art                                                      *
 * ------------------------------------------------------------------ */

/** Wordmark composed entirely in cc-* tokens. Decorative; the text label
 *  below it carries the meaning for assistive tech. */
function WordmarkArt() {
  return (
    <svg
      viewBox="0 0 360 90"
      width="100%"
      height="auto"
      role="img"
      aria-label="ChilliCream wordmark"
      className="block"
    >
      <defs>
        <linearGradient id="cc-resv2-spectrum" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor="#16b9e4" />
          <stop offset="50%" stopColor="#7c92c6" />
          <stop offset="100%" stopColor="#f0786a" />
        </linearGradient>
      </defs>
      <text
        x="0"
        y="62"
        fontFamily="var(--font-heading)"
        fontWeight={700}
        fontSize={56}
        letterSpacing="-0.02em"
        fill="url(#cc-resv2-spectrum)"
      >
        ChilliCream
      </text>
      <rect
        x="0"
        y="76"
        width="240"
        height="2"
        fill="var(--color-cc-card-border)"
      />
    </svg>
  );
}

/* ------------------------------------------------------------------ *
 * Channel + link data                                                 *
 * ------------------------------------------------------------------ */

interface ChannelLink {
  readonly href: string;
  readonly title: string;
  readonly handle: string;
  readonly description: string;
  readonly external: boolean;
}

const CONTACT_LINKS: readonly ChannelLink[] = [
  {
    href: "mailto:contact@chillicream.com",
    title: "Email",
    handle: "contact@chillicream.com",
    description: "Sales, partnerships, press, and anything else.",
    external: false,
  },
  {
    href: "https://slack.chillicream.com/",
    title: "Slack",
    handle: "slack.chillicream.com",
    description: "Community workspace for users and maintainers.",
    external: true,
  },
  {
    href: "https://github.com/ChilliCream/graphql-platform",
    title: "GitHub",
    handle: "ChilliCream/graphql-platform",
    description: "Source, issues, discussions, and releases.",
    external: true,
  },
];

const COMMUNITY_LINKS: readonly ChannelLink[] = [
  {
    href: "/blog",
    title: "Blog",
    handle: "/blog",
    description: "Release notes, design deep dives, and field reports.",
    external: false,
  },
  {
    href: "/docs",
    title: "Docs",
    handle: "/docs",
    description: "Guides and reference for Hot Chocolate and Nitro.",
    external: false,
  },
  {
    href: "https://www.youtube.com/c/ChilliCream",
    title: "YouTube",
    handle: "youtube.com/c/ChilliCream",
    description: "Talks, tutorials, and product walkthroughs.",
    external: true,
  },
  {
    href: "https://x.com/Chilli_Cream",
    title: "X",
    handle: "@Chilli_Cream",
    description: "Release announcements and short updates.",
    external: true,
  },
  {
    href: "https://www.linkedin.com/company/chillicream",
    title: "LinkedIn",
    handle: "linkedin.com/company/chillicream",
    description: "Company news and hiring updates.",
    external: true,
  },
  {
    href: "https://nitro.chillicream.com",
    title: "Nitro",
    handle: "nitro.chillicream.com",
    description: "The Nitro product site.",
    external: true,
  },
];

interface LegalLink {
  readonly href: string;
  readonly title: string;
  readonly summary: string;
}

const LEGAL_LINKS: readonly LegalLink[] = [
  {
    href: "/legal/acceptable-use-policy",
    title: "Acceptable Use Policy",
    summary: "The rules that govern use of ChilliCream services.",
  },
  {
    href: "/legal/cookie-policy",
    title: "Cookie Policy",
    summary: "What we set in your browser and why.",
  },
  {
    href: "/legal/privacy-policy",
    title: "Privacy Policy",
    summary: "How we collect, store, and handle your data.",
  },
  {
    href: "/legal/terms-of-service",
    title: "Terms of Service",
    summary: "The agreement between you and ChilliCream.",
  },
  {
    href: "/licensing/chillicream-license",
    title: "ChilliCream License",
    summary: "Commercial license terms for paid plans.",
  },
];

/* ------------------------------------------------------------------ *
 * Section components                                                  *
 * ------------------------------------------------------------------ */

function HeroBlock() {
  return (
    <section className="relative pt-12 pb-20 sm:pt-16 sm:pb-24">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          backgroundImage:
            "radial-gradient(ellipse 60% 70% at 30% 0%, rgba(94,234,212,0.10), transparent 65%)",
        }}
      />
      <div className="flex flex-col gap-10">
        <div className="flex flex-col gap-6">
          <Eyebrow>Company / Hub</Eyebrow>
          <h1 className="text-cc-heading font-heading text-h3 sm:text-h2 leading-tight font-semibold tracking-tight">
            Everything about{" "}
            <span
              style={{
                backgroundImage:
                  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
                WebkitBackgroundClip: "text",
                backgroundClip: "text",
                color: "transparent",
              }}
            >
              ChilliCream
            </span>
            <br />
            that is not a product page.
          </h1>
          <p className="text-cc-prose max-w-2xl text-base sm:text-lg">
            Get in touch, follow the channels we actually post on, read how we
            use the brand, and find the legal terms and license. No press kit
            theatre, no invented downloads. Just the real links.
          </p>
          <div className="flex flex-wrap items-center gap-3">
            <SolidButton href="#contact">Get in touch</SolidButton>
            <OutlineButton href="#legal">Read the legal terms</OutlineButton>
          </div>
        </div>

        <nav
          aria-label="On this page"
          className="border-cc-card-border bg-cc-card-bg flex flex-wrap gap-x-6 gap-y-2 rounded-2xl border px-5 py-4 backdrop-blur-sm"
        >
          <Eyebrow className="basis-full sm:basis-auto">On this page</Eyebrow>
          {[
            { href: "#contact", label: "01 Contact" },
            { href: "#community", label: "02 Community" },
            { href: "#brand", label: "03 Brand" },
            { href: "#legal", label: "04 Legal" },
            { href: "#merch", label: "05 Merch" },
          ].map((item) => (
            <a
              key={item.href}
              href={item.href}
              className="text-cc-ink hover:text-cc-accent text-sm no-underline transition-colors"
            >
              {item.label}
            </a>
          ))}
        </nav>
      </div>
    </section>
  );
}

interface ChannelCardProps {
  readonly entry: ChannelLink;
}

function ChannelCard({ entry }: ChannelCardProps) {
  const external = entry.external
    ? { target: "_blank", rel: "noopener noreferrer" }
    : {};
  return (
    <Link
      href={entry.href}
      {...external}
      className="group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col gap-2 rounded-xl border p-5 no-underline transition-colors"
    >
      <div className="flex items-center justify-between gap-3">
        <span className="text-cc-heading group-hover:text-cc-accent text-base font-semibold transition-colors">
          {entry.title}
        </span>
        <span
          aria-hidden
          className="text-cc-ink-dim group-hover:text-cc-accent font-mono text-xs transition-colors"
        >
          {entry.external ? "->" : ">"}
        </span>
      </div>
      <span className="text-cc-ink-dim font-mono text-xs break-all">
        {entry.handle}
      </span>
      <span className="text-cc-prose text-sm">{entry.description}</span>
    </Link>
  );
}

function ContactBand() {
  return (
    <Band
      id="contact"
      ordinal="01"
      eyebrow="Get in touch"
      title="The direct lines."
      intro={
        <>
          For sales, partnerships, press, security disclosures, or anything
          else, write to us at the address below. For product questions and
          community discussion, Slack and GitHub are the fastest paths.
        </>
      }
    >
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {CONTACT_LINKS.map((entry) => (
          <ChannelCard key={entry.href} entry={entry} />
        ))}
      </div>
    </Band>
  );
}

function CommunityBand() {
  return (
    <Band
      id="community"
      ordinal="02"
      eyebrow="Channels"
      title="Where we actually post."
      intro={
        <>
          We use a handful of channels. These are the ones where new content,
          releases, and announcements actually appear. If a channel is not on
          this list, treat it as unofficial.
        </>
      }
    >
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {COMMUNITY_LINKS.map((entry) => (
          <ChannelCard key={entry.href} entry={entry} />
        ))}
      </div>
    </Band>
  );
}

function BrandBand() {
  return (
    <Band
      id="brand"
      ordinal="03"
      eyebrow="Brand"
      title="How to refer to us."
      intro={
        <>
          A short, honest brand guide. We do not currently publish downloadable
          logos, press kits, or font files, so if you need an asset for an
          article, talk, or sponsorship, write to us and we will send what we
          have.
        </>
      }
    >
      <div className="grid gap-6 lg:grid-cols-[1.05fr_1fr]">
        <div className="border-cc-card-border bg-cc-card-bg flex flex-col gap-5 rounded-2xl border p-6 sm:p-8">
          <Eyebrow>Wordmark</Eyebrow>
          <div className="border-cc-card-border bg-cc-surface/60 rounded-xl border px-6 py-8">
            <WordmarkArt />
          </div>
          <dl className="grid gap-4 sm:grid-cols-2">
            <div>
              <dt className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.24em] uppercase">
                Spelling
              </dt>
              <dd className="text-cc-heading font-heading mt-1 text-xl font-semibold">
                ChilliCream
              </dd>
              <p className="text-cc-ink-dim mt-1 text-sm">
                One word. Capital C, double l, capital C. Not Chillicream, not
                Chilli Cream, not Chili Cream.
              </p>
            </div>
            <div>
              <dt className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.24em] uppercase">
                Type voice
              </dt>
              <dd className="text-cc-heading font-heading mt-1 text-xl font-semibold">
                Josefin Sans
              </dd>
              <p className="text-cc-ink-dim mt-1 text-sm">
                Heading voice across the site. Body copy uses the native system
                stack.
              </p>
            </div>
          </dl>
        </div>

        <div className="border-cc-card-border bg-cc-card-bg flex flex-col gap-5 rounded-2xl border p-6 sm:p-8">
          <Eyebrow>Wordmark colors</Eyebrow>
          <p className="text-cc-prose text-sm">
            The wordmark uses a three stop spectrum, cyan to violet to coral.
            Used at most once per screen so it stays a signature, not a pattern.
          </p>
          <ul className="flex flex-col gap-3">
            {[
              { name: "Cyan", hex: "#16b9e4" },
              { name: "Violet", hex: "#7c92c6" },
              { name: "Coral", hex: "#f0786a" },
            ].map((swatch) => (
              <li
                key={swatch.hex}
                className="border-cc-card-border flex items-center gap-4 rounded-xl border px-4 py-3"
              >
                <span
                  aria-hidden
                  className="border-cc-card-border h-8 w-8 rounded-full border"
                  style={{ backgroundColor: swatch.hex }}
                />
                <span className="text-cc-heading text-sm font-semibold">
                  {swatch.name}
                </span>
                <span className="text-cc-ink-dim ml-auto font-mono text-xs">
                  {swatch.hex}
                </span>
              </li>
            ))}
          </ul>
        </div>

        <div className="border-cc-card-border bg-cc-card-bg flex flex-col gap-4 rounded-2xl border p-6 sm:p-8 lg:col-span-2">
          <Eyebrow>Product names</Eyebrow>
          <p className="text-cc-prose text-sm">
            ChilliCream is the company. The products have their own names and
            should appear exactly as written here when referenced in articles,
            talks, or release notes.
          </p>
          <ul className="grid gap-3 sm:grid-cols-2">
            {[
              {
                name: "Hot Chocolate",
                note: "The open source GraphQL server for .NET.",
              },
              {
                name: "Nitro",
                note: "Our GraphQL platform and IDE.",
              },
              {
                name: "Fusion",
                note: "Schema composition for distributed GraphQL.",
              },
              {
                name: "Strawberry Shake",
                note: "The typed .NET GraphQL client.",
              },
            ].map((product) => (
              <li
                key={product.name}
                className="border-cc-card-border flex gap-3 rounded-xl border px-4 py-3"
              >
                <span className="text-cc-accent mt-1 shrink-0">
                  <CheckIcon size={14} />
                </span>
                <div>
                  <div className="text-cc-heading text-sm font-semibold">
                    {product.name}
                  </div>
                  <div className="text-cc-ink-dim text-sm">{product.note}</div>
                </div>
              </li>
            ))}
          </ul>
          <p className="text-cc-ink-dim text-sm">
            Need an asset we do not list here? Write to{" "}
            <a
              href="mailto:contact@chillicream.com"
              className="text-cc-accent hover:text-cc-accent-hover no-underline"
            >
              contact@chillicream.com
            </a>{" "}
            and we will send what we have.
          </p>
        </div>
      </div>
    </Band>
  );
}

function LegalBand() {
  return (
    <Band
      id="legal"
      ordinal="04"
      eyebrow="Legal"
      title="The fine print, plainly listed."
      intro={
        <>
          The four legal policies that govern using ChilliCream services, plus
          the commercial license that covers paid plans. Each one opens the full
          document on its own page.
        </>
      }
    >
      <ol className="border-cc-card-border bg-cc-card-bg divide-cc-card-border flex flex-col divide-y rounded-2xl border">
        {LEGAL_LINKS.map((entry, index) => (
          <li key={entry.href}>
            <Link
              href={entry.href}
              className="group hover:bg-cc-hover flex items-center gap-6 px-5 py-5 no-underline transition-colors sm:px-7"
            >
              <span className="text-cc-ink-faint font-heading w-10 shrink-0 text-2xl font-semibold tabular-nums">
                {String(index + 1).padStart(2, "0")}
              </span>
              <div className="flex-1">
                <div className="text-cc-heading group-hover:text-cc-accent text-base font-semibold transition-colors sm:text-lg">
                  {entry.title}
                </div>
                <div className="text-cc-ink-dim mt-1 text-sm">
                  {entry.summary}
                </div>
              </div>
              <span
                aria-hidden
                className="text-cc-ink-dim group-hover:text-cc-accent font-mono text-xs transition-colors"
              >
                Read &gt;
              </span>
            </Link>
          </li>
        ))}
      </ol>
    </Band>
  );
}

function MerchBand() {
  return (
    <Band
      id="merch"
      ordinal="05"
      eyebrow="Merch"
      title="The shop."
      intro={
        <>
          Stickers, mugs, and the occasional surprise. The shop is the only
          official place to buy ChilliCream merch. Everything else floating
          around is unofficial.
        </>
      }
    >
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-6 sm:p-10">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0"
          style={{
            background: "var(--cc-promo-gradient)",
          }}
        />
        <div className="relative flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex flex-col gap-3">
            <Eyebrow>store.chillicream.com</Eyebrow>
            <h3 className="text-cc-heading font-heading text-h5 sm:text-h4 font-semibold tracking-tight">
              ChilliCream goods, shipped from one place.
            </h3>
            <p className="text-cc-prose max-w-xl text-sm sm:text-base">
              Run by us. If you are looking for stickers for a meetup or a
              hoodie for the office, this is where to get them.
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <SolidButton href="https://store.chillicream.com">
              Open the shop
            </SolidButton>
            <OutlineButton href="mailto:contact@chillicream.com">
              Email for bulk orders
            </OutlineButton>
          </div>
        </div>
      </div>
    </Band>
  );
}

function FooterCta() {
  return (
    <section className="border-cc-card-border mt-8 mb-4 rounded-2xl border px-6 py-10 text-center sm:px-10">
      <Eyebrow>One more thing</Eyebrow>
      <h2 className="text-cc-heading font-heading mt-3 text-2xl font-semibold tracking-tight sm:text-3xl">
        Cannot find what you need?
      </h2>
      <p className="text-cc-prose mx-auto mt-3 max-w-xl text-sm sm:text-base">
        Write to us. A real person reads the inbox, and we will point you to the
        right channel, the right doc, or the right human.
      </p>
      <div className="mt-6 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="mailto:contact@chillicream.com">
          contact@chillicream.com
        </SolidButton>
        <OutlineButton href="https://slack.chillicream.com/">
          Join Slack
        </OutlineButton>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ *
 * Page                                                                *
 * ------------------------------------------------------------------ */

export default function ResourcesV2Page() {
  return (
    <>
      <HeroBlock />
      <BandDivider accent />
      <ContactBand />
      <BandDivider />
      <CommunityBand />
      <BandDivider />
      <BrandBand />
      <BandDivider />
      <LegalBand />
      <BandDivider />
      <MerchBand />
      <FooterCta />
    </>
  );
}
