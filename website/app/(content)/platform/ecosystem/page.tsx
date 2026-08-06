import Link from "next/link";
import type { CSSProperties, ReactNode } from "react";

import { ButtonRow } from "@/src/components/ButtonRow";
import { FromOurBlog } from "@/src/components/FromOurBlog";
import { NextStepsSection } from "@/src/components/NextStepsSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { SectionHeading } from "@/src/components/SectionHeading";
import {
  GITHUB_REPO_URL,
  GITHUB_STARGAZERS_URL,
  TOOLS,
} from "@/src/components/header/navData";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Card } from "@/src/design-system/Card";
import { getGitHubCommitActivity } from "@/src/helpers/githubCommitActivity";
import type { GitHubContributor } from "@/src/helpers/githubContributors";
import { getGitHubContributors } from "@/src/helpers/githubContributors";
import { getGitHubStarCount } from "@/src/helpers/githubStars";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { BlogIcon } from "@/src/icons/Blog";
import { GitHubIcon } from "@/src/icons/GitHub";
import { SlackIcon } from "@/src/icons/Slack";
import { YouTubeIcon } from "@/src/icons/YouTube";

export const metadata = pageMetadata({
  title: ".NET GraphQL Ecosystem",
  description:
    "Explore ChilliCream's open-source .NET GraphQL platform, public development, standards participation, and community channels.",
  path: "/platform/ecosystem",
});

/**
 * Color rationing: everything on this page uses cc-* theme tokens. The raw
 * color values on this page are the orbit ring stroke (a hairline cream
 * between the border token steps) and the commit heatmap scale next to its
 * component (GitHub's green ramp, kept verbatim so the graph reads as
 * GitHub's); the node edge mask's black stops are mask alpha, not paint.
 */
const RING_STROKE = "rgba(245,240,234,0.17)";

/**
 * Hero orbit geometry: a nominal 1600px box scaled by --u (0.62 / 0.78 / 1).
 * Invariant: the copy stack sits INSIDE ring A, so rings never cross text
 * and render at constant stroke with no radial fade. Both artwork layers are
 * unclipped full-bleed wrappers: the box can run past the section's own
 * bounds and paint over whatever follows the hero, which is accepted. The
 * nodes layer alone carries the edge mask below so chips near the top and
 * bottom dissolve instead of stopping abruptly.
 */
const NODE_EDGE_MASK =
  "linear-gradient(to bottom, transparent 0, black 64px, black calc(100% - 56px), transparent 100%)";

/**
 * The scene is a still composition, like the reference card: every node holds
 * its authored angle, so chips and lanes keep the alignment they were
 * choreographed for. The only motion is a one-time entrance in which each
 * node fades and settles into place, staggered by --d. With reduced motion the
 * media query never matches and nodes simply render in place.
 */
const ENTRANCE_STYLES = `
@media (prefers-reduced-motion: no-preference) {
  .eco-enter { animation: eco-enter 0.6s ease-out var(--d) both; }
  @keyframes eco-enter {
    from { opacity: 0; transform: translate(-50%, -50%) scale(0.85); }
    to { opacity: 1; transform: translate(-50%, -50%) scale(1); }
  }
}
`;

/** House focus ring (design-system Dropdown idiom) for whole-card links. */
const CARD_FOCUS_CLASSES =
  "focus-visible:border-cc-accent focus-visible:ring-cc-accent/30 focus-visible:ring-2 focus-visible:outline-hidden";

/**
 * Shared shell for the counter pills (stars, license). pt-[2px] is a measured
 * optical correction: at this size the mono face's ink sits about 1.5px above
 * true center (its ascent reserves room that caps and digits never use), and
 * flex centering follows the line box, not the ink.
 */
const PILL_CLASSES =
  "border-cc-card-border bg-cc-surface text-cc-ink-dim flex h-7 items-center gap-1.5 rounded-full border px-3 pt-[2px] font-mono text-[0.6rem] tracking-[0.14em] whitespace-nowrap uppercase";

/**
 * Hero orbit nodes are real links: they punch through the layer's
 * pointer-events-none, pick up the house hover and focus states, and open
 * community destinations.
 */
const ORBIT_LINK_CLASSES = `hover:border-cc-card-border-hover pointer-events-auto no-underline transition-colors ${CARD_FOCUS_CLASSES}`;

interface OrbitRingSpec {
  /** Ring radius in the nominal 1600px box. */
  readonly r: number;
  readonly strokeOpacity: number;
}

const ORBIT_RINGS: readonly OrbitRingSpec[] = [
  { r: 440, strokeOpacity: 1 },
  { r: 560, strokeOpacity: 1 },
  { r: 680, strokeOpacity: 0.9 },
  { r: 800, strokeOpacity: 0.85 },
];

