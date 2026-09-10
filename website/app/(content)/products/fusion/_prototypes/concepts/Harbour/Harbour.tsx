import { Fragment } from "react";
import type { ComponentType, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Link } from "@/src/design-system/Link";

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
 */

const EYEBROW =
  "text-cc-nav-label font-mono text-[10px] tracking-[0.24em] uppercase";

interface Berth {
  readonly id: string;
  readonly marker: string;
  readonly Visual: ComponentType;
  readonly ratio: string;
}

/** One berth per text block, in the order the copy lists them. */
const BERTHS: readonly Berth[] = [
  {
    id: "what-is-fusion",
    marker: "Berth 01 · the pier",
    Visual: ManifestPier,
    ratio: "4 / 3",
  },
  {
    id: "both-specifications",
    marker: "Berth 02 · the quay",
    Visual: QuayFlags,
    ratio: "4 / 3",
  },
  {
    id: "any-server",
    marker: "Berth 03 · the customs gate",
    Visual: CustomsGate,
    ratio: "4 / 3",
  },
  {
    id: "client-safety",
    marker: "Berth 04 · the harbour log",
    Visual: HarbourLog,
    ratio: "4 / 3",
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
    <p className="text-cc-ink-dim mt-6 text-sm">
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
      <PageSection maxWidth="6xl" className="py-16 sm:py-24">
        <div className="grid items-center gap-10 lg:grid-cols-2 lg:gap-16">
          <div className={flipped ? "lg:order-2" : undefined}>
            <p className={EYEBROW}>{berth.marker}</p>
            <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 text-balance">
              {section.title}
            </h2>
            <div className="text-cc-prose mt-6 space-y-4 text-base">
              {section.paragraphs.map((paragraph) => (
                <p key={paragraph.slice(0, 24)}>
                  {withLinks(paragraph, section.links)}
                </p>
              ))}
            </div>
            <InPractice links={section.inPractice} />
          </div>

          <div className={flipped ? "lg:order-1" : undefined}>
            <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-2xl border">
              <Scene ratio={berth.ratio}>
                <Visual />
              </Scene>
            </div>
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
      <section className="relative isolate flex min-h-[86svh] items-end overflow-hidden">
        <div className="absolute inset-0">
          <Scene className="h-full">
            <HarbourHero />
          </Scene>
        </div>
        <div
          aria-hidden="true"
          className="from-cc-bg via-cc-bg/70 absolute inset-0 bg-gradient-to-t to-transparent"
        />
        <PageSection maxWidth="6xl" className="relative pt-40 pb-20">
          <p className={EYEBROW}>{HERO.eyebrow}</p>
          <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-4">
            {HERO.title}
          </h1>
          <p className="text-cc-prose mt-6 max-w-2xl text-lg sm:text-xl">
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
        <PageSection maxWidth="6xl" className="py-16 sm:py-24">
          <div className="grid items-center gap-10 lg:grid-cols-2 lg:gap-16">
            <div>
              <p className={EYEBROW}>The beacon</p>
              <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 text-balance">
                {NITRO_BAND.title}
              </h2>
              <p className="text-cc-prose mt-6 text-base">
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
            <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-2xl border">
              <Scene ratio="16 / 9">
                <BeaconSweep />
              </Scene>
            </div>
          </div>
        </PageSection>
      </div>
    </div>
  );
}
