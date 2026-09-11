import { Fragment } from "react";
import type { ComponentType, ReactNode } from "react";

import { ButtonRow } from "@/src/components/ButtonRow";
import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { Link } from "@/src/design-system/Link";

import { BRAND } from "../../brand";
import type { CopyLink, CopySection } from "../../copy";
import { HERO, NITRO_BAND, SECTIONS } from "../../copy";
import { Scene } from "../../Primitives";
import { AsBuiltRecord } from "./AsBuiltRecord";
import { DimensionChain } from "./DimensionChain";
import { GaugeSchedule } from "./GaugeSchedule";
import { HeroAssembly } from "./HeroAssembly";
import { BP } from "./palette";
import { SpecStamps } from "./SpecStamps";
import { ToleranceCheck } from "./ToleranceCheck";

/**
 * Fusion product page, concept v7: Blueprint.
 *
 * The page is a set of engineering drawings. Every plate is cyan line-work on
 * the site's navy surfaces: the gateway is the main assembly, each subgraph is
 * a sub-assembly with a part number, and every claim in the copy is answered
 * by a plate that draws itself on - a dimension chain for one query, a stamped
 * parts sheet for the two specifications, a tolerance check that red-lines a
 * conflict, an as-built record swept against a revision, an instrumentation
 * schedule of live dials. The page itself is the site's: `bg-cc-bg`, the
 * shared container and rhythm, the site type scale; the drawing lives inside
 * the scene boxes. The copy is the production page's, imported verbatim from
 * `../../copy`.
 */

interface Plate {
  readonly id: string;
  /** Drawing number printed on the note rail, e.g. "SHEET 101". */
  readonly sheet: string;
  /** What the plate is of, printed under the number. */
  readonly subject: string;
  readonly Visual: ComponentType;
  readonly ratio: string;
  /** Accessible name for the scene. */
  readonly label: string;
}