interface ConnectorSpec {
  readonly key: string;
  /** Ring radius and angle the lane leaves from. */
  readonly r1: number;
  readonly a1: number;
  /** Ring radius and angle the lane merges into. */
  readonly r2: number;
  readonly a2: number;
  /** Matches the ring the lane joins. */
  readonly strokeOpacity: number;
}

/**
 * Short lane changes between adjacent rings: two per flank, spanning only a
 * few degrees so each reads as a quick, direct hop instead of a long
 * on-ramp. Endpoint angles sit in the gaps between chips and inside the
 * visible band of the hero frame.
 */
const CONNECTORS: readonly ConnectorSpec[] = [
  { key: "a-b", r1: 440, a1: 167, r2: 560, a2: 176, strokeOpacity: 1 },
  { key: "c-d", r1: 680, a1: 205, r2: 800, a2: 197, strokeOpacity: 0.85 },
  { key: "b-a", r1: 560, a1: 338, r2: 440, a2: 344, strokeOpacity: 1 },
  { key: "b-c", r1: 560, a1: 366, r2: 680, a2: 358, strokeOpacity: 0.9 },
];

const rad = (deg: number) => (deg * Math.PI) / 180;

/** Point on a hero ring in the nominal 1600px box (angles are degrees, clockwise from 3 o'clock). */
function polarPoint(r: number, angleDeg: number): readonly [number, number] {
  return [800 + r * Math.cos(rad(angleDeg)), 800 + r * Math.sin(rad(angleDeg))];
}

/**
 * Lane-change curve between two rings: an eased spiral that leaves the first
 * ring tangentially at angle a1 and merges tangentially into the second at
 * a2 (degrees, clockwise from 3 o'clock, screen coordinates, either angular
 * direction). The radius follows a smoothstep from r1 to r2, so the lane
 * hugs each ring near its commit before crossing over. The spiral is emitted
 * as a chain of cubic beziers (one per 45 degrees of span) fitted from its
 * exact positions and derivatives, which stays smooth over the long 60 to
 * 100 degree on-ramps.
 */
function connectorPath(r1: number, a1: number, r2: number, a2: number): string {
  const t1 = rad(a1);
  const dt = rad(a2) - rad(a1);
  const dr = r2 - r1;
  const position = (u: number): [number, number] => {
    const s = u * u * (3 - 2 * u);
    const r = r1 + dr * s;
    const th = t1 + dt * u;
    return [800 + r * Math.cos(th), 800 + r * Math.sin(th)];
  };
  const derivative = (u: number): [number, number] => {
    const s = u * u * (3 - 2 * u);
    const ds = 6 * u * (1 - u);
    const r = r1 + dr * s;
    const rPrime = dr * ds;
    const th = t1 + dt * u;
    return [
      rPrime * Math.cos(th) - r * dt * Math.sin(th),
      rPrime * Math.sin(th) + r * dt * Math.cos(th),
    ];
  };
  const segments = Math.max(1, Math.ceil(Math.abs(a2 - a1) / 45));
  const h = 1 / segments;
  const f = (n: number) => n.toFixed(1);
  const [x0, y0] = position(0);
  const parts = [`M ${f(x0)} ${f(y0)}`];
  for (let i = 0; i < segments; i++) {
    const u0 = i * h;
    const u1 = (i + 1) * h;
    const [px0, py0] = position(u0);
    const [px1, py1] = position(u1);
    const [dx0, dy0] = derivative(u0);
    const [dx1, dy1] = derivative(u1);
    parts.push(
      `C ${f(px0 + (dx0 * h) / 3)} ${f(py0 + (dy0 * h) / 3)},`,
      `${f(px1 - (dx1 * h) / 3)} ${f(py1 - (dy1 * h) / 3)},`,
      `${f(px1)} ${f(py1)}`,
    );
  }
  return parts.join(" ");
}

interface OrbitNodeSpec {
  readonly key: string;
  /** Angle in degrees, clockwise from 3 o'clock (screen coordinates). */
  readonly angle: number;
  /** Ring radius in the nominal 1600px box. */
  readonly radius: number;
  readonly render: (starCount: number | null) => ReactNode;
}

/**
 * The non-avatar roster: community destinations and the two provable counter
 * pills, all real links riding the outer rings. Everything else in the orbit
 * is a contributor.
 */
