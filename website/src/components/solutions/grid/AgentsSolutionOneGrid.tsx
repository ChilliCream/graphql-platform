"use client";

import React, { FC, ReactNode } from "react";
import styled from "styled-components";

import { AccentThread } from "@/components/redesign-system/AccentThread";
import { GridButton, GRID_TOKENS } from "@/components/redesign-system/grid";
import { FEATURE_CARDS, LOGOS } from "@/data/solutions/shared";
import { findRelatedSolutions } from "@/data/solutions/solutions";
import type { SolutionRecord } from "@/data/solutions/types";

import { ConceptDiagramBody } from "../ConceptDiagram";
import { HeroMotif } from "../HeroMotif";
import { PillarIcon } from "../PillarIcon";

export interface AgentsSolutionOneGridProps {
  readonly record: SolutionRecord;
}

// Single-grid composition for /solutions/agents?v=grid. The whole page is
// ONE 12-column CSS grid lattice instead of 11 stacked GridSection bands.
// Adjacent cells share a single 1px hairline, achieved by the outer frame
// providing the top + left edges and each cell providing its own bottom +
// right edges. Padding-collapsing across cells is what makes the page read
// as one continuous wall, not a series of bands.
//
// This is a proof-of-concept tied to the agents slug only. The other six
// slugs continue to render through SolutionPageRendererGrid. No shared
// abstraction is extracted yet, by design.
//
// Cell layout (12-col x ~9-row, deviating slightly from the proposal so
// each row reads at a deliberate height):
//
//   Row 1 (480px):  hero text 1..7  |  hero motif 8..12
//   Row 2 (220px):  stat 1..3 | stat 4..6 | stat 7..9 | stat 10..12
//   Row 3 (300px):  pillar 1..3 | pillar 4..6 | pillar 7..9 | pillar 10..12
//   Row 4 (560px):  diagram 1..8 (inverted) | code 9..12
//   Row 5 (300px):  testimonial 1..8 | playbook 9..12
//   Row 6 (200px):  feat 1..2 | feat 3..4 | feat 5..6 | feat 7..8 | feat 9..10 | feat 11..12
//   Row 7 (220px):  logo wall 1..12 (4-up internal sub-grid)
//   Row 8 (260px):  related 1..4 | related 5..8 | related 9..12
//   Row 9 (380px):  final cta 1..12
export const AgentsSolutionOneGrid: FC<AgentsSolutionOneGridProps> = ({
  record,
}) => {
  const related = findRelatedSolutions(record);
  const features = record.featureCards
    .map((id) => FEATURE_CARDS[id])
    .filter((c): c is NonNullable<typeof c> => c !== undefined)
    .slice(0, 6);
  const logos = record.logos
    .map((id) => LOGOS[id])
    .filter((l): l is NonNullable<typeof l> => l !== undefined)
    .slice(0, 4);

  const { hero, proofMetrics, pillars, codeSnippet, testimonials, finalCta } =
    record;
  const testimonial = testimonials[0];

  return (
    <AccentThread page="solutions" override={record.accent}>
      <Page>
        <Frame>
          {/* Row 1: hero */}
          <Cell $col="1 / span 7" $row="1" $padding="hero">
            <HeroCopy>
              <span className="cc-grid-eyebrow">{hero.eyebrow}</span>
              <h1 className="cc-grid-h1">{hero.headline}</h1>
              <p className="cc-grid-lede">{hero.sub}</p>
              <CtaRow>
                <GridButton variant="primary" href={hero.primaryCta.href}>
                  {hero.primaryCta.label}
                </GridButton>
                <GridButton variant="secondary" href={hero.secondaryCta.href}>
                  {hero.secondaryCta.label}
                </GridButton>
              </CtaRow>
            </HeroCopy>
          </Cell>
          <Cell $col="8 / span 5" $row="1" $padding="none">
            <MotifSlot aria-hidden>
              {record.heroMotif ? (
                <HeroMotif kind={record.heroMotif} slug={record.slug} />
              ) : null}
            </MotifSlot>
          </Cell>

          {/* Row 2: 4-up proof strip */}
          {proofMetrics.slice(0, 4).map((metric, i) => (
            <Cell
              key={`stat-${i}`}
              $col={`${1 + i * 3} / span 3`}
              $row="2"
              $padding="cell"
            >
              <Stat>
                <StatCaption>{metric.customer.toUpperCase()}</StatCaption>
                <StatValue>{metric.value}</StatValue>
                <StatLabel>{metric.outcome}</StatLabel>
              </Stat>
            </Cell>
          ))}

          {/* Row 3: 4-up pillars */}
          {pillars.items.slice(0, 4).map((p, i) => (
            <Cell
              key={`pillar-${i}`}
              $col={`${1 + i * 3} / span 3`}
              $row="3"
              $padding="cell"
            >
              <Pillar>
                <PillarIconSlot aria-hidden>
                  <PillarIcon kind={p.icon} size={22} />
                </PillarIconSlot>
                <h3 className="cc-grid-h3">{p.title}</h3>
                <p className="cc-grid-body">{p.body}</p>
              </Pillar>
            </Cell>
          ))}

          {/* Row 4: diagram (inverted, full-bleed) + code */}
          <Cell $col="1 / span 8" $row="4" $padding="none" $variant="inverted">
            <DiagramCanvas>
              <DiagramHead>
                <span className="cc-grid-eyebrow">The shape</span>
                <h2 className="cc-grid-h2">
                  An API platform agents can drive.
                </h2>
              </DiagramHead>
              <DiagramSvgSlot>
                <ConceptDiagramBody kind={record.diagram} />
              </DiagramSvgSlot>
            </DiagramCanvas>
          </Cell>
          {codeSnippet ? (
            <Cell $col="9 / span 4" $row="4" $padding="none">
              <CodeFrame>
                <CodeHead>
                  <CodeFileGroup>
                    <CodeDots aria-hidden>
                      <span />
                      <span />
                      <span />
                    </CodeDots>
                    <CodeFile>{codeSnippet.fileName}</CodeFile>
                  </CodeFileGroup>
                  <CodeLang>{codeSnippet.language}</CodeLang>
                </CodeHead>
                <CodeBody>
                  <code>{codeSnippet.source.replace(/^\n/, "")}</code>
                </CodeBody>
              </CodeFrame>
            </Cell>
          ) : (
            <Cell $col="9 / span 4" $row="4" $padding="cell" />
          )}

          {/* Row 5: testimonial + playbook */}
          {testimonial ? (
            <Cell $col="1 / span 8" $row="5" $padding="cell">
              <Quote>
                <QuoteMark aria-hidden>&ldquo;</QuoteMark>
                <QuoteBody>{testimonial.quote}</QuoteBody>
                <Attribution>
                  <strong>{testimonial.title}</strong>
                  <Sep>·</Sep>
                  <span>{testimonial.company}</span>
                </Attribution>
              </Quote>
            </Cell>
          ) : (
            <Cell $col="1 / span 8" $row="5" $padding="cell" />
          )}
          {record.collateral ? (
            <Cell $col="9 / span 4" $row="5" $padding="cell">
              <Playbook>
                <span className="cc-grid-eyebrow">Workshop · 90-min</span>
                <PlaybookTitle>{record.collateral.title}</PlaybookTitle>
                <PlaybookCta>
                  <GridButton variant="primary" href={record.collateral.href}>
                    Reserve a slot
                  </GridButton>
                </PlaybookCta>
              </Playbook>
            </Cell>
          ) : (
            <Cell $col="9 / span 4" $row="5" $padding="cell" />
          )}

          {/* Row 6: 6-up foundations */}
          {features.map((c, i) => (
            <Cell
              key={`feat-${c.id}`}
              $col={`${1 + i * 2} / span 2`}
              $row="6"
              $padding="cell"
            >
              <Feature>
                <FeatureIcon aria-hidden>
                  <PillarIcon kind={c.icon} size={20} />
                </FeatureIcon>
                <FeatureLabel>{c.title}</FeatureLabel>
              </Feature>
            </Cell>
          ))}

          {/* Row 7: logo wall (single cell with 4-up sub-grid). Eyebrow caption
            sits above the row of names so each name has the full tile to
            breathe; thin hairline dividers separate the names. */}
          <Cell $col="1 / span 12" $row="7" $padding="none">
            <LogoCaption>{record.logoCaption}</LogoCaption>
            <LogoWall>
              {logos.map((l) => (
                <LogoTile key={l.id}>
                  {l.named ? (
                    <Wordmark>{l.label}</Wordmark>
                  ) : (
                    <Descriptor>{l.label}</Descriptor>
                  )}
                </LogoTile>
              ))}
            </LogoWall>
          </Cell>

          {/* Row 8: related */}
          {related.slice(0, 3).map((s, i) => (
            <Cell
              key={`rel-${s.slug}`}
              $col={`${1 + i * 4} / span 4`}
              $row="8"
              $padding="cell"
              $as="a"
              $href={`/solutions/${s.slug}/?v=grid`}
              $hoverable
            >
              <Related>
                <RelatedEyebrow>
                  {s.category === "industry" ? "Industry" : "Use case"}
                </RelatedEyebrow>
                <h3 className="cc-grid-h3">{s.title}</h3>
                <RelatedBody>{s.hero.sub}</RelatedBody>
                <RelatedArrow aria-hidden>
                  <span>Explore</span>
                  <ArrowGlyph>&rarr;</ArrowGlyph>
                </RelatedArrow>
              </Related>
            </Cell>
          ))}

          {/* Row 9: final CTA */}
          <Cell $col="1 / span 12" $row="9" $padding="hero">
            <FinalCta>
              <span className="cc-grid-eyebrow">Pick your way in</span>
              <FinalHeadline>{finalCta.headline}</FinalHeadline>
              <FinalSub>{finalCta.sub}</FinalSub>
              <FinalActions>
                <GridButton variant="primary" href={finalCta.primary.href}>
                  {finalCta.primary.label}
                </GridButton>
                <FinalGhost href={finalCta.secondary.href}>
                  {finalCta.secondary.label} →
                </FinalGhost>
              </FinalActions>
            </FinalCta>
          </Cell>
        </Frame>
      </Page>
    </AccentThread>
  );
};

