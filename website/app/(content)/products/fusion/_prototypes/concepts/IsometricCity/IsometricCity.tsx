import { Fragment } from "react";
import type { ComponentType, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Link } from "@/src/design-system/Link";

import type { CopyLink, CopySection } from "../../copy";
import { HERO, NITRO_BAND, SECTIONS } from "../../copy";
import { Scene } from "../../Primitives";
import { CityHero } from "./CityHero";
import { CityMeters } from "./CityMeters";
import { DistrictFlags } from "./DistrictFlags";
import { QueryRoute } from "./QueryRoute";
import { TrafficCameraLog } from "./TrafficCameraLog";
import { ZoningPermit } from "./ZoningPermit";

/**
 * Fusion product page, concept v6: Isometric City.
 *
 * The page is read as a 2.5D city block seen from above. The gateway is the
 * plaza with one door, clients are the vehicles that arrive at it, subgraphs
 * are buildings with language signage and a specification flag on the roof,
 * OpenAPI and gRPC are buildings with another facade on the same road,
 * composition is the zoning inspection a building passes before it opens, and
 * Nitro is the traffic camera log of which vehicle really drives which road.
 *
 * The words are the production page's, imported from `../../copy`; only the
 * layout, the block chrome and the five animated scenes belong to the concept.
 */

const EYEBROW =
  "text-cc-nav-label font-mono text-[10px] tracking-[0.24em] uppercase";

interface Block {
  readonly id: string;
  readonly marker: string;
  readonly Visual: ComponentType;
  readonly ratio: string;
  /** Accessible name for the scene; the SVG inside it is decorative. */
  readonly label: string;
}

/** One city block per text block, in the order the copy lists them. */
const BLOCKS: readonly Block[] = [
  {
    id: "what-is-fusion",
    marker: "Block 01 · the plaza",
    Visual: QueryRoute,
    ratio: "4 / 3",
    label:
      "One route across the city: a vehicle stops at the plaza's single door and the gateway's route visits four buildings and returns.",
  },
  {
    id: "both-specifications",
    marker: "Block 02 · the street of flags",
    Visual: DistrictFlags,
    ratio: "4 / 3",
    label:
      "Buildings flying GraphQL Federation and Apollo Federation flags, one flying both, and OpenAPI and gRPC facades on the same road into the plaza.",
  },
  {
    id: "any-server",
    marker: "Block 03 · the zoning office",
    Visual: ZoningPermit,
    ratio: "4 / 3",
    label:
      "Five buildings in five languages already stand; the sixth lot fails its zoning inspection on an incompatible enum and the boom stays down.",
  },
  {
    id: "client-safety",
    marker: "Block 04 · the junction camera",
    Visual: TrafficCameraLog,
    ratio: "4 / 3",
    label:
      "A road is closed and the zoning stamp stays green, while the traffic camera log marks the mobile client's trip on that road as breaking.",
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

/** The street between two blocks: a kerb, a centre line and a kerb. */
function Street() {
  return (
    <div
      aria-hidden="true"
      className="mx-auto flex max-w-6xl items-center gap-2 px-5 sm:px-12"
    >
      <span className="bg-cc-card-border h-px flex-1" />
      <span className="bg-cc-ink-faint h-px w-6" />
      <span className="bg-cc-ink-faint h-px w-3" />
      <span className="bg-cc-ink-faint h-px w-6" />
      <span className="bg-cc-card-border h-px flex-1" />
    </div>
  );
}

interface CityBlockSectionProps {
  readonly block: Block;
  readonly section: CopySection;
  readonly flipped: boolean;
}

function CityBlockSection({ block, section, flipped }: CityBlockSectionProps) {
  const { Visual } = block;

  return (
    <div id={section.id}>
      <PageSection maxWidth="6xl" className="py-16 sm:py-24">
        <div className="grid items-center gap-10 lg:grid-cols-2 lg:gap-16">
          <div className={flipped ? "lg:order-2" : undefined}>
            <p className={EYEBROW}>{block.marker}</p>
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
              <Scene ratio={block.ratio} label={block.label}>
                <Visual />
              </Scene>
            </div>
          </div>
        </div>
      </PageSection>
    </div>
  );
}

export function IsometricCity() {
  return (
    <div className="bg-cc-bg">
      {/* Hero: the city assembling block by block */}
      <section className="relative isolate flex min-h-[88svh] items-end overflow-hidden">
        <div className="absolute inset-0">
          <Scene className="h-full">
            <CityHero />
          </Scene>
        </div>
        <div
          aria-hidden="true"
          className="from-cc-bg via-cc-bg/75 absolute inset-0 bg-gradient-to-t to-transparent"
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

      {BLOCKS.map((block, i) => {
        const section = SECTIONS.find((s) => s.id === block.id);
        if (!section) return null;

        return (
          <Fragment key={block.id}>
            {i > 0 ? <Street /> : null}
            <CityBlockSection
              block={block}
              section={section}
              flipped={i % 2 === 1}
            />
          </Fragment>
        );
      })}

      {/* Nitro band: the traffic desk over the city */}
      <div id={NITRO_BAND.id} className="border-cc-card-border border-t">
        <PageSection maxWidth="6xl" className="py-16 sm:py-24">
          <div className="grid items-center gap-10 lg:grid-cols-2 lg:gap-16">
            <div>
              <p className={EYEBROW}>The traffic desk</p>
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
              <Scene
                ratio="16 / 9"
                label="A traffic desk reading latency, throughput and error rate for the gateway and for each subgraph behind it."
              >
                <CityMeters />
              </Scene>
            </div>
          </div>
        </PageSection>
      </div>
    </div>
  );
}