const ORBIT_NODES: readonly OrbitNodeSpec[] = [
  {
    key: "stars",
    angle: 197,
    radius: 680,
    render: (starCount) => (
      <a
        href={GITHUB_STARGAZERS_URL}
        target="_blank"
        rel="noopener noreferrer"
        className={`${PILL_CLASSES} ${ORBIT_LINK_CLASSES}`}
      >
        <StarPillContent count={starCount} />
      </a>
    ),
  },
  {
    key: "license",
    angle: 343,
    radius: 680,
    render: () => (
      <a
        href={`${GITHUB_REPO_URL}/blob/main/LICENSE`}
        target="_blank"
        rel="noopener noreferrer"
        className={`${PILL_CLASSES} ${ORBIT_LINK_CLASSES}`}
      >
        MIT LICENSED
      </a>
    ),
  },
  {
    key: "github",
    angle: 155,
    radius: 680,
    render: () => (
      <SocialChip href={GITHUB_REPO_URL} label="ChilliCream on GitHub">
        <GitHubIcon className="h-5 w-5 fill-current" />
      </SocialChip>
    ),
  },
  {
    key: "slack",
    angle: 25,
    radius: 680,
    render: () => (
      <SocialChip href={TOOLS.slack} label="Join the ChilliCream Slack">
        <SlackIcon className="h-5 w-5 fill-current" />
      </SocialChip>
    ),
  },
  {
    key: "blog",
    angle: 38,
    radius: 560,
    render: () => (
      <SocialChip href="/blog" label="ChilliCream blog">
        <BlogIcon className="h-5 w-5 fill-current" />
      </SocialChip>
    ),
  },
];

interface AvatarSlotSpec {
  /** Angle in degrees, clockwise from 3 o'clock (screen coordinates). */
  readonly angle: number;
  /** Ring radius in the nominal 1600px box. */
  readonly radius: number;
  /** Three avatar scales, mixed so the orbit reads organic, not stamped. */
  readonly size: "lg" | "md" | "sm";
}

const AVATAR_SIZES = {
  lg: "h-14 w-14",
  md: "h-11 w-11",
  sm: "h-9 w-9",
} as const;

/**
 * Slots for contributor avatars, the body of the orbit. Slot order is rank
 * order: the featured lg trio on ring A goes to the top of the contributor
 * list, md fills the mid weight, sm the outer texture. Angles are authored
 * literals chosen so no avatar touches the copy, the pills, the lanes, or
 * the edge masks. The frame stays balanced when the contributor fetch
 * returns null and no avatar renders.
 */
const AVATAR_SLOTS: readonly AvatarSlotSpec[] = [
  { angle: 205, radius: 440, size: "lg" },
  { angle: 335, radius: 440, size: "lg" },
  { angle: 32, radius: 440, size: "lg" },
  { angle: 148, radius: 440, size: "md" },
  { angle: 186, radius: 440, size: "md" },
  { angle: 357, radius: 560, size: "md" },
  { angle: 210, radius: 560, size: "md" },
  { angle: 160, radius: 560, size: "md" },
  { angle: 368, radius: 680, size: "md" },
  { angle: 353, radius: 440, size: "sm" },
  { angle: 188, radius: 560, size: "sm" },
  { angle: 15, radius: 560, size: "sm" },
  { angle: 330, radius: 560, size: "sm" },
  { angle: 142, radius: 560, size: "sm" },
  { angle: 26, radius: 560, size: "sm" },
  { angle: 180, radius: 680, size: "sm" },
];

interface OrbitNodeProps {
  readonly angle: number;
  readonly radius: number;
  /** Entrance stagger delay in ms. */
  readonly delay: number;
  readonly children: ReactNode;
}

/**
 * Pins a node to its polar point on a ring. Positions are percentages of the
 * same nominal 1600px box the rings SVG draws in, so nodes and graph stay
 * aligned at every unit scale. The inner element self-centers and runs the
 * staggered entrance.
 */
function OrbitNode({ angle, radius, delay, children }: OrbitNodeProps) {
  const [x, y] = polarPoint(radius, angle);
  return (
    <div
      className="absolute"
      style={{
        left: `${((x / 1600) * 100).toFixed(3)}%`,
        top: `${((y / 1600) * 100).toFixed(3)}%`,
      }}
    >
      {/* Centering lives in `transform` (not the translate utilities) so the
          entrance keyframes replace it instead of stacking a second shift. */}
      <div
        className="eco-enter"
        style={
          {
            transform: "translate(-50%, -50%)",
            "--d": `${delay}ms`,
          } as CSSProperties
        }
      >
        {children}
      </div>
    </div>
  );
}

interface SocialChipProps {
  readonly href: string;
  readonly label: string;
  readonly children: ReactNode;
}

/** Disc-shaped link carrying a monochrome currentColor community icon. */
function SocialChip({ href, label, children }: SocialChipProps) {
  const className = `border-cc-card-border bg-cc-surface text-cc-ink-dim hover:text-cc-heading flex h-11 w-11 items-center justify-center rounded-full border ${ORBIT_LINK_CLASSES}`;

  if (href.startsWith("/")) {
    return (
      <Link href={href} aria-label={label} className={className}>
        {children}
      </Link>
    );
  }

  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={label}
      className={className}
    >
      {children}
    </a>
  );
}