// ===== Page chrome =====
// Outer page surface owns the design tokens (matches SolutionsGridRoot but
// inlined here so the agents one-grid is fully self-contained for this PoC).

const Page = styled.div`
  --cc-grid-bg: ${GRID_TOKENS.bgBase};
  --cc-grid-card-bg: ${GRID_TOKENS.bgCard};
  --cc-grid-card-bg-inverted: ${GRID_TOKENS.bgInverted};
  --cc-grid-card-hover: ${GRID_TOKENS.bgHover};
  --cc-grid-hairline: ${GRID_TOKENS.hairline};
  --cc-grid-hairline-strong: ${GRID_TOKENS.hairlineStrong};
  --cc-grid-ink: ${GRID_TOKENS.inkPrimary};
  --cc-grid-ink-body: ${GRID_TOKENS.inkBody};
  --cc-grid-ink-muted: ${GRID_TOKENS.inkMuted};
  --cc-grid-ink-faint: ${GRID_TOKENS.inkFaint};

  --cc-ink: ${GRID_TOKENS.inkPrimary};
  --cc-ink-dim: ${GRID_TOKENS.inkBody};
  --cc-ink-faint: ${GRID_TOKENS.inkFaint};

  /* ConceptDiagram body relies on these legacy color tokens for the
     per-language tints in the polyglot/agents diagrams. */
  --cc-col-cat: oklch(0.74 0.18 30);
  --cc-col-bil: oklch(0.82 0.16 90);
  --cc-col-ord: oklch(0.76 0.16 150);
  --cc-col-shi: oklch(0.74 0.14 220);
  --cc-col-usr: oklch(0.72 0.18 310);
  --cc-col-tel: oklch(0.74 0.14 200);
  --cc-amber: oklch(0.85 0.16 75);

  position: relative;
  width: 100%;
  background: ${GRID_TOKENS.bgBase};
  color: ${GRID_TOKENS.inkPrimary};
  font-family: var(--cc-font-sans), system-ui, sans-serif;
  padding: clamp(40px, 6vw, 80px) clamp(24px, 5vw, 64px);

  * {
    box-sizing: border-box;
  }

  .cc-grid-eyebrow {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
    margin: 0;
  }

  .cc-grid-h1 {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 600;
    letter-spacing: -0.045em;
    line-height: 0.98;
    font-size: clamp(44px, 6vw, 80px);
    margin: 0;
    color: ${GRID_TOKENS.inkPrimary};
    text-wrap: balance;
  }

  .cc-grid-h2 {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 600;
    letter-spacing: -0.03em;
    line-height: 1.04;
    font-size: clamp(28px, 3.5vw, 44px);
    margin: 0;
    color: ${GRID_TOKENS.inkPrimary};
    text-wrap: balance;
  }

  .cc-grid-h3 {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 600;
    letter-spacing: -0.02em;
    line-height: 1.2;
    font-size: 19px;
    margin: 0;
    color: ${GRID_TOKENS.inkPrimary};
  }

  .cc-grid-lede {
    font-size: clamp(15px, 1.2vw, 18px);
    line-height: 1.6;
    color: ${GRID_TOKENS.inkBody};
    margin: 0;
    text-wrap: pretty;
  }

  .cc-grid-body {
    font-size: 14px;
    line-height: 1.55;
    color: ${GRID_TOKENS.inkBody};
    margin: 0;
    text-wrap: pretty;
  }
`;

