import { Fragment } from "react";
import type { ReactElement, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Link } from "@/src/design-system/Link";

import { Scene } from "../../Primitives";
import type { CopyLink, CopySection } from "../../copy";
import { HERO, NITRO_BAND, SECTIONS } from "../../copy";
import { ApronMeters } from "./ApronMeters";
import { BoardingCheck } from "./BoardingCheck";
import { ClearanceCheck } from "./ClearanceCheck";
import { FlightPlan } from "./FlightPlan";
import { GateMarkings } from "./GateMarkings";
import { HeroApron } from "./HeroApron";
import { AP } from "./palette";
import { SplitFlapBoard } from "./SplitFlapBoard";

/**
 * Fusion product page, concept v5: Airport.
 *
 * The gateway is the tower, the subgraphs are gates and every client lands on
 * the one runway. The hero is the apron under a split-flap departures board,
 * each text block sits beside the stand that demonstrates it, and the page
 * closes on the on-time meters. All words come from `../../copy`.
 */

const PANEL_CLASS = "rounded-xl border overflow-hidden";

const panelStyle = {
  background: AP.panel,
  borderColor: AP.panelEdge,
} as const;

const monoStyle = { fontFamily: AP.mono } as const;

/** Re-links the phrases the production page links, leaving the words untouched. */
function withLinks(text: string, links: readonly CopyLink[]): ReactNode {
  let parts: (string | ReactElement)[] = [text];

  for (const link of links) {
    parts = parts.flatMap((part) => {
      if (typeof part !== "string") return [part];
      const at = part.indexOf(link.label);
      if (at < 0) return [part];
      return [
        part.slice(0, at),
        <Link key={link.href} href={link.href}>
          {link.label}
        </Link>,
        part.slice(at + link.label.length),
      ];
    });
  }

  return parts.map((part, i) => <Fragment key={i}>{part}</Fragment>);
}

interface InPracticeProps {
  readonly links: readonly CopyLink[];
}

function InPractice({ links }: InPracticeProps) {
  if (links.length === 0) return null;

  return (
    <p className="text-cc-ink-dim text-sm">
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

/** One stand per text block, in the order the copy declares them. */
const VISUALS: Readonly<Record<string, ReactNode>> = {
  "what-is-fusion": <FlightPlan />,
  "both-specifications": <GateMarkings />,
  "any-server": <ClearanceCheck />,
  "client-safety": <BoardingCheck />,
};

/** Gate designator printed above each text block, in the copy's order. */
const STANDS = ["A1", "A2", "B1", "B2"] as const;

interface StandRowProps {
  readonly index: number;
  readonly section: CopySection;
  readonly visual: ReactNode;
}

/** One text block and the stand that demonstrates its claim. */
function StandRow({ index, section, visual }: StandRowProps) {
  const flip = index % 2 === 1;

  return (
    <PageSection maxWidth="6xl" className="py-16 sm:py-24">
      <div id={section.id} className="grid items-center gap-10 lg:grid-cols-2">
        <div className={flip ? "lg:order-2" : undefined}>
          <p
            className="mb-3 text-[10px] tracking-[0.28em]"
            style={{ ...monoStyle, color: AP.amber }}
          >
            {`GATE ${STANDS[index] ?? String(index + 1)}`}
          </p>
          <h2 className="text-cc-heading text-h3 font-heading mb-5">
            {section.title}
          </h2>
          <div className="text-cc-ink-dim space-y-4 text-base">
            {section.paragraphs.map((paragraph) => (
              <p key={paragraph}>{withLinks(paragraph, section.links)}</p>
            ))}
            <InPractice links={section.inPractice} />
          </div>
        </div>

        <div
          className={`${PANEL_CLASS} ${flip ? "lg:order-1" : ""}`.trim()}
          style={panelStyle}
        >
          <Scene ratio="16 / 11">{visual}</Scene>
        </div>
      </div>
    </PageSection>
  );
}

export function Airport() {
  return (
    <div style={{ background: AP.bg }}>
      <section className="relative flex min-h-[88svh] items-center overflow-hidden">
        <HeroApron />
        <div
          aria-hidden="true"
          className="absolute inset-0"
          style={{
            background:
              "linear-gradient(180deg, rgba(8,13,19,0.92) 0%, rgba(8,13,19,0.7) 45%, rgba(8,13,19,0.95) 100%)",
          }}
        />
        <PageSection maxWidth="6xl" className="relative py-20 sm:py-24">
          <div className="grid items-center gap-12 lg:grid-cols-[1.05fr_0.95fr]">
            <div>
              <p
                className="mb-5 text-[11px] tracking-[0.28em] uppercase"
                style={{ ...monoStyle, color: AP.amber }}
              >
                {HERO.eyebrow}
              </p>
              <h1 className="text-cc-heading text-hero font-heading mb-6">
                {HERO.title}
              </h1>
              <p className="text-cc-prose lead mb-8">{HERO.teaser}</p>
              <div className="flex flex-wrap gap-3">
                <SolidButton href={HERO.buttons[0].href}>
                  {HERO.buttons[0].label}
                </SolidButton>
                <OutlineButton href={HERO.buttons[1].href}>
                  {HERO.buttons[1].label}
                </OutlineButton>
              </div>
            </div>
            <SplitFlapBoard />
          </div>
        </PageSection>
      </section>

      {SECTIONS.map((section, i) => (
        <StandRow
          key={section.id}
          index={i}
          section={section}
          visual={VISUALS[section.id]}
        />
      ))}

      <PageSection maxWidth="6xl" className="pb-24">
        <div
          id={NITRO_BAND.id}
          className={`${PANEL_CLASS} p-8 sm:p-10`}
          style={panelStyle}
        >
          <div className="grid items-center gap-8 lg:grid-cols-2">
            <div>
              <p
                className="mb-3 text-[10px] tracking-[0.28em]"
                style={{ ...monoStyle, color: AP.amber }}
              >
                TOWER LOG
              </p>
              <h2 className="text-cc-heading text-h4 font-heading mb-4">
                {NITRO_BAND.title}
              </h2>
              <p className="text-cc-ink-dim mb-6 text-base">
                {NITRO_BAND.description}
              </p>
              <div className="flex flex-wrap gap-3">
                <SolidButton href={NITRO_BAND.buttons[0].href}>
                  {NITRO_BAND.buttons[0].label}
                </SolidButton>
                <OutlineButton href={NITRO_BAND.buttons[1].href}>
                  {NITRO_BAND.buttons[1].label}
                </OutlineButton>
              </div>
            </div>
            <div className={PANEL_CLASS} style={panelStyle}>
              <Scene ratio="16 / 9">
                <ApronMeters />
              </Scene>
            </div>
          </div>
        </div>
      </PageSection>
    </div>
  );
}