interface StarPillProps {
  readonly count: number | null;
}

/**
 * Live stargazer count from the same hourly-cached source as the header. When
 * the count is unavailable the pill stays truthful: icon plus GITHUB, never a
 * hardcoded number, never empty.
 */
function StarPillContent({ count }: StarPillProps) {
  return (
    <>
      <GitHubIcon aria-hidden="true" className="h-3 w-3 fill-current" />
      {count === null ? (
        <span>GITHUB</span>
      ) : (
        <span className="text-cc-heading">
          <span className="sr-only">GitHub stars: </span>
          {count.toLocaleString("en-US")}
        </span>
      )}
    </>
  );
}

/** The non-interactive pill shell around the star count (community grid). */
function StarPill({ count }: StarPillProps) {
  return (
    <div className={PILL_CLASSES}>
      <StarPillContent count={count} />
    </div>
  );
}

interface MobileHeroLinksProps {
  readonly starCount: number | null;
  readonly contributors: ReadonlyArray<GitHubContributor> | null;
}

/** Keeps meaningful orbit destinations in flow when the clipped artwork hides. */
function MobileHeroLinks({ starCount, contributors }: MobileHeroLinksProps) {
  const linkClassName = `border-cc-card-border bg-cc-surface text-cc-ink-dim flex min-h-11 items-center justify-center gap-2 rounded-lg border px-3 font-mono text-[0.65rem] no-underline ${CARD_FOCUS_CLASSES}`;
  return (
    <nav
      aria-label="Ecosystem links"
      className="pointer-events-auto mx-auto mt-10 max-w-md sm:hidden"
    >
      <ul className="grid grid-cols-2 gap-2">
        <li>
          <a
            href={GITHUB_STARGAZERS_URL}
            target="_blank"
            rel="noopener noreferrer"
            className={linkClassName}
          >
            <StarPillContent count={starCount} />
          </a>
        </li>
        <li>
          <a
            href={`${GITHUB_REPO_URL}/blob/main/LICENSE`}
            target="_blank"
            rel="noopener noreferrer"
            className={linkClassName}
          >
            MIT license
          </a>
        </li>
        <li>
          <a
            href={TOOLS.slack}
            target="_blank"
            rel="noopener noreferrer"
            className={linkClassName}
          >
            <SlackIcon className="size-4 fill-current" />
            Slack
          </a>
        </li>
        <li>
          <Link href="/blog" className={linkClassName}>
            <BlogIcon className="size-4 fill-current" />
            Blog
          </Link>
        </li>
      </ul>
      {contributors !== null && contributors.length > 0 ? (
        <div className="mt-5">
          <p className="text-cc-ink-dim font-mono text-[0.62rem] tracking-[0.14em] uppercase">
            Top contributors on GitHub
          </p>
          <ul className="mt-3 grid grid-cols-6 gap-2">
            {contributors.slice(0, AVATAR_SLOTS.length).map((contributor) => (
              <li key={contributor.login}>
                <a
                  href={`https://github.com/${contributor.login}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label={`${contributor.login} on GitHub`}
                  className={`block rounded-full ${CARD_FOCUS_CLASSES}`}
                >
                  {/* eslint-disable-next-line @next/next/no-img-element -- remote GitHub avatar */}
                  <img
                    src={contributor.avatarUrl}
                    alt=""
                    loading="lazy"
                    decoding="async"
                    referrerPolicy="no-referrer"
                    className="border-cc-card-border bg-cc-surface aspect-square w-full rounded-full border"
                  />
                </a>
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </nav>
  );
}

interface HeroProps {
  readonly starCount: number | null;
  readonly contributors: ReadonlyArray<GitHubContributor> | null;
}

function Hero({ starCount, contributors }: HeroProps) {
  return (
    <section className="relative flex min-h-[640px] flex-col items-center justify-center py-24 [--u:0.62] sm:min-h-[720px] sm:[--u:0.78] lg:min-h-[820px] lg:[--u:1]">
      <style>{ENTRANCE_STYLES}</style>

      {/* Rings layer: constant-stroke circles and short branch lanes.
          Full-bleed and unclipped: the box can extend past the section's own
          top/bottom, painting over whatever precedes or follows the hero. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-y-0 left-1/2 w-screen -translate-x-1/2"
      >
        <div
          className="absolute top-[46%] left-1/2 -translate-x-1/2 -translate-y-1/2"
          style={{
            width: "calc(1600px * var(--u))",
            height: "calc(1600px * var(--u))",
          }}
        >
          <svg viewBox="0 0 1600 1600" className="h-full w-full">
            {ORBIT_RINGS.map((ring) => (
              <circle
                key={ring.r}
                cx="800"
                cy="800"
                r={ring.r}
                fill="none"
                stroke={RING_STROKE}
                strokeOpacity={ring.strokeOpacity}
                vectorEffect="non-scaling-stroke"
              />
            ))}
            {CONNECTORS.map((c) => (
              <path
                key={c.key}
                d={connectorPath(c.r1, c.a1, c.r2, c.a2)}
                fill="none"
                stroke={RING_STROKE}
                strokeOpacity={c.strokeOpacity}
                vectorEffect="non-scaling-stroke"
              />
            ))}
          </svg>
        </div>
      </div>

      {/* Nodes layer: same box in a second full-bleed, unclipped wrapper.
          Its vertical mask dissolves chips near the top/bottom edges for a
          soft transition. Not aria-hidden: the pills and social chips are
          real links, and they punch through the wrapper's
          pointer-events-none on their own. */}
      <div
        className="pointer-events-none absolute inset-y-0 left-1/2 hidden w-screen -translate-x-1/2 sm:block"
        style={{ maskImage: NODE_EDGE_MASK, WebkitMaskImage: NODE_EDGE_MASK }}
      >
        <div
          className="absolute top-[46%] left-1/2 -translate-x-1/2 -translate-y-1/2"
          style={{
            width: "calc(1600px * var(--u))",
            height: "calc(1600px * var(--u))",
          }}
        >
          {ORBIT_NODES.map((node, i) => (
            <OrbitNode
              key={node.key}
              angle={node.angle}
              radius={node.radius}
              delay={i * 40}
            >
              {node.render(starCount)}
            </OrbitNode>
          ))}
          {/* Contributor avatars are the body of the orbit, each linking to
              its GitHub account (the anchor carries the accessible name, so
              alt stays empty). Plain img on purpose: remote GitHub avatars
              bypass the image pipeline. */}
          {contributors?.slice(0, AVATAR_SLOTS.length).map((contributor, i) => {
            const slot = AVATAR_SLOTS[i];
            return (
              <OrbitNode
                key={contributor.login}
                angle={slot.angle}
                radius={slot.radius}
                delay={(ORBIT_NODES.length + i) * 40}
              >
                <a
                  href={`https://github.com/${contributor.login}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label={`${contributor.login} on GitHub`}
                  className={`group block rounded-full ${ORBIT_LINK_CLASSES}`}
                >
                  {/* eslint-disable-next-line @next/next/no-img-element -- see comment above */}
                  <img
                    src={contributor.avatarUrl}
                    alt=""
                    loading="lazy"
                    decoding="async"
                    referrerPolicy="no-referrer"
                    className={`border-cc-card-border bg-cc-surface group-hover:border-cc-card-border-hover rounded-full border transition-colors ${AVATAR_SIZES[slot.size]}`}
                  />
                </a>
              </OrbitNode>
            );
          })}
        </div>
      </div>

      {/* Scrim: light center safety behind the copy. At the sm and mobile
          unit scales the text column is wider than the inner rings, so this
          keeps the copy readable where arcs pass behind it. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute top-[46%] left-1/2 z-[1] h-[560px] w-[120%] max-w-[880px] -translate-x-1/2 -translate-y-1/2 sm:h-[520px] sm:w-[85%]"
        style={{
          background:
            "radial-gradient(closest-side, var(--color-cc-bg) 55%, transparent 100%)",
        }}
      />

      {/* pointer-events-none keeps this full-height copy box from eating
          clicks on the orbit links beside it; the CTA row opts back in. */}
      <div className="pointer-events-none relative z-10 px-5 text-center">
        <h1 className="font-heading text-h3 sm:text-h2 lg:text-h1 text-cc-heading mx-auto max-w-3xl font-semibold text-balance">
          The open ecosystem{" "}
          <span className="text-cc-accent">for .NET, built in public.</span>
        </h1>
        <p className="lead text-cc-ink-dim mx-auto mt-5 max-w-2xl">
          Explore the code, read the docs, and talk to the maintainers building
          the platform.
        </p>
        <ButtonRow align="center" className="pointer-events-auto mt-8">
          <SolidButton href={GITHUB_REPO_URL}>Explore the code</SolidButton>
          {/* Opaque page-color fill so orbiting chips never show through. */}
          <OutlineButton href="/docs" className="bg-cc-bg">
            Read the docs
          </OutlineButton>
        </ButtonRow>
        <MobileHeroLinks starCount={starCount} contributors={contributors} />
        <p className="font-heading text-h4 text-cc-heading mx-auto mt-20 max-w-2xl font-semibold sm:mt-24">
          Open source you can inspect. Standards you can follow. People you can
          reach.
        </p>
      </div>
    </section>
  );
}

/**
 * GitHub's dark-theme contribution ramp over a faint cream empty cell, from
 * no commits to the busiest day of the year.
 */
const HEATMAP_LEVELS = [
  "rgba(245,240,234,0.06)",
  "#0e4429",
  "#006d32",
  "#26a641",
  "#39d353",
] as const;

interface CommitHeatmapProps {
  readonly weeks: ReadonlyArray<ReadonlyArray<number>>;
}

/**
 * GitHub-style contribution heatmap: one column per week, one cell per day,
 * leveled against the busiest day. Fluid cells (aspect-square in an fr grid)
 * let the full year compress into whatever width the card offers. Decorative,
 * so the grid is aria-hidden and the caption row carries the meaning.
 */
function CommitHeatmap({ weeks }: CommitHeatmapProps) {
  const maxCount = Math.max(...weeks.flat(), 1);
  return (
    <div
      aria-hidden="true"
      className="grid auto-cols-fr grid-flow-col grid-rows-7 gap-[2px]"
    >
      {weeks.flatMap((days, weekIndex) =>
        days.map((count, dayIndex) => (
          <div
            key={`${weekIndex}-${dayIndex}`}
            className="aspect-square rounded-[2px]"
            style={{
              backgroundColor:
                HEATMAP_LEVELS[
                  count === 0
                    ? 0
                    : Math.min(4, Math.ceil((count / maxCount) * 4))
                ],
            }}
          />
        )),
      )}
    </div>
  );
}

interface ProofRowSpec {
  readonly tag: string;
  readonly body: string;
}

const PROOF_ROWS: readonly ProofRowSpec[] = [
  {
    tag: "ONE REPOSITORY",
    body: "Every part is built and released from the same codebase, so the pieces stay in step.",
  },
  {
    tag: "MIT LICENSE",
    body: "Open source under the MIT license. Free to use in commercial products.",
  },
  {
    tag: "PUBLIC DEVELOPMENT",
    body: "Follow issues, pull requests, releases, and changelogs as the work lands.",
  },
];

interface ProofBandProps {
  readonly commitActivity: ReadonlyArray<ReadonlyArray<number>> | null;
}

function ProofBand({ commitActivity }: ProofBandProps) {
  return (
    <section className="py-14 sm:py-20">
      <RevealOnScroll>
        <div className="grid grid-cols-1 items-center gap-10 lg:grid-cols-12 lg:gap-16">
          <div className="min-w-0 lg:col-span-5">
            <SectionHeading
              title="Built in the open, in one repository."
              description="The server, gateway, client, and core libraries are all developed in a single public GitHub repository."
            />
          </div>
          <div className="min-w-0 lg:col-span-7">
            <Card
              as="a"
              href={GITHUB_REPO_URL}
              target="_blank"
              rel="noopener noreferrer"
              hoverBorder
              className={`block no-underline backdrop-blur ${CARD_FOCUS_CLASSES}`}
            >
              <div className="border-cc-card-border flex items-center justify-between gap-3 border-b px-6 py-4">
                <span className="flex min-w-0 items-center gap-3">
                  <span className="border-cc-card-border bg-cc-surface text-cc-ink-dim flex h-9 w-9 shrink-0 items-center justify-center rounded-full border">
                    <GitHubIcon className="h-4 w-4 fill-current" />
                  </span>
                  <span className="text-cc-heading truncate font-mono text-sm">
                    ChilliCream/graphql-platform
                  </span>
                </span>
                <span aria-hidden="true" className="text-cc-ink-dim">
                  ↗
                </span>
              </div>
              <div className="divide-cc-card-border divide-y">
                {PROOF_ROWS.map((row) => (
                  <div
                    key={row.tag}
                    className="grid grid-cols-1 items-baseline gap-1 px-6 py-4 sm:grid-cols-[11rem_1fr] sm:gap-6"
                  >
                    <span className="text-cc-ink-dim font-mono text-[0.6rem] tracking-[0.18em] uppercase">
                      {row.tag}
                    </span>
                    <span className="text-cc-ink text-sm">{row.body}</span>
                  </div>
                ))}
              </div>
              {/* Live texture: the past year of commits, straight from the
                  repository this card links to. Hidden when GitHub has not
                  answered, never faked. */}
              {commitActivity ? (
                <div className="border-cc-card-border border-t">
                  <p className="text-cc-ink-dim px-6 pt-4 font-mono text-[0.6rem] tracking-[0.18em] uppercase">
                    COMMITS · PAST YEAR
                  </p>
                  <div className="mt-3">
                    <CommitHeatmap weeks={commitActivity} />
                  </div>
                </div>
              ) : null}
            </Card>
          </div>
        </div>
      </RevealOnScroll>
    </section>
  );
}

interface StandardsItemSpec {
  /** ChilliCream's role in the group, shown as the card tag. */
  readonly role: "HOST" | "TWO SEATS" | "MEMBERS" | "ORGANIZERS";
  /** Accents the tags carrying the strongest claims: TSC seats and leads. */
  readonly accent: boolean;
  readonly name: string;
  /** What the group itself owns; the tag carries our relationship to it. */
  readonly body: string;
  readonly href: string;
}

/**
 * One box per working group and initiative. Roles are as stated by the
 * founders; memberships and URLs match the official roster at
 * graphql.org/community/team. These claims are the page's sharpest trust
 * argument, so keep them literal and current.
 */
const STANDARDS_ITEMS: readonly StandardsItemSpec[] = [
  {
    role: "TWO SEATS",
    accent: true,
    name: "Technical Steering Committee",
    body: "The committee steering the GraphQL specification. Michael Staib and Pascal Senn both hold seats.",
    href: "https://github.com/graphql/graphql-wg/blob/main/GraphQL-TSC.md",
  },
  {
    role: "HOST",
    accent: true,
    name: "Composite Schemas Working Group",
    body: "Michael Staib hosts the Composite Schemas subcommittee, which develops an open standard for composing GraphQL services.",
    href: "https://github.com/graphql/composite-schemas-wg",
  },
  {
    role: "HOST",
    accent: true,
    name: "GraphQL/OpenTelemetry Working Group",
    body: "Pascal Senn hosts GraphQL/OTel, which develops OpenTelemetry conventions for GraphQL APIs.",
    href: "https://github.com/graphql/otel-wg",
  },
  {
    role: "ORGANIZERS",
    accent: true,
    name: "GraphQL Day",
    body: "Community events around the world, organized with the GraphQL Foundation team.",
    href: "https://graphql.org/day",
  },
  {
    role: "MEMBERS",
    accent: false,
    name: "GraphQL Working Group",
    body: "The main working group evolving the GraphQL specification itself.",
    href: "https://github.com/graphql/graphql-wg",
  },
  {
    role: "MEMBERS",
    accent: false,
    name: "GraphQL over HTTP",
    body: "The specification for transporting GraphQL over HTTP, so servers and clients interoperate.",
    href: "https://github.com/graphql/graphql-over-http",
  },
  {
    role: "MEMBERS",
    accent: false,
    name: "AI Working Group",
    body: "Best practices for GraphQL in AI systems and agent-powered applications.",
    href: "https://github.com/graphql/ai-wg",
  },
];

function StandardsBand() {
  return (
    <section className="py-14 sm:py-20">
      <RevealOnScroll>
        <SectionHeading
          title="Helping write the standards we implement."
          description={
            <>
              ChilliCream contributors help shape the specifications and
              conventions the platform implements. See the{" "}
              <a
                href="https://graphql.org/community/team/"
                target="_blank"
                rel="noopener noreferrer"
                className="text-cc-accent hover:text-cc-accent-hover"
              >
                official GraphQL team page
              </a>{" "}
              for current roles and participation.
            </>
          }
        />
        <div className="mt-10 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {STANDARDS_ITEMS.map((item) => (
            <Card
              key={item.name}
              as="a"
              href={item.href}
              target="_blank"
              rel="noopener noreferrer"
              variant="tile"
              hoverBorder
              className={`group flex h-full flex-col no-underline ${CARD_FOCUS_CLASSES}`}
            >
              {/* TSC seats and leads carry the accent; membership stays dim. */}
              <p
                className={`font-mono text-[0.6rem] tracking-[0.18em] uppercase ${
                  item.accent ? "text-cc-accent" : "text-cc-ink-dim"
                }`}
              >
                {item.role}
              </p>
              <h3 className="font-heading text-cc-heading mt-4 text-base font-semibold">
                {item.name}
              </h3>
              <p className="text-cc-ink-dim mt-2 pb-6 text-sm">{item.body}</p>
              {/* Pinned footer names the destination host, like a citation. */}
              <div className="mt-auto flex items-center justify-between">
                <span className="text-cc-ink-dim font-mono text-[0.6rem] tracking-[0.14em] uppercase">
                  {new URL(item.href).host}
                </span>
                <span
                  aria-hidden="true"
                  className="text-cc-ink-dim group-hover:text-cc-heading transition-colors"
                >
                  ↗
                </span>
              </div>
            </Card>
          ))}
        </div>
      </RevealOnScroll>
    </section>
  );
}