// Outer frame: 12-col grid, gap 0. The frame paints the top + left
// hairlines; each cell paints its own bottom + right. That collapses
// adjacent borders into a single 1px line and avoids the doubled-border
// look that GridSection stacking produces.
const Frame = styled.div`
  display: grid;
  grid-template-columns: repeat(12, 1fr);
  grid-template-rows:
    minmax(540px, 580px)
    minmax(220px, 240px)
    minmax(260px, 280px)
    minmax(480px, 520px)
    minmax(280px, 300px)
    minmax(160px, 180px)
    minmax(220px, 240px)
    minmax(240px, 260px)
    360px;
  gap: 0;
  width: 100%;
  max-width: 1440px;
  margin: 0 auto;
  border-top: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  border-left: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});

  @media (max-width: 720px) {
    grid-template-columns: 1fr;
    grid-template-rows: none;
    border-left: 0;
  }
`;

// Cell: takes col/row spans plus a padding intent + variant. Borders on
// bottom + right collapse against the frame's top + left to produce a
// single shared hairline between every adjacent pair.
type CellPadding = "none" | "cell" | "hero";

interface CellOuterProps {
  $col: string;
  $row: string;
  $padding: CellPadding;
  $variant?: "default" | "inverted";
  $hoverable?: boolean;
}

