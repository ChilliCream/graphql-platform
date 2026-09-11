import { Fragment } from "react";
import type { ComponentType, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { Link } from "@/src/design-system/Link";

import { BRAND } from "../../brand";
import type { CopyLink, CopySection } from "../../copy";
import { HERO, NITRO_BAND, SECTIONS } from "../../copy";
import { Scene } from "../../Primitives";
import { BeaconSweep } from "./BeaconSweep";
import { CustomsGate } from "./CustomsGate";
import { HarbourHero } from "./HarbourHero";
import { HarbourLog } from "./HarbourLog";
import { ManifestPier } from "./ManifestPier";
import { QuayFlags } from "./QuayFlags";

/**
 * Fusion product page, concept v2: Harbour.
 *
 * The page is read as a port at dusk. Clients are ships that all dock at one
 * pier, subgraphs are warehouses along the quay flying a language flag and a
 * specification pennant, composition is the customs check every manifest
 * passes before departure, and Nitro is the harbour log of which ship actually
 * carries which container. The words are the production page's, imported from
 * `../../copy`; only the layout, the berth chrome and the six animated scenes
 * belong to the concept.
 *
 * The page keeps the site's grid: `bg-cc-bg` under every section, a
 * `PageSection` container per section and the site's vertical rhythm. The dusk
 * of the harbour lives in the bounded scene boxes and in one soft full-bleed
 * glow behind the hero, never as a background of its own.
 */

interface Berth {
  readonly id: string;
  readonly marker: string;
  readonly Visual: ComponentType;
  readonly ratio: string;
  /** Accessible name for the scene; the SVG inside it is decorative. */
  readonly label: string;
}

/** One berth per text block, in the order the copy lists them. */
const BERTHS: readonly Berth[] = [
  {
    id: "what-is-fusion",
    marker: "Berth 01 · the pier",
    Visual: ManifestPier,
    ratio: "4 / 3",
    label:
      "One query is loaded as containers from four warehouses onto the single ship at the pier.",
  },
  {
    id: "both-specifications",
    marker: "Berth 02 · the quay",
    Visual: QuayFlags,
    ratio: "4 / 3",
    label:
      "Warehouses flying GraphQL Federation and Apollo Federation pennants, plus OpenAPI and gRPC warehouses, all moored to one harbour.",
  },
  {
    id: "any-server",
    marker: "Berth 03 · the customs gate",
    Visual: CustomsGate,
    ratio: "4 / 3",
    label:
      "Five source schemas pass the customs check; a conflicting manifest drops the barrier and stops the build.",
  },
  {
    id: "client-safety",
    marker: "Berth 04 · the harbour log",
    Visual: HarbourLog,
    ratio: "4 / 3",
    label:
      "Composition still passes after a container is withdrawn; the harbour log marks the mobile client's operation as breaking.",
  },
];

/**
 * Re-links the phrases the production page links, without touching a word of
 * the paragraph: the label is matched verbatim inside the string and wrapped.
 */
function withLinks(text: string, links: readonly CopyLink[]): ReactNode {
  let parts: ReactNode[] = [text];

  for (const link of links) {
    const next: ReactNode[] = [];
    for (const part of parts) {
      if (typeof part !== "string") {
        next.push(part);
        continue;
      }
      const at = part.indexOf(link.label);
      if (at === -1) {
        next.push(part);
        continue;
      }
      next.push(
        part.slice(0, at),
        <Link key={link.href} href={link.href}>
          {link.label}
        </Link>,
        part.slice(at + link.label.length),
      );
    }
    parts = next;
  }

  return parts.map((part, i) => <Fragment key={i}>{part}</Fragment>);
}

interface InPracticeProps {
  readonly links: readonly CopyLink[];
}

function InPractice({ links }: InPracticeProps) {
  if (links.length === 0) return null;

  return (
    <p className="text-cc-ink-dim text-caption mt-6">
      In practice:{" "}
      {links.map((link, i) => (
        <Fragment key={link.href}>
          {i > 0 ? " and " : null}
          <Link href={link.href}>{link.label}</Link>
        </Fragment>
      ))}
      .
    </p>
  );
}

interface BerthingProps {
  readonly ratio: string;
  readonly label: string;
  readonly children: ReactNode;
}

/** Bounded stage every Harbour scene is drawn in. */
function Berthing({ ratio, label, children }: BerthingProps) {
  return (
    <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-2xl border">
      <Scene ratio={ratio} label={label}>
        {children}
      </Scene>
    </div>
  );
}

/**
 * Dusk over the harbour: three soft glows behind the hero, in the brand
 * accents the scenes use. Atmosphere only - the page background stays
 * `bg-cc-bg` underneath.
 */
function HarbourAtmosphere() {
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 left-1/2 -z-10 w-screen -translate-x-1/2 overflow-hidden"
    >
      <div
        className="absolute top-[8%] right-0 h-[38rem] w-[38rem] translate-x-1/4 rounded-full opacity-80 blur-3xl"
        style={{
          background: `radial-gradient(circle, color-mix(in srgb, ${BRAND.amber} 16%, transparent), transparent 68%)`,
        }}
      />
      <div
        className="absolute top-[36%] left-0 h-[34rem] w-[34rem] -translate-x-1/4 rounded-full opacity-80 blur-3xl"
        style={{
          background: `radial-gradient(circle, color-mix(in srgb, ${BRAND.cyan} 14%, transparent), transparent 68%)`,
        }}
      />
      <div
        className="absolute bottom-0 left-1/2 h-[30rem] w-[30rem] -translate-x-1/2 rounded-full opacity-70 blur-3xl"
        style={{
          background: `radial-gradient(circle, color-mix(in srgb, ${BRAND.violet} 14%, transparent), transparent 68%)`,
        }}
      />
    </div>
  );
}