interface CommunityCardSpec {
  readonly title: string;
  readonly body: string;
  readonly href: string;
  readonly icon: ReactNode;
  readonly withStars: boolean;
  /** Footer micro-label for cards without the star pill. */
  readonly action: string | null;
}

const COMMUNITY_CARDS: readonly CommunityCardSpec[] = [
  {
    title: "GitHub",
    body: "Issues, discussions, pull requests, and the source itself.",
    href: GITHUB_REPO_URL,
    icon: <GitHubIcon className="h-5 w-auto fill-current" />,
    withStars: true,
    action: null,
  },
  {
    title: "Slack",
    body: "Ask questions and talk to the team and other users directly.",
    href: TOOLS.slack,
    icon: <SlackIcon className="h-5 w-auto fill-current" />,
    withStars: false,
    action: "Join",
  },
  {
    title: "YouTube",
    body: "Talks, release walkthroughs, and deep dives.",
    href: TOOLS.youtube,
    icon: <YouTubeIcon className="h-5 w-auto fill-current" />,
    withStars: false,
    action: "Watch",
  },
  {
    title: "Blog",
    body: "Release notes, engineering write-ups, and announcements.",
    href: "/blog",
    icon: <BlogIcon className="h-5 w-auto fill-current" />,
    withStars: false,
    action: "Read",
  },
];