const cellPaddingValue = (p: CellPadding) => {
  switch (p) {
    case "none":
      return "0";
    case "hero":
      return "clamp(40px, 4.5vw, 64px)";
    case "cell":
    default:
      return "clamp(24px, 2.6vw, 36px)";
  }
};

const CellOuter = styled.div<CellOuterProps>`
  grid-column: ${({ $col }) => $col};
  grid-row: ${({ $row }) => $row};
  position: relative;
  display: flex;
  flex-direction: column;
  background: ${({ $variant }) =>
    $variant === "inverted"
      ? `var(--cc-grid-card-bg-inverted, ${GRID_TOKENS.bgInverted})`
      : `var(--cc-grid-card-bg, ${GRID_TOKENS.bgCard})`};
  color: ${({ $variant }) =>
    $variant === "inverted" ? "#ffffff" : GRID_TOKENS.inkPrimary};
  border-right: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  border-bottom: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  border-radius: 0;
  padding: ${({ $padding }) => cellPaddingValue($padding)};
  text-decoration: none;
  transition: background 0.12s ease;

  ${({ $hoverable }) =>
    $hoverable
      ? `
        &:hover,
        &:focus-visible {
          background: var(--cc-grid-card-hover, ${GRID_TOKENS.bgHover});
          cursor: pointer;
        }
      `
      : ""}

  @media (max-width: 720px) {
    grid-column: 1 / -1 !important;
    grid-row: auto !important;
    border-right: 0;
  }
`;

