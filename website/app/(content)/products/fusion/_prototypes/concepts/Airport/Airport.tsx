import { Fragment } from "react";
import type { ReactElement, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { Link } from "@/src/design-system/Link";

import { CC } from "../../brand";
import { Scene } from "../../Primitives";
import type { CopyLink, CopySection } from "../../copy";
import { HERO, NITRO_BAND, SECTIONS } from "../../copy";
import { ApronMeters } from "./ApronMeters";
import { BoardingCheck } from "./BoardingCheck";
import { ClearanceCheck } from "./ClearanceCheck";
import { FlightPlan } from "./FlightPlan";
import { GateMarkings } from "./GateMarkings";
import { HeroApron } from "./HeroApron";
import { SplitFlapBoard } from "./SplitFlapBoard";

/**
 * Fusion product page, concept v5: Airport.
 *
 * The gateway is the tower, the subgraphs are gates and every client lands on
 * the one runway. The hero is the apron under a split-flap departures board,
 * each text block sits beside the stand that demonstrates it, and the page
 * closes on the on-time meters. All words come from `../../copy`.
 *
 * The airport is built out of the site's own grid, type scale and tokens: the
 * page keeps `bg-cc-bg`, every band is the shared `max-w-6xl` container on the
 * site's vertical rhythm, and the night apron stays inside the bounded scene
 * boxes and the hero scrim.
 */

const PANEL_CLASS = "border-cc-card-border bg-cc-card-bg rounded-xl border";

/** Hero scrim: the page background itself, so the copy sits on the page colour. */
const scrim = (percent: number) =>
  `color-mix(in srgb, ${CC.bg} ${percent}%, transparent)`;

const HERO_SCRIM = `linear-gradient(180deg, ${scrim(92)} 0%, ${scrim(70)} 45%, ${scrim(95)} 100%)`;

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
    <section className="border-cc-card-border border-t">
      <div className="mx-auto max-w-6xl px-5 py-20 sm:px-12 sm:py-28">
        <div
          id={section.id}
          className="grid items-center gap-10 lg:grid-cols-2"
        >
          <div className={flip ? "lg:order-2" : undefined}>
            <Eyebrow color="ink-dim" size="2xs" className="mb-3">
              {`Gate ${STANDS[index] ?? String(index + 1)}`}
            </Eyebrow>
            <h2 className="text-cc-heading font-heading text-h4 sm:text-h3 mb-5">
              {section.title}
            </h2>
            <div className="text-cc-ink text-body space-y-4">
              {section.paragraphs.map((paragraph) => (
                <p key={paragraph}>{withLinks(paragraph, section.links)}</p>
              ))}
              <InPractice links={section.inPractice} />
            </div>
          </div>

          <div
            className={`${PANEL_CLASS} overflow-hidden ${flip ? "lg:order-1" : ""}`.trim()}
          >
            <Scene ratio="16 / 11">{visual}</Scene>
          </div>
        </div>
      </div>
    </section>
  );
}

export function Airport() {
  return (
    <div className="bg-cc-bg">
      <section className="relative flex min-h-[88svh] items-center overflow-hidden">
        <HeroApron />
        <div
          aria-hidden="true"
          className="absolute inset-0"
          style={{ background: HERO_SCRIM }}
        />
        <PageSection maxWidth="7xl" className="relative w-full py-20 sm:py-28">
          <div className="grid items-center gap-12 lg:grid-cols-[1.05fr_0.95fr]">
            <div>
              <Eyebrow color="accent" className="mb-5">
                {HERO.eyebrow}
              </Eyebrow>
              <h1 className="text-cc-heading font-heading text-h2 sm:text-h1 mb-6">
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

      <section className="border-cc-card-border border-t">
        <div className="mx-auto max-w-6xl px-5 py-20 sm:px-12 sm:py-28">
          <div id={NITRO_BAND.id} className={`${PANEL_CLASS} p-8 sm:p-10`}>
            <div className="grid items-center gap-8 lg:grid-cols-2">
              <div>
                <Eyebrow color="ink-dim" size="2xs" className="mb-3">
                  Tower log
                </Eyebrow>
                <h2 className="text-cc-heading font-heading text-h4 mb-4">
                  {NITRO_BAND.title}
                </h2>
                <p className="text-cc-ink text-body mb-6">
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
              <div className="border-cc-card-border overflow-hidden rounded-lg border">
                <Scene ratio="16 / 9">
                  <ApronMeters />
                </Scene>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
