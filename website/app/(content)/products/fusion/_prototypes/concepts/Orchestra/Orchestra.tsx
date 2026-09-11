import { Fragment } from "react";
import type { ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Link } from "@/src/design-system/Link";

import { CC } from "../../brand";
import { HERO, NITRO_BAND, SECTIONS } from "../../copy";
import type { CopyLink } from "../../copy";
import { Scene } from "../../Primitives";
import { HouseMeters } from "./HouseMeters";
import { OnePiece } from "./OnePiece";
import { Recording } from "./Recording";
import { Rehearsal } from "./Rehearsal";
import { ScoreStrip } from "./ScoreStrip";
import { TwoNotations } from "./TwoNotations";

/**
 * Concept v4 "Orchestra": the Fusion page as a conductor's score.
 *
 * The gateway conducts, every subgraph is a section of the orchestra, and the
 * page is laid out on staff lines: four movements, each a text block beside
 * the animated figure that plays its claim. Copy is imported verbatim from
 * `_prototypes/copy.ts`; only the layout, the typography and the motion are
 * this concept's own.
 */

const MOVEMENTS = ["I", "II", "III", "IV"];

/** Staff lines behind a block, drawn with a gradient so nothing shifts. */
const STAFF_LINE = `color-mix(in srgb, ${CC.inkFaint} 40%, transparent)`;
const STAVES = `repeating-linear-gradient(to bottom, transparent 0px, transparent 15px, ${STAFF_LINE} 15px, ${STAFF_LINE} 16px)`;

interface EngravedProps {
  readonly children: ReactNode;
  readonly className?: string;
}

/** A small engraved label in the score's monospace voice. */
function Engraved({ children, className }: EngravedProps) {
  return (
    <span
      className={`text-cc-nav-label font-mono text-[11px] tracking-[0.24em] uppercase ${className ?? ""}`.trim()}
    >
      {children}
    </span>
  );
}

/**
 * Renders one fixed paragraph, re-linking the phrases the production page
 * links. The words are never changed: the paragraph is split on the link
 * label and put back together around a `Link`.
 */
interface ParagraphProps {
  readonly text: string;
  readonly links: readonly CopyLink[];
}

function Paragraph({ text, links }: ParagraphProps) {
  let parts: ReactNode[] = [text];

  for (const link of links) {
    const next: ReactNode[] = [];
    for (const part of parts) {
      if (typeof part !== "string" || !part.includes(link.label)) {
        next.push(part);
        continue;
      }
      const [head, ...rest] = part.split(link.label);
      next.push(
        head,
        <Link key={link.href} href={link.href}>
          {link.label}
        </Link>,
        rest.join(link.label),
      );
    }
    parts = next;
  }

  return (
    <p className="text-cc-prose text-body">
      {parts.map((part, i) => (
        <Fragment key={i}>{part}</Fragment>
      ))}
    </p>
  );
}

interface InPracticeProps {
  readonly links: readonly CopyLink[];
}

function InPractice({ links }: InPracticeProps) {
  if (links.length === 0) {
    return null;
  }

  return (
    <p className="text-cc-ink-dim text-caption">
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

interface MovementProps {
  readonly index: number;
  readonly section: (typeof SECTIONS)[number];
  readonly ratio: string;
  readonly visual: ReactNode;
}

/** One text block beside the figure that plays it; the sides alternate. */
function Movement({ index, section, ratio, visual }: MovementProps) {
  const flipped = index % 2 === 1;

  return (
    <PageSection
      maxWidth="6xl"
      className="border-cc-card-border border-t py-20 sm:py-28"
    >
      <div className="grid items-center gap-10 lg:grid-cols-2 lg:gap-16">
        <div className={flipped ? "lg:order-2" : undefined}>
          <div className="flex items-baseline gap-3">
            <Engraved className="text-cc-accent">{`Movement ${MOVEMENTS[index]}`}</Engraved>
            <span className="bg-cc-card-border h-px flex-1" />
          </div>
          <h2
            id={section.id}
            className="text-cc-heading font-heading text-h4 sm:text-h3 mt-4 text-balance"
          >
            {section.title}
          </h2>
          <div className="mt-6 space-y-4">
            {section.paragraphs.map((paragraph) => (
              <Paragraph
                key={paragraph.slice(0, 24)}
                text={paragraph}
                links={section.links}
              />
            ))}
            <InPractice links={section.inPractice} />
          </div>
        </div>

        <figure
          className={`border-cc-card-border bg-cc-surface m-0 overflow-hidden rounded-xl border ${flipped ? "lg:order-1" : ""}`.trim()}
        >
          <Scene ratio={ratio} label={section.title}>
            {visual}
          </Scene>
        </figure>
      </div>
    </PageSection>
  );
}

const VISUALS: readonly { ratio: string; node: ReactNode }[] = [
  { ratio: "720 / 420", node: <OnePiece /> },
  { ratio: "720 / 440", node: <TwoNotations /> },
  { ratio: "720 / 420", node: <Rehearsal /> },
  { ratio: "720 / 440", node: <Recording /> },
];

export function Orchestra() {
  return (
    <main className="bg-cc-bg">
      <PageSection
        maxWidth="7xl"
        className="flex min-h-[88svh] flex-col justify-center py-20 sm:py-28"
      >
        <div style={{ backgroundImage: STAVES }} className="py-10">
          <Engraved>{HERO.eyebrow}</Engraved>
          <h1 className="text-cc-heading font-heading text-h2 sm:text-h1 mt-4 text-balance">
            {HERO.title}
          </h1>
          <p className="text-cc-prose text-body sm:text-lead mt-6 max-w-2xl">
            {HERO.teaser}
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <SolidButton href={HERO.buttons[0].href}>
              {HERO.buttons[0].label}
            </SolidButton>
            <OutlineButton href={HERO.buttons[1].href}>
              {HERO.buttons[1].label}
            </OutlineButton>
          </div>
        </div>

        <figure className="border-cc-card-border bg-cc-surface m-0 mt-10 overflow-hidden rounded-xl border">
          <Scene
            ratio="960 / 380"
            label="The conductor's score: every source schema on its own staff, played into one piece"
          >
            <ScoreStrip />
          </Scene>
        </figure>
      </PageSection>

      {SECTIONS.map((section, i) => (
        <Movement
          key={section.id}
          index={i}
          section={section}
          ratio={VISUALS[i].ratio}
          visual={VISUALS[i].node}
        />
      ))}

      <PageSection
        maxWidth="6xl"
        className="border-cc-card-border border-t py-20 sm:py-28"
      >
        <div
          id={NITRO_BAND.id}
          className="grid items-center gap-10 lg:grid-cols-2 lg:gap-16"
        >
          <div>
            <Engraved className="text-cc-accent">Encore</Engraved>
            <h2 className="text-cc-heading font-heading text-h4 sm:text-h3 mt-4 text-balance">
              {NITRO_BAND.title}
            </h2>
            <p className="text-cc-prose text-body mt-6">
              {NITRO_BAND.description}
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href={NITRO_BAND.buttons[0].href}>
                {NITRO_BAND.buttons[0].label}
              </SolidButton>
              <OutlineButton href={NITRO_BAND.buttons[1].href}>
                {NITRO_BAND.buttons[1].label}
              </OutlineButton>
            </div>
          </div>

          <figure className="border-cc-card-border bg-cc-surface m-0 overflow-hidden rounded-xl border">
            <Scene ratio="520 / 230" label={NITRO_BAND.title}>
              <HouseMeters />
            </Scene>
          </figure>
        </div>
      </PageSection>
    </main>
  );
}
