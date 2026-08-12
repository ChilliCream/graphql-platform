import Link from "next/link";
import type { CSSProperties, ReactNode } from "react";

import { ButtonRow } from "@/src/components/ButtonRow";
import { TOOLS } from "@/src/components/header/navData";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { GITHUB_REPO_URL, GITHUB_STARGAZERS_URL } from "@/src/helpers/github";
import type { GitHubContributor } from "@/src/helpers/githubContributors";
import { BlogIcon } from "@/src/icons/Blog";
import { GitHubIcon } from "@/src/icons/GitHub";
import { SlackIcon } from "@/src/icons/Slack";

import { CARD_FOCUS_CLASSES } from "./cardFocus";
import {
  CONNECTORS,
  ORBIT_RINGS,
  connectorPath,
  polarPoint,
} from "./orbitGeometry";
import { PILL_CLASSES, StarPillContent } from "./StarPill";

const RING_STROKE = "rgba(245,240,234,0.17)";

const NODE_EDGE_MASK =
  "linear-gradient(to bottom, transparent 0, black 64px, black calc(100% - 56px), transparent 100%)";

const ENTRANCE_STYLES = `
@media (prefers-reduced-motion: no-preference) {
  .eco-enter { animation: eco-enter 0.6s ease-out var(--d) both; }
  @keyframes eco-enter {
    from { opacity: 0; transform: translate(-50%, -50%) scale(0.85); }
    to { opacity: 1; transform: translate(-50%, -50%) scale(1); }
  }
}
`;

const ORBIT_LINK_CLASSES = `hover:border-cc-card-border-hover pointer-events-auto no-underline transition-colors ${CARD_FOCUS_CLASSES}`;

interface OrbitNodeSpec {
  readonly key: string;
  readonly angle: number;
  readonly radius: number;
  readonly content: ReactNode;
}

const ORBIT_NODES: readonly OrbitNodeSpec[] = [
  {
    key: "stars",
    angle: 197,
    radius: 680,
    content: (
      <a
        href={GITHUB_STARGAZERS_URL}
        target="_blank"
        rel="noopener noreferrer"
        className={`${PILL_CLASSES} ${ORBIT_LINK_CLASSES}`}
      >
        <StarPillContent />
      </a>
    ),
  },
  {
    key: "license",
    angle: 343,
    radius: 680,
    content: (
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
    content: (
      <SocialChip href={GITHUB_REPO_URL} label="ChilliCream on GitHub">
        <GitHubIcon className="h-5 w-5 fill-current" />
      </SocialChip>
    ),
  },
  {
    key: "slack",
    angle: 25,
    radius: 680,
    content: (
      <SocialChip href={TOOLS.slack} label="Join the ChilliCream Slack">
        <SlackIcon className="h-5 w-5 fill-current" />
      </SocialChip>
    ),
  },
  {
    key: "blog",
    angle: 38,
    radius: 560,
    content: (
      <SocialChip href="/blog" label="ChilliCream blog">
        <BlogIcon className="h-5 w-5 fill-current" />
      </SocialChip>
    ),
  },
];

interface AvatarSlotSpec {
  readonly angle: number;
  readonly radius: number;
  readonly size: "lg" | "md" | "sm";
}

const AVATAR_SIZES = {
  lg: "h-14 w-14",
  md: "h-11 w-11",
  sm: "h-9 w-9",
} as const;

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
  readonly delay: number;
  readonly children: ReactNode;
}

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

interface MobileHeroLinksProps {
  readonly contributors: ReadonlyArray<GitHubContributor> | null;
}

function MobileHeroLinks({ contributors }: MobileHeroLinksProps) {
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
            <StarPillContent />
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
  readonly contributors: ReadonlyArray<GitHubContributor> | null;
}

export function Hero({ contributors }: HeroProps) {
  return (
    <section className="relative flex min-h-[640px] flex-col items-center justify-center py-24 [--u:0.62] sm:min-h-[720px] sm:[--u:0.78] lg:min-h-[820px] lg:[--u:1]">
      <style href="ecosystem-hero-entrance" precedence="medium">
        {ENTRANCE_STYLES}
      </style>

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
              {node.content}
            </OrbitNode>
          ))}
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
                  {/* eslint-disable-next-line @next/next/no-img-element -- remote GitHub avatar */}
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

      <div
        aria-hidden="true"
        className="pointer-events-none absolute top-[46%] left-1/2 z-[1] h-[560px] w-[120%] max-w-[880px] -translate-x-1/2 -translate-y-1/2 sm:h-[520px] sm:w-[85%]"
        style={{
          background:
            "radial-gradient(closest-side, var(--color-cc-bg) 55%, transparent 100%)",
        }}
      />

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
          <OutlineButton href="/docs" className="bg-cc-bg">
            Read the docs
          </OutlineButton>
        </ButtonRow>
        <MobileHeroLinks contributors={contributors} />
        <p className="font-heading text-h4 text-cc-heading mx-auto mt-20 max-w-2xl font-semibold sm:mt-24">
          Open source you can inspect. Standards you can follow. People you can
          reach.
        </p>
      </div>
    </section>
  );
}