interface CommunityCardProps {
  readonly card: CommunityCardSpec;
  readonly starCount: number | null;
}

function CommunityCard({ card, starCount }: CommunityCardProps) {
  const content = (
    <>
      <div className="border-cc-card-border bg-cc-surface text-cc-ink-dim flex h-10 w-10 items-center justify-center rounded-full border">
        {card.icon}
      </div>
      <h3 className="font-heading text-cc-heading mt-4 text-base font-semibold">
        {card.title}
      </h3>
      <p className="text-cc-ink-dim mt-2 pb-6 text-sm">{card.body}</p>
      {/* Pinned footer keeps the four card bottoms on one baseline. */}
      <div className="mt-auto flex h-7 items-center">
        {card.withStars ? (
          <StarPill count={starCount} />
        ) : (
          <span className="text-cc-ink-dim group-hover:text-cc-heading text-sm font-medium transition-colors">
            {card.action} <span aria-hidden="true">→</span>
          </span>
        )}
      </div>
    </>
  );

  const external = !card.href.startsWith("/");

  return (
    <Card
      as="a"
      href={card.href}
      target={external ? "_blank" : undefined}
      rel={external ? "noopener noreferrer" : undefined}
      variant="tile"
      hoverBorder
      className={`group flex h-full flex-col no-underline ${CARD_FOCUS_CLASSES}`}
    >
      {content}
    </Card>
  );
}