interface CellProps {
  $col: string;
  $row: string;
  $padding: CellPadding;
  $variant?: "default" | "inverted";
  $hoverable?: boolean;
  $as?: "a" | "div";
  $href?: string;
  children?: ReactNode;
}

const Cell: FC<CellProps> = ({ $as, $href, children, ...rest }) => {
  if ($as === "a" && $href) {
    return (
      <CellOuter as="a" href={$href} {...rest}>
        {children}
      </CellOuter>
    );
  }
  return <CellOuter {...rest}>{children}</CellOuter>;
};

// ===== Hero =====

const HeroCopy = styled.div`
  display: flex;
  flex-direction: column;
  gap: 24px;
  max-width: 640px;
  height: 100%;
  justify-content: center;
  padding-top: 0;
`;

const CtaRow = styled.div`
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
  margin-top: 16px;
`;

const MotifSlot = styled.div`
  position: relative;
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: ${GRID_TOKENS.inkPrimary};
  padding: clamp(16px, 2vw, 28px);
  overflow: hidden;

  /* Accent halo behind the motif: a soft radial that lifts the illustration
     off the dark cell so the orbit / ray-burst doesn't read as floating. */
  &::before {
    content: "";
    position: absolute;
    inset: 0;
    background: radial-gradient(
      ellipse at center,
      var(--cc-accent, ${GRID_TOKENS.inkPrimary}) 0%,
      transparent 65%
    );
    opacity: 0.18;
    pointer-events: none;
  }

  > svg {
    position: relative;
    width: 100%;
    height: 100%;
    max-height: 480px;
    max-width: 100%;
    display: block;
  }
`;

// ===== Stat =====

const Stat = styled.div`
  display: flex;
  flex-direction: column;
  gap: 14px;
  height: 100%;
  justify-content: space-between;
`;

const StatCaption = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 12px;
  font-weight: 500;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkFaint};
`;

const StatValue = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(48px, 5.5vw, 80px);
  font-weight: 600;
  letter-spacing: -0.04em;
  line-height: 0.95;
  color: ${GRID_TOKENS.inkPrimary};
  font-feature-settings: "tnum" 1, "ss01" 1;
`;

const StatLabel = styled.span`
  font-size: 13px;
  line-height: 1.4;
  color: ${GRID_TOKENS.inkBody};
  text-wrap: pretty;
  max-width: 32ch;
`;

// ===== Pillar =====

const Pillar = styled.article`
  display: flex;
  flex-direction: column;
  gap: 12px;
  height: 100%;

  .cc-grid-h3 {
    text-wrap: balance;
  }

  .cc-grid-body {
    max-width: 32ch;
  }
`;

const PillarIconSlot = styled.div`
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  color: ${GRID_TOKENS.inkPrimary};
  margin-bottom: 6px;
`;

// ===== Diagram =====