/** Mooring rail between two berths: a hairline with three bollards. */
function QuayRail() {
  return (
    <div
      aria-hidden="true"
      className="mx-auto flex max-w-6xl items-center gap-3 px-5 sm:px-12"
    >
      <span className="bg-cc-card-border h-px flex-1" />
      <span className="bg-cc-ink-faint h-1.5 w-1.5 rounded-full" />
      <span className="bg-cc-ink-faint h-1.5 w-1.5 rounded-full" />
      <span className="bg-cc-ink-faint h-1.5 w-1.5 rounded-full" />
      <span className="bg-cc-card-border h-px flex-1" />
    </div>
  );
}

interface BerthSectionProps {
  readonly berth: Berth;
  readonly section: CopySection;
  readonly flipped: boolean;
}

function BerthSection({ berth, section, flipped }: BerthSectionProps) {
  const { Visual } = berth;

  return (
    <div id={section.id}>
      <PageSection maxWidth="6xl" className="py-20 sm:py-28">
        <div className="grid items-center gap-10 lg:grid-cols-2 lg:gap-16">
          <div className={flipped ? "lg:order-2" : undefined}>
            <Eyebrow>{berth.marker}</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 text-balance">
              {section.title}
            </h2>
            <div className="text-cc-prose text-body mt-6 space-y-4">
              {section.paragraphs.map((paragraph) => (
                <p key={paragraph.slice(0, 24)}>
                  {withLinks(paragraph, section.links)}
                </p>
              ))}
            </div>
            <InPractice links={section.inPractice} />
          </div>

          <div className={flipped ? "lg:order-1" : undefined}>
            <Berthing ratio={berth.ratio} label={berth.label}>
              <Visual />
            </Berthing>
          </div>
        </div>
      </PageSection>
    </div>
  );
}

export function Harbour() {
  return (
    <div className="bg-cc-bg">
      {/* Hero: the harbour at dusk */}
      <section className="relative isolate overflow-hidden">
        <HarbourAtmosphere />
        <PageSection maxWidth="7xl" className="py-20 sm:py-28">
          <Eyebrow>{HERO.eyebrow}</Eyebrow>
          <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-4 max-w-4xl text-balance">
            {HERO.title}
          </h1>
          <p className="text-cc-prose text-body sm:text-lead mt-6 max-w-2xl">
            {HERO.teaser}
          </p>
          <div className="mt-10 flex flex-wrap gap-4">
            <SolidButton href={HERO.buttons[0].href}>
              {HERO.buttons[0].label}
            </SolidButton>
            <OutlineButton href={HERO.buttons[1].href}>
              {HERO.buttons[1].label}
            </OutlineButton>
          </div>
          <div className="mt-14 sm:mt-16">
            <Berthing
              ratio="12 / 7"
              label="A harbour at dusk: the fleet of client ships rides at anchor in front of the lit warehouses on the quay, under the harbour master's sweeping beam."
            >
              <HarbourHero />
            </Berthing>
          </div>
        </PageSection>
      </section>

      {BERTHS.map((berth, i) => {
        const section = SECTIONS.find((s) => s.id === berth.id);
        if (!section) return null;

        return (
          <Fragment key={berth.id}>
            {i > 0 ? <QuayRail /> : null}
            <BerthSection
              berth={berth}
              section={section}
              flipped={i % 2 === 1}
            />
          </Fragment>
        );
      })}

      {/* Nitro band: the beacon over the basin */}
      <div id={NITRO_BAND.id} className="border-cc-card-border border-t">
        <PageSection maxWidth="6xl" className="py-20 sm:py-28">
          <div className="grid items-center gap-10 lg:grid-cols-2 lg:gap-16">
            <div>
              <Eyebrow>The beacon</Eyebrow>
              <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 text-balance">
                {NITRO_BAND.title}
              </h2>
              <p className="text-cc-prose text-body mt-6">
                {NITRO_BAND.description}
              </p>
              <div className="mt-10 flex flex-wrap gap-4">
                <SolidButton href={NITRO_BAND.buttons[0].href}>
                  {NITRO_BAND.buttons[0].label}
                </SolidButton>
                <OutlineButton href={NITRO_BAND.buttons[1].href}>
                  {NITRO_BAND.buttons[1].label}
                </OutlineButton>
              </div>
            </div>
            <Berthing
              ratio="16 / 9"
              label="A harbour beacon sweeping berths that report latency, throughput and error rate for the gateway and each subgraph."
            >
              <BeaconSweep />
            </Berthing>
          </div>
        </PageSection>
      </div>
    </div>
  );
}