/** One plate per text block, in the order the copy lists them. */
const PLATES: readonly Plate[] = [
  {
    id: "what-is-fusion",
    sheet: "SHEET 101",
    subject: "Query resolution",
    Visual: DimensionChain,
    ratio: "16 / 9",
    label:
      "One query from one client drawn as a dimension chain: the gateway takes one measurement off each subgraph and closes the chain with a single overall dimension, one response off one endpoint.",
  },
  {
    id: "both-specifications",
    sheet: "SHEET 102",
    subject: "Specification stamps",
    Visual: SpecStamps,
    ratio: "1 / 1",
    label:
      "Five source schemas stamped with the specification each is written to, GraphQL Federation and Apollo Federation drafted identically, running down one trunk into a single composite schema together with an OpenAPI and a gRPC source on adapter drawings.",
  },
  {
    id: "any-server",
    sheet: "SHEET 103",
    subject: "Tolerance check",
    Visual: ToleranceCheck,
    ratio: "4 / 3",
    label:
      "Composition drawn as a tolerance check between two ordinary GraphQL servers written in different languages: the field types disagree, a red-line revision cloud goes round the conflict and the drawing set is rejected before anything is deployed.",
  },
  {
    id: "client-safety",
    sheet: "SHEET 104",
    subject: "As-built record",
    Visual: AsBuiltRecord,
    ratio: "4 / 3",
    label:
      "A revision that removes a field still passes composition, while Nitro sweeps the as-built record of operations published by real clients and marks the mobile app's operation breaking before the merge.",
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

/**
 * Blueprint light over the hero: two soft glows in the concept's dimension
 * cyan. Atmosphere only - the page background stays `bg-cc-bg` underneath.
 */
function BlueprintAtmosphere() {
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 left-1/2 -z-10 w-screen -translate-x-1/2 overflow-hidden"
    >
      <div
        className="absolute top-[6%] right-0 h-[36rem] w-[36rem] translate-x-1/4 rounded-full opacity-80 blur-3xl"
        style={{
          background: `radial-gradient(circle, color-mix(in srgb, ${BRAND.cyan} 14%, transparent), transparent 68%)`,
        }}
      />
      <div
        className="absolute bottom-0 left-0 h-[30rem] w-[30rem] -translate-x-1/3 rounded-full opacity-70 blur-3xl"
        style={{
          background: `radial-gradient(circle, color-mix(in srgb, ${BRAND.teal} 12%, transparent), transparent 68%)`,
        }}
      />
    </div>
  );
}

interface NoteRailProps {
  readonly sheet: string;
  readonly subject: string;
}

/** The drafting note every text block is filed under. */
function NoteRail({ sheet, subject }: NoteRailProps) {
  return (
    <div className="mb-6 flex items-center gap-3">
      <Eyebrow as="span" size="2xs">
        {sheet}
      </Eyebrow>
      <span
        aria-hidden="true"
        className="h-px flex-1"
        style={{ background: BP.inkFaint }}
      />
      <Eyebrow as="span" size="2xs" color="ink-dim">
        {subject}
      </Eyebrow>
    </div>
  );
}

interface PlateFrameProps {
  readonly ratio: string;
  readonly label: string;
  readonly children: ReactNode;
}

/** The drawn frame a plate is pinned inside. */
function PlateFrame({ ratio, label, children }: PlateFrameProps) {
  return (
    <div className="border-cc-card-border overflow-hidden rounded-sm border">
      <Scene ratio={ratio} label={label}>
        {children}
      </Scene>
    </div>
  );
}

interface PlateSectionProps {
  readonly plate: Plate;
  readonly section: CopySection;
  readonly flipped: boolean;
}

function PlateSection({ plate, section, flipped }: PlateSectionProps) {
  const { Visual } = plate;

  return (
    <section id={section.id} className="border-cc-card-border border-t">
      <PageSection maxWidth="6xl" className="py-20 sm:py-28">
        <NoteRail sheet={plate.sheet} subject={plate.subject} />
        <div className="grid items-start gap-10 lg:grid-cols-2 lg:gap-16">
          <div className={flipped ? "lg:order-2" : undefined}>
            <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
              {section.title}
            </h2>
            <div className="text-cc-ink-dim text-body mt-6 space-y-4">
              {section.paragraphs.map((paragraph) => (
                <p key={paragraph.slice(0, 24)}>
                  {withLinks(paragraph, section.links)}
                </p>
              ))}
            </div>
            <InPractice links={section.inPractice} />
          </div>

          <div className={flipped ? "lg:order-1" : undefined}>
            <PlateFrame ratio={plate.ratio} label={plate.label}>
              <Visual />
            </PlateFrame>
          </div>
        </div>
      </PageSection>
    </section>
  );
}

export function Blueprint() {
  return (
    <div className="bg-cc-bg">
      {/* Hero: the general arrangement drawing of the whole gateway */}
      <section className="relative isolate overflow-hidden">
        <BlueprintAtmosphere />
        <PageSection
          maxWidth="7xl"
          className="flex min-h-[86svh] items-center py-20 sm:py-28"
        >
          <div className="grid w-full items-center gap-12 lg:grid-cols-[1fr_1.05fr] lg:gap-16">
            <div>
              <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
                <Eyebrow as="span" size="2xs">
                  DWG-100
                </Eyebrow>
                <Eyebrow as="span" size="2xs" color="ink-dim">
                  {HERO.eyebrow}
                </Eyebrow>
              </div>
              <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-6 text-balance">
                {HERO.title}
              </h1>
              <div
                aria-hidden="true"
                className="mt-6 h-px w-full max-w-md"
                style={{ background: BP.inkFaint }}
              />
              <p className="text-cc-ink text-body sm:text-lead mt-6 max-w-2xl">
                {HERO.teaser}
              </p>
              <ButtonRow align="start" className="mt-10">
                <SolidButton href={HERO.buttons[0].href}>
                  {HERO.buttons[0].label}
                </SolidButton>
                <OutlineButton href={HERO.buttons[1].href}>
                  {HERO.buttons[1].label}
                </OutlineButton>
              </ButtonRow>
            </div>

            <PlateFrame
              ratio="4 / 3"
              label="The general arrangement drawing of the Fusion gateway: the main assembly in the middle, five GraphQL sub-assemblies and two bought-in sources ballooned around it, lifting into an exploded view that turns on the table before it settles back together."
            >
              <HeroAssembly />
            </PlateFrame>
          </div>
        </PageSection>
      </section>

      {PLATES.map((plate, i) => {
        const section = SECTIONS.find((s) => s.id === plate.id);
        if (!section) return null;

        return (
          <PlateSection
            key={plate.id}
            plate={plate}
            section={section}
            flipped={i % 2 === 1}
          />
        );
      })}

      {/* Nitro band: the instrumentation schedule of the drawing set */}
      <section id={NITRO_BAND.id} className="border-cc-card-border border-t">
        <PageSection maxWidth="6xl" className="py-20 sm:py-28">
          <NoteRail sheet="SHEET 105" subject="Instrumentation" />
          <div className="grid items-start gap-10 lg:grid-cols-2 lg:gap-16">
            <div>
              <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
                {NITRO_BAND.title}
              </h2>
              <p className="text-cc-ink-dim text-body mt-6">
                {NITRO_BAND.description}
              </p>
              <ButtonRow align="start" className="mt-10">
                <SolidButton href={NITRO_BAND.buttons[0].href}>
                  {NITRO_BAND.buttons[0].label}
                </SolidButton>
                <OutlineButton href={NITRO_BAND.buttons[1].href}>
                  {NITRO_BAND.buttons[1].label}
                </OutlineButton>
              </ButtonRow>
            </div>

            <PlateFrame
              ratio="2 / 1"
              label="An instrumentation schedule: three dials read the gateway's latency, throughput and error rate, and a smaller dial reads each subgraph behind it."
            >
              <GaugeSchedule />
            </PlateFrame>
          </div>
        </PageSection>
      </section>
    </div>
  );
}