const DiagramCanvas = styled.div`
  display: flex;
  flex-direction: column;
  gap: 32px;
  width: 100%;
  height: 100%;
  padding: clamp(32px, 3.6vw, 56px);
  color: ${GRID_TOKENS.inkPrimary};
  overflow: hidden;
`;

const DiagramHead = styled.div`
  display: flex;
  flex-direction: column;
  gap: 10px;

  .cc-grid-h2 {
    color: ${GRID_TOKENS.inkPrimary};
    max-width: 18ch;
  }
`;

const DiagramSvgSlot = styled.div`
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  min-width: 0;
  min-height: 0;
  overflow: hidden;

  svg {
    width: 100%;
    height: auto;
    max-width: 100%;
    max-height: 100%;
    display: block;
    /* preserveAspectRatio is on the inline SVG (xMidYMid meet); the box
       constraints above keep it inside the cell on every breakpoint. */
  }
`;

// ===== Code snippet =====

const CodeFrame = styled.div`
  display: flex;
  flex-direction: column;
  width: 100%;
  height: 100%;
  overflow: hidden;
  background: ${GRID_TOKENS.bgInverted};
`;

const CodeHead = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 12px 18px;
  border-bottom: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
`;

const CodeFileGroup = styled.div`
  display: inline-flex;
  align-items: center;
  gap: 12px;
`;

const CodeDots = styled.div`
  display: inline-flex;
  align-items: center;
  gap: 6px;

  span {
    width: 9px;
    height: 9px;
    border-radius: 50%;
    display: inline-block;
    opacity: 0.7;
  }

  span:nth-child(1) {
    background: #ff5f56;
  }

  span:nth-child(2) {
    background: #ffbd2e;
  }

  span:nth-child(3) {
    background: #27c93f;
  }
`;

const CodeFile = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11.5px;
  color: ${GRID_TOKENS.inkBody};
  letter-spacing: 0.02em;
`;

const CodeLang = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 9.5px;
  font-weight: 500;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
  padding: 3px 8px;
  border: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
`;

const CodeBody = styled.pre`
  margin: 0;
  padding: 20px 22px;
  flex: 1;
  overflow: hidden;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11.5px;
  line-height: 1.7;
  color: ${GRID_TOKENS.inkBody};
  white-space: pre;
  text-overflow: clip;
  font-feature-settings: "calt" 0;

  code {
    color: ${GRID_TOKENS.inkPrimary};
  }
`;

// ===== Testimonial =====

const Quote = styled.figure`
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 28px;
  height: 100%;
  justify-content: center;
  position: relative;
  max-width: 60ch;
`;

const QuoteMark = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 80px;
  line-height: 0.8;
  color: ${GRID_TOKENS.inkFaint};
  font-weight: 500;
  margin-bottom: -12px;
  display: block;
  user-select: none;
`;

const QuoteBody = styled.blockquote`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(22px, 1.95vw, 28px);
  font-weight: 500;
  line-height: 1.32;
  letter-spacing: -0.018em;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
  text-wrap: balance;
`;

const Attribution = styled.figcaption`
  display: inline-flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 10.5px;
  font-weight: 500;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};

  strong {
    color: ${GRID_TOKENS.inkPrimary};
    font-weight: 500;
  }
`;

const Sep = styled.span`
  color: ${GRID_TOKENS.inkFaint};
`;

// ===== Playbook =====

const Playbook = styled.div`
  display: flex;
  flex-direction: column;
  gap: 16px;
  height: 100%;
`;

const PlaybookTitle = styled.h3`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(20px, 2vw, 26px);
  font-weight: 600;
  letter-spacing: -0.025em;
  line-height: 1.18;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
  flex: 1;
  text-wrap: balance;
`;

const PlaybookCta = styled.div`
  margin-top: auto;
`;

// ===== Foundations =====

