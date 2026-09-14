import { Fragment } from "react";
import type { ReactElement, ReactNode } from "react";

import { ButtonRow } from "@/src/components/ButtonRow";
import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { Link } from "@/src/design-system/Link";

import { HERO, NITRO_BAND, SECTIONS } from "../../copy";
import type { CopyLink } from "../../copy";
import { CallLog } from "./CallLog";
import { HeroBoard } from "./HeroBoard";
import { LineCards } from "./LineCards";
import { PatchThrough } from "./PatchThrough";
import { PlugStandards } from "./PlugStandards";
import { WiringCheck } from "./WiringCheck";
import { BAND_WASH, EDGE_SOFT } from "./palette";

/**
 * Concept v3 "Switchboard": the Fusion page as an operator's telephone
 * exchange. Client lines call in on the left, one board patches each call into
 * the service lines that answer, and every claim in the copy is shown on the
 * board itself - two plug standards in one jack strip, line cards in any
 * language, the wiring check that stops a bad build, and the call log matched
 * against a rewiring.
 *
 * The copy is imported verbatim from `_prototypes/copy.ts`; only the layout
 * and the visuals belong to this concept.
 */

const [WHAT_IS, BOTH_SPECS, ANY_SERVER, CLIENT_SAFETY] = SECTIONS;

/** Renders a paragraph, linking any of the section's link labels it contains. */
function withLinks(text: string, links: readonly CopyLink[]): ReactNode {
  let parts: (string | ReactElement)[] = [text];

  for (const link of links) {
    parts = parts.flatMap((part): (string | ReactElement)[] => {
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

interface ProseProps {
  readonly children: ReactNode;
}

function Prose({ children }: ProseProps) {
  return <div className="text-cc-ink-dim text-body space-y-4">{children}</div>;
}

interface InPracticeProps {
  readonly links: readonly CopyLink[];
}

function InPractice({ links }: InPracticeProps) {
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

interface RowProps {
  readonly copy: ReactNode;
  readonly visual: ReactNode;
  /** Puts the visual first on wide screens, so the rows alternate. */
  readonly reverse?: boolean;
  readonly className?: string;
}

function Row({ copy, visual, reverse, className }: RowProps) {
  return (
    <div
      className={`grid grid-cols-1 items-center gap-10 lg:grid-cols-12 lg:gap-14 ${className ?? ""}`.trim()}
    >
      <div className={`lg:col-span-5 ${reverse ? "lg:order-2" : ""}`.trim()}>
        {copy}
      </div>
      <div className={`lg:col-span-7 ${reverse ? "lg:order-1" : ""}`.trim()}>
        {visual}
      </div>
    </div>
  );
}

interface PositionProps {
  readonly id: string;
  /** The engraved position number on the board, e.g. "01". */
  readonly position: string;
  readonly title: string;
  readonly children: ReactNode;
}

function Position({ id, position, title, children }: PositionProps) {
  return (
    <section id={id} className="border-cc-card-border border-t">
      <PageSection maxWidth="6xl" className="py-20 sm:py-28">
        <div className="flex items-center gap-4">
          <Eyebrow as="span" size="2xs">
            {`POSITION ${position}`}
          </Eyebrow>
          <span
            aria-hidden="true"
            className="h-px flex-1"
            style={{ background: EDGE_SOFT }}
          />
        </div>
        <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-5 max-w-3xl text-balance">
          {title}
        </h2>
        <div className="mt-12">{children}</div>
      </PageSection>
    </section>
  );
}

export function Switchboard() {
  return (
    <>
      <section className="relative overflow-hidden">
        <PageSection
          maxWidth="7xl"
          className="flex min-h-[88svh] items-center py-20 sm:py-28"
        >
          <div className="grid w-full grid-cols-1 items-center gap-12 lg:grid-cols-2 lg:gap-16">
            <div>
              <Eyebrow size="2xs">{HERO.eyebrow}</Eyebrow>
              <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-6 text-balance">
                {HERO.title}
              </h1>
              <p className="text-cc-ink text-body sm:text-lead mt-6 max-w-xl">
                {HERO.teaser}
              </p>
              <ButtonRow align="start" className="mt-9">
                <SolidButton href={HERO.buttons[0].href}>
                  {HERO.buttons[0].label}
                </SolidButton>
                <OutlineButton href={HERO.buttons[1].href}>
                  {HERO.buttons[1].label}
                </OutlineButton>
              </ButtonRow>
            </div>
            <HeroBoard />
          </div>
        </PageSection>
      </section>

      <Position id={WHAT_IS.id} position="01" title={WHAT_IS.title}>
        <Row
          copy={
            <Prose>
              <p>{withLinks(WHAT_IS.paragraphs[0], WHAT_IS.links)}</p>
              <p>{withLinks(WHAT_IS.paragraphs[1], WHAT_IS.links)}</p>
            </Prose>
          }
          visual={<PatchThrough />}
        />
      </Position>

      <Position id={BOTH_SPECS.id} position="02" title={BOTH_SPECS.title}>
        <Row
          reverse
          copy={
            <Prose>
              <p>{withLinks(BOTH_SPECS.paragraphs[0], BOTH_SPECS.links)}</p>
              <p>{withLinks(BOTH_SPECS.paragraphs[1], BOTH_SPECS.links)}</p>
              <InPractice links={BOTH_SPECS.inPractice} />
            </Prose>
          }
          visual={<PlugStandards />}
        />
      </Position>

      <Position id={ANY_SERVER.id} position="03" title={ANY_SERVER.title}>
        <Row
          copy={
            <Prose>
              <p>{withLinks(ANY_SERVER.paragraphs[0], ANY_SERVER.links)}</p>
            </Prose>
          }
          visual={<LineCards />}
        />
        <Row
          reverse
          className="mt-20"
          copy={
            <Prose>
              <p>{withLinks(ANY_SERVER.paragraphs[1], ANY_SERVER.links)}</p>
              <InPractice links={ANY_SERVER.inPractice} />
            </Prose>
          }
          visual={<WiringCheck />}
        />
      </Position>

      <Position id={CLIENT_SAFETY.id} position="04" title={CLIENT_SAFETY.title}>
        <Row
          copy={
            <Prose>
              <p>
                {withLinks(CLIENT_SAFETY.paragraphs[0], CLIENT_SAFETY.links)}
              </p>
              <p>
                {withLinks(CLIENT_SAFETY.paragraphs[1], CLIENT_SAFETY.links)}
              </p>
              <InPractice links={CLIENT_SAFETY.inPractice} />
            </Prose>
          }
          visual={<CallLog />}
        />
      </Position>

      <section id={NITRO_BAND.id} className="border-cc-card-border border-t">
        <PageSection maxWidth="6xl" className="py-20 sm:py-28">
          <div
            className="rounded-2xl px-6 py-12 sm:px-12"
            style={{
              border: `1px solid ${EDGE_SOFT}`,
              background: BAND_WASH,
            }}
          >
            <Eyebrow size="2xs">THE CALL LOG</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-5 max-w-3xl text-balance">
              {NITRO_BAND.title}
            </h2>
            <p className="text-cc-ink-dim text-body mt-5 max-w-2xl">
              {NITRO_BAND.description}
            </p>
            <ButtonRow align="start" className="mt-8">
              <SolidButton href={NITRO_BAND.buttons[0].href}>
                {NITRO_BAND.buttons[0].label}
              </SolidButton>
              <OutlineButton href={NITRO_BAND.buttons[1].href}>
                {NITRO_BAND.buttons[1].label}
              </OutlineButton>
            </ButtonRow>
          </div>
        </PageSection>
      </section>
    </>
  );
}