interface CommunityGridProps {
  readonly starCount: number | null;
}

function CommunityGrid({ starCount }: CommunityGridProps) {
  return (
    <section className="py-14 sm:py-20">
      <RevealOnScroll>
        <SectionHeading
          title="Find the people behind the code."
          description="Follow the work on GitHub, bring questions to Slack, and learn from talks and engineering posts."
        />
        <div className="mt-10 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {COMMUNITY_CARDS.map((card) => (
            <CommunityCard key={card.title} card={card} starCount={starCount} />
          ))}
        </div>
      </RevealOnScroll>
    </section>
  );
}

/**
 * Hero curation: contributor logins kept out of the orbit. The next ranked
 * contributor moves up and takes the slot.
 */
const HIDDEN_CONTRIBUTOR_LOGINS = new Set(["artola"]);

export default async function EcosystemPage() {
  const [starCount, contributors, commitActivity] = await Promise.all([
    getGitHubStarCount(),
    getGitHubContributors(),
    getGitHubCommitActivity(),
  ]);
  const heroContributors =
    contributors?.filter(
      (contributor) => !HIDDEN_CONTRIBUTOR_LOGINS.has(contributor.login),
    ) ?? null;

  return (
    <>
      <Hero starCount={starCount} contributors={heroContributors} />
      <ProofBand commitActivity={commitActivity} />
      <StandardsBand />
      <CommunityGrid starCount={starCount} />
      <RevealOnScroll>
        <FromOurBlog limit={3} className="py-10 sm:py-14" />
      </RevealOnScroll>
      <RevealOnScroll>
        <NextStepsSection
          title="See whether it fits your architecture."
          text="Read the docs, run a focused evaluation, and talk to a maintainer about your architecture. Then decide."
          primaryLink="/docs"
          primaryLinkText="Read the docs"
          secondaryLink={TOOLS.slack}
          secondaryLinkText="Talk to a maintainer"
        />
      </RevealOnScroll>
    </>
  );
}