const Feature = styled.div`
  display: flex;
  flex-direction: column;
  gap: 14px;
  align-items: flex-start;
  height: 100%;
  justify-content: center;
`;

const FeatureIcon = styled.div`
  width: 22px;
  height: 22px;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  color: ${GRID_TOKENS.inkBody};
`;

const FeatureLabel = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 13px;
  font-weight: 500;
  line-height: 1.35;
  color: ${GRID_TOKENS.inkPrimary};
  letter-spacing: -0.005em;
  text-wrap: balance;
`;

// ===== Logo wall =====

const LogoWall = styled.div`
  flex: 1;
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0;
  width: 100%;

  @media (max-width: 720px) {
    grid-template-columns: 1fr 1fr;
  }
`;

const LogoTile = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: clamp(20px, 2vw, 28px);
  min-height: 120px;
  position: relative;

  /* Thin vertical hairline divider between names. The frame already paints
     the cell's outer borders, so the divider only renders between adjacent
     tiles. */
  & + &::before {
    content: "";
    position: absolute;
    left: 0;
    top: 18%;
    bottom: 18%;
    width: 1px;
    background: var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  }

  @media (max-width: 720px) {
    & + &::before {
      display: none;
    }
    &:nth-child(odd) + & {
      border-left: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
    }
    &:nth-child(n + 3) {
      border-top: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
    }
  }
`;

const Wordmark = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(20px, 2vw, 28px);
  font-weight: 600;
  letter-spacing: -0.02em;
  color: ${GRID_TOKENS.inkPrimary};
`;

const Descriptor = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(20px, 2vw, 28px);
  font-weight: 600;
  letter-spacing: -0.02em;
  color: ${GRID_TOKENS.inkPrimary};
  line-height: 1.15;
  text-wrap: balance;
`;

const LogoCaption = styled.p`
  text-align: center;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
  margin: 0;
  padding: 14px 0;
  border-bottom: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
`;

// ===== Related =====

const Related = styled.div`
  display: flex;
  flex-direction: column;
  gap: 10px;
  height: 100%;
`;

const RelatedEyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
`;

const RelatedBody = styled.p`
  font-size: 13.5px;
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  margin: 0;
  flex: 1;
  text-wrap: pretty;
`;

const RelatedArrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  font-weight: 500;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  margin-top: auto;
  display: inline-flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding-top: 18px;
  border-top: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  transition: border-color 0.18s ease;

  ${CellOuter}:hover & {
    border-color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  }
`;

const ArrowGlyph = styled.span`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  font-size: 18px;
  letter-spacing: 0;
  color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  transition: transform 0.18s ease;

  ${CellOuter}:hover & {
    transform: translateX(4px);
  }
`;

// ===== Final CTA =====

const FinalCta = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 20px;
  height: 100%;
  max-width: 760px;
  margin: 0 auto;
  text-align: center;
`;

const FinalHeadline = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-weight: 600;
  letter-spacing: -0.04em;
  line-height: 1.02;
  font-size: clamp(32px, 4.2vw, 56px);
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
  max-width: 22ch;
  text-wrap: balance;
`;

const FinalSub = styled.p`
  font-size: clamp(15px, 1.2vw, 18px);
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  margin: 0;
  max-width: 60ch;
  text-wrap: pretty;
`;

const FinalActions = styled.div`
  margin-top: 18px;
  display: flex;
  align-items: center;
  gap: 28px;
  flex-wrap: wrap;
  justify-content: center;

  > * {
    flex-shrink: 0;
  }
`;

const FinalGhost = styled.a`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 14px;
  font-weight: 500;
  color: ${GRID_TOKENS.inkBody};
  text-decoration: none;
  border-bottom: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  padding-bottom: 3px;
  transition: border-color 0.12s ease, color 0.12s ease;

  &:hover,
  &:focus-visible {
    color: ${GRID_TOKENS.inkPrimary};
    border-color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  }
`;
