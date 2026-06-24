"use client";

import NextLink from "next/link";
import {
  animate,
  motion,
  MotionConfig,
  useInView,
  useMotionValue,
  useReducedMotion,
  useTransform,
} from "motion/react";
import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type ComponentType,
  type CSSProperties,
} from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { LogoCloud } from "@/src/components/home/LogoCloud";
import { CookieCrumble } from "@/src/icons/CookieCrumble";
import { GreenDonut } from "@/src/icons/GreenDonut";
import { HotChocolate } from "@/src/icons/HotChocolate";
import { Mocha } from "@/src/icons/Mocha";
import { Nitro } from "@/src/icons/Nitro";
import { StrawberryShake } from "@/src/icons/StrawberryShake";

// -----------------------------------------------------------------------------
// Data (kept verbatim from v1 ground truth)
// -----------------------------------------------------------------------------

interface ProductNode {
  readonly name: string;
  readonly tagline: string;
  readonly href: string;
  readonly external?: boolean;
  readonly icon: ComponentType<{
    readonly className?: string;
    readonly style?: CSSProperties;
  }>;
  readonly x: number;
  readonly y: number;
}

// Constellation layout on a 1000x560 viewBox. Coordinates are tuned by hand so
// connector lines stay readable and labels do not overlap.
const PRODUCT_NODES: readonly ProductNode[] = [
  {
    name: "Hot Chocolate",
    tagline: "GraphQL server for .NET",
    href: "/products/hotchocolate",
    icon: HotChocolate,
    x: 500,
    y: 90,
  },
  {
    name: "Strawberry Shake",
    tagline: "Typed .NET client",
    href: "/products/strawberryshake",
    icon: StrawberryShake,
    x: 170,
    y: 200,
  },
  {
    name: "Nitro",
    tagline: "Control plane and IDE",
    href: "https://nitro.chillicream.com",
    external: true,
    icon: Nitro,
    x: 830,
    y: 200,
  },
  {
    name: "Mocha",
    tagline: "Mediator and messaging",
    href: "https://github.com/ChilliCream/graphql-platform",
    external: true,
    icon: Mocha,
    x: 260,
    y: 440,
  },
  {
    name: "Green Donut",
    tagline: "DataLoader for .NET",
    href: "https://github.com/ChilliCream/graphql-platform",
    external: true,
    icon: GreenDonut,
    x: 500,
    y: 470,
  },
  {
    name: "Cookie Crumble",
    tagline: "GraphQL-aware snapshot testing",
    href: "https://github.com/ChilliCream/graphql-platform",
    external: true,
    icon: CookieCrumble,
    x: 740,
    y: 440,
  },
];

// Connector edges between product nodes (indices into PRODUCT_NODES).
const CONNECTORS: readonly (readonly [number, number])[] = [
  [0, 1],
  [0, 2],
  [0, 4],
  [0, 5],
  [0, 3],
  [1, 3],
  [2, 5],
  [3, 4],
  [4, 5],
  [1, 4],
];

interface Principle {
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
}

const PRINCIPLES: readonly Principle[] = [
  {
    eyebrow: "01",
    title: "Open source first",
    body: "The entire platform lives on GitHub. You can read the code, file an issue, send a pull request, and ship the same bits we ship. Public roadmaps, public releases, public discussions.",
  },
  {
    eyebrow: "02",
    title: "Honest about what we ship",
    body: "We describe what the platform does, not what it might do one day. If a capability is in preview, we say so. If a tradeoff exists, we name it. No marketing language standing in for engineering.",
  },
  {
    eyebrow: "03",
    title: ".NET-native, end to end",
    body: "Server, client, mediator, DataLoader, and tests are designed together for .NET. OpenTelemetry-native observability, performance treated as a feature, and a developer experience that respects the language.",
  },
];

interface Metric {
  readonly value: number;
  readonly suffix?: string;
  readonly label: string;
}

const METRICS: readonly Metric[] = [
  { value: 6, label: "Products in the platform" },
  { value: 7, suffix: "+", label: "Community channels" },
];

interface CommunityLink {
  readonly label: string;
  readonly href: string;
  readonly description: string;
}

const COMMUNITY: readonly CommunityLink[] = [
  {
    label: "Slack",
    href: "https://slack.chillicream.com/",
    description:
      "Ask questions, trade patterns, talk to the people who build the platform.",
  },
  {
    label: "GitHub",
    href: "https://github.com/ChilliCream/graphql-platform",
    description:
      "Read the code, file issues, send pull requests. The platform is built in the open.",
  },
  {
    label: "YouTube",
    href: "https://www.youtube.com/c/ChilliCream",
    description:
      "Talks, deep dives, and walkthroughs from the team and the wider community.",
  },
  {
    label: "Blog",
    href: "/blog",
    description:
      "Release notes, design decisions, and engineering writeups straight from the team.",
  },
  {
    label: "Nitro",
    href: "https://nitro.chillicream.com",
    description:
      "Sign in to the control plane: schema registry, CI checks, observability, IDE.",
  },
  {
    label: "X",
    href: "https://x.com/Chilli_Cream",
    description:
      "Short updates, releases, and conversation with the broader GraphQL community.",
  },
  {
    label: "LinkedIn",
    href: "https://www.linkedin.com/company/chillicream",
    description:
      "Follow the company for product news, talks, and hiring updates.",
  },
];

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export function AboutV7Client() {
  return (
    <MotionConfig reducedMotion="user">
      <MissionHero />
      <PlatformConstellation />
      <LogoCloud />
      <PrinciplesTriad />
      <MetricsStrip />
      <CommunityGrid />
      <EngageBand />
    </MotionConfig>
  );
}

// -----------------------------------------------------------------------------
// Hero
// -----------------------------------------------------------------------------

function MissionHero() {
  const reduced = useReducedMotion();
  const headline = "The GraphQL platform for .NET teams.";
  const words = headline.split(" ");

  return (
    <section className="py-16 text-center sm:py-24">
      <motion.p
        initial={reduced ? false : { opacity: 0, y: 6 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
        className="text-cc-nav-label mb-4 font-mono text-xs tracking-[0.2em] uppercase"
      >
        About ChilliCream
      </motion.p>

      <h1 className="font-heading text-cc-heading text-hero mx-auto max-w-4xl tracking-tight">
        {words.map((word, i) => {
          // Accent the ".NET teams." span contiguously to match v1.
          const accent = i === words.length - 2 || i === words.length - 1;
          return (
            <motion.span
              key={`${word}-${i}`}
              initial={reduced ? false : { opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{
                duration: 0.45,
                delay: reduced ? 0 : 0.12 + i * 0.05,
                ease: [0.22, 1, 0.36, 1],
              }}
              className={`inline-block ${accent ? "text-cc-accent" : ""}`}
            >
              {word}
              {i < words.length - 1 ? " " : ""}
            </motion.span>
          );
        })}
      </h1>

      <motion.p
        initial={reduced ? false : { opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, delay: reduced ? 0 : 0.45 }}
        className="lead text-cc-ink mx-auto mt-8 max-w-2xl"
      >
        We build an end-to-end GraphQL platform: the server, the typed client,
        the gateway, the control plane, and the tools to test and ship it. All
        open source, all designed together, all in the open on GitHub.
      </motion.p>

      <motion.div
        initial={reduced ? false : { opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, delay: reduced ? 0 : 0.6 }}
        className="mt-10 flex flex-wrap items-center justify-center gap-3"
      >
        <SolidButton href="https://github.com/ChilliCream/graphql-platform">
          Explore the platform on GitHub
        </SolidButton>
        <OutlineButton href="/services">Work with us</OutlineButton>
      </motion.div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Platform Constellation (centerpiece)
// -----------------------------------------------------------------------------

function PlatformConstellation() {
  const sectionRef = useRef<HTMLDivElement | null>(null);
  const inView = useInView(sectionRef, { once: true, margin: "-15% 0px" });
  const reduced = useReducedMotion();

  const [activeIndex, setActiveIndex] = useState(0);
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);

  // Slow pulse cycles a highlighted active node every few seconds.
  useEffect(() => {
    if (reduced || !inView) return;
    const id = window.setInterval(() => {
      setActiveIndex((i) => (i + 1) % PRODUCT_NODES.length);
    }, 2600);
    return () => window.clearInterval(id);
  }, [reduced, inView]);

  const focused = hoverIndex ?? activeIndex;

  // Indices of connectors that touch the focused node, for accent highlighting.
  const activeEdgeIndices = useMemo(() => {
    const set = new Set<number>();
    CONNECTORS.forEach((edge, idx) => {
      if (edge[0] === focused || edge[1] === focused) set.add(idx);
    });
    return set;
  }, [focused]);

  return (
    <section ref={sectionRef} className="py-16 sm:py-20">
      <header className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.2em] uppercase">
          The platform we build
        </p>
        <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
          Six products. One platform.
        </h2>
        <p className="text-cc-ink mt-5 text-base sm:text-lg">
          Every piece is a real, shipping open-source project in the
          ChilliCream/graphql-platform repository. Pick one and adopt it on its
          own, or compose the full stack.
        </p>
      </header>

      <div className="bg-cc-card-bg border-cc-card-border mt-12 overflow-hidden rounded-3xl border p-2 sm:p-4">
        <div className="relative">
          <svg
            viewBox="0 0 1000 560"
            role="img"
            aria-label="The ChilliCream platform: six products connected as a constellation."
            className="block h-auto w-full"
          >
            <defs>
              <radialGradient id="cc-v7-glow" cx="50%" cy="50%" r="50%">
                <stop
                  offset="0%"
                  stopColor="var(--color-cc-accent)"
                  stopOpacity="0.35"
                />
                <stop
                  offset="100%"
                  stopColor="var(--color-cc-accent)"
                  stopOpacity="0"
                />
              </radialGradient>
            </defs>

            {/* Connector lines: draw themselves on view, brighten if they touch the focused node. */}
            <g>
              {CONNECTORS.map(([a, b], i) => {
                const from = PRODUCT_NODES[a];
                const to = PRODUCT_NODES[b];
                const isActive = activeEdgeIndices.has(i);
                return (
                  <motion.line
                    key={`edge-${i}`}
                    x1={from.x}
                    y1={from.y}
                    x2={to.x}
                    y2={to.y}
                    stroke={
                      isActive
                        ? "var(--color-cc-accent)"
                        : "var(--color-cc-card-border)"
                    }
                    strokeWidth={isActive ? 1.5 : 1}
                    strokeLinecap="round"
                    initial={reduced ? false : { pathLength: 0, opacity: 0 }}
                    animate={
                      inView || reduced
                        ? { pathLength: 1, opacity: isActive ? 0.9 : 0.55 }
                        : undefined
                    }
                    transition={{
                      pathLength: {
                        duration: reduced ? 0 : 1.1,
                        delay: reduced ? 0 : 0.2 + i * 0.08,
                        ease: [0.22, 1, 0.36, 1],
                      },
                      opacity: { duration: 0.4 },
                    }}
                  />
                );
              })}
            </g>

            {/* Nodes: fade and scale in, focused node gets a soft halo and an accent ring. */}
            <g>
              {PRODUCT_NODES.map((node, i) => {
                const isFocused = focused === i;
                return (
                  <ConstellationNode
                    key={node.name}
                    node={node}
                    index={i}
                    inView={inView}
                    reduced={reduced ?? false}
                    isFocused={isFocused}
                    onEnter={() => setHoverIndex(i)}
                    onLeave={() => setHoverIndex(null)}
                  />
                );
              })}
            </g>
          </svg>

          {/* Live callout for the focused node. Positioned absolutely over the SVG. */}
          <NodeCallout node={PRODUCT_NODES[focused]} />
        </div>
      </div>

      <p className="text-cc-ink-dim mt-6 text-center text-sm">
        Fusion, our distributed gateway, is built on Hot Chocolate and lives in
        the same repository. Hover a node to focus it, click to open.
      </p>
    </section>
  );
}

interface ConstellationNodeProps {
  readonly node: ProductNode;
  readonly index: number;
  readonly inView: boolean;
  readonly reduced: boolean;
  readonly isFocused: boolean;
  readonly onEnter: () => void;
  readonly onLeave: () => void;
}

function ConstellationNode({
  node,
  index,
  inView,
  reduced,
  isFocused,
  onEnter,
  onLeave,
}: ConstellationNodeProps) {
  const Icon = node.icon;
  const radius = 38;

  const group = (
    <motion.g
      initial={reduced ? false : { opacity: 0, scale: 0.6 }}
      animate={
        inView || reduced
          ? { opacity: 1, scale: isFocused ? 1.06 : 1 }
          : undefined
      }
      transition={{
        opacity: {
          duration: 0.5,
          delay: reduced ? 0 : 0.4 + index * 0.08,
        },
        scale: {
          type: "spring",
          stiffness: 220,
          damping: 22,
          delay: reduced ? 0 : 0.4 + index * 0.08,
        },
      }}
      style={{ pointerEvents: "none" }}
    >
      {/* Soft halo behind focused node. */}
      <motion.circle
        cx={node.x}
        cy={node.y}
        r={radius + 28}
        fill="url(#cc-v7-glow)"
        initial={false}
        animate={{ opacity: isFocused ? 1 : 0 }}
        transition={{ duration: 0.4 }}
      />

      {/* Backing disc (card surface). */}
      <circle
        cx={node.x}
        cy={node.y}
        r={radius}
        fill="var(--color-cc-surface)"
        stroke="var(--color-cc-card-border)"
        strokeWidth={1}
      />

      {/* Accent ring on focus. */}
      <circle
        cx={node.x}
        cy={node.y}
        r={radius + 4}
        fill="none"
        stroke={isFocused ? "var(--color-cc-accent)" : "transparent"}
        strokeWidth={1.25}
        style={{ transition: "stroke 240ms ease" }}
      />

      {/* Pulsing outer ring on the focused node, suppressed under reduced motion. */}
      {!reduced && isFocused ? (
        <motion.circle
          cx={node.x}
          cy={node.y}
          r={radius + 4}
          fill="none"
          stroke="var(--color-cc-accent)"
          strokeWidth={1.25}
          initial={{ opacity: 0.6, scale: 1 }}
          animate={{ opacity: 0, scale: 1.5 }}
          transition={{
            duration: 1.8,
            repeat: Infinity,
            ease: "easeOut",
          }}
          style={{ transformOrigin: `${node.x}px ${node.y}px` }}
        />
      ) : null}

      {/* Inlined product icon rendered via foreignObject so the existing React SVG icons render unchanged. */}
      <foreignObject
        x={node.x - 24}
        y={node.y - 24}
        width={48}
        height={48}
        style={{ pointerEvents: "none" }}
      >
        <div
          style={{
            width: 48,
            height: 48,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <Icon style={{ width: 48, height: 48 }} />
        </div>
      </foreignObject>

      {/* Static name label below the node. */}
      <text
        x={node.x}
        y={node.y + radius + 22}
        textAnchor="middle"
        fontSize={14}
        fontWeight={600}
        fill={isFocused ? "var(--color-cc-heading)" : "var(--color-cc-ink)"}
        style={{
          fontFamily: "var(--font-heading)",
          letterSpacing: "-0.01em",
          transition: "fill 240ms ease",
        }}
      >
        {node.name}
      </text>
    </motion.g>
  );

  // Transparent hit area sized to comfortably cover the disc plus the label,
  // so the SVG anchor captures pointer and keyboard interaction without the
  // motion.g needing its own role or tabindex.
  const hitWidth = 160;
  const hitHeight = radius * 2 + 44;
  const hit = (
    <rect
      x={node.x - hitWidth / 2}
      y={node.y - radius - 6}
      width={hitWidth}
      height={hitHeight}
      rx={12}
      fill="transparent"
      style={{ cursor: "pointer" }}
    />
  );

  // Wrap the node in SVG <a>. SVG <a> is the correct anchor inside an <svg>
  // (HTML <a> wrapping <g> is not standards-clean) and gives us focus,
  // keyboard activation, and click behavior for free.
  if (node.external) {
    return (
      <a
        href={node.href}
        target="_blank"
        rel="noopener noreferrer"
        aria-label={`${node.name}: ${node.tagline}`}
        onMouseEnter={onEnter}
        onMouseLeave={onLeave}
        onFocus={onEnter}
        onBlur={onLeave}
      >
        {group}
        {hit}
      </a>
    );
  }

  return (
    <a
      href={node.href}
      aria-label={`${node.name}: ${node.tagline}`}
      onMouseEnter={onEnter}
      onMouseLeave={onLeave}
      onFocus={onEnter}
      onBlur={onLeave}
    >
      {group}
      {hit}
    </a>
  );
}

function NodeCallout({ node }: { readonly node: ProductNode }) {
  return (
    <div className="pointer-events-none absolute inset-x-0 bottom-3 flex justify-center px-4">
      <motion.div
        key={node.name}
        initial={{ opacity: 0, y: 6 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
        className="bg-cc-surface/85 border-cc-card-border max-w-md rounded-full border px-4 py-2 text-center backdrop-blur"
      >
        <span className="font-heading text-cc-heading text-sm tracking-tight">
          {node.name}
        </span>
        <span className="text-cc-ink-dim mx-2">/</span>
        <span className="text-cc-ink-dim text-xs">{node.tagline}</span>
      </motion.div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Principles
// -----------------------------------------------------------------------------

function PrinciplesTriad() {
  const reduced = useReducedMotion();
  return (
    <section className="py-16 sm:py-20">
      <header className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.2em] uppercase">
          Principles
        </p>
        <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
          How we build, every release.
        </h2>
      </header>

      <ol className="mt-12 grid gap-5 md:grid-cols-3">
        {PRINCIPLES.map((p, i) => (
          <motion.li
            key={p.title}
            initial={reduced ? false : { opacity: 0, y: 14 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-10% 0px" }}
            transition={{
              duration: 0.5,
              delay: reduced ? 0 : i * 0.08,
              ease: [0.22, 1, 0.36, 1],
            }}
            className="bg-cc-card-bg border-cc-card-border rounded-2xl border p-7"
          >
            <div className="text-cc-accent font-mono text-xs tracking-[0.2em]">
              {p.eyebrow}
            </div>
            <h3 className="font-heading text-cc-heading text-h5 mt-4 inline-block tracking-tight">
              {p.title}
              <motion.span
                aria-hidden="true"
                className="bg-cc-accent mt-2 block h-px origin-left"
                initial={reduced ? false : { scaleX: 0 }}
                whileInView={{ scaleX: 1 }}
                viewport={{ once: true, margin: "-10% 0px" }}
                transition={{
                  duration: 0.7,
                  delay: reduced ? 0 : 0.15 + i * 0.08,
                  ease: [0.22, 1, 0.36, 1],
                }}
              />
            </h3>
            <p className="text-cc-ink mt-3 text-sm leading-relaxed">{p.body}</p>
          </motion.li>
        ))}
      </ol>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Metrics
// -----------------------------------------------------------------------------

function MetricsStrip() {
  const ref = useRef<HTMLDivElement | null>(null);
  const inView = useInView(ref, { once: true, margin: "-20% 0px" });

  return (
    <section className="py-12 sm:py-16">
      <div
        ref={ref}
        className="bg-cc-card-bg border-cc-card-border grid grid-cols-1 gap-6 rounded-2xl border p-8 sm:grid-cols-2 sm:gap-4 sm:p-10"
      >
        {METRICS.map((m, i) => (
          <CountUpStat key={m.label} metric={m} run={inView} index={i} />
        ))}
      </div>
    </section>
  );
}

interface CountUpStatProps {
  readonly metric: Metric;
  readonly run: boolean;
  readonly index: number;
}

function CountUpStat({ metric, run, index }: CountUpStatProps) {
  const reduced = useReducedMotion();
  const mv = useMotionValue(reduced ? metric.value : 0);
  const rounded = useTransform(mv, (v: number) => Math.round(v));

  useEffect(() => {
    if (!run) return;
    if (reduced) {
      mv.set(metric.value);
      return;
    }
    const controls = animate(mv, metric.value, {
      duration: 1.1,
      delay: 0.1 + index * 0.12,
      ease: [0.22, 1, 0.36, 1],
    });
    return () => controls.stop();
  }, [run, reduced, mv, metric.value, index]);

  return (
    <div className="text-center sm:text-left">
      <div className="font-heading text-cc-heading text-h2 tracking-tight">
        <motion.span>{rounded}</motion.span>
        {metric.suffix ?? ""}
      </div>
      <div className="text-cc-ink-dim mt-2 text-sm">{metric.label}</div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Community
// -----------------------------------------------------------------------------

function CommunityGrid() {
  const reduced = useReducedMotion();
  return (
    <section className="py-16 sm:py-20">
      <header className="mx-auto max-w-3xl text-center">
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.2em] uppercase">
          Join the community
        </p>
        <h2 className="font-heading text-cc-heading text-h2 tracking-tight">
          The platform is built in public.
        </h2>
        <p className="text-cc-ink mt-5 text-base sm:text-lg">
          Follow along, ask questions, and contribute. These are the real
          channels where the work happens.
        </p>
      </header>

      <ul className="mt-12 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {COMMUNITY.map((item, i) => (
          <motion.li
            key={item.label}
            initial={reduced ? false : { opacity: 0, y: 10 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-10% 0px" }}
            transition={{
              duration: 0.4,
              delay: reduced ? 0 : i * 0.05,
              ease: [0.22, 1, 0.36, 1],
            }}
          >
            <CommunityCard {...item} />
          </motion.li>
        ))}
      </ul>
    </section>
  );
}

function CommunityCard({ label, href, description }: CommunityLink) {
  const classes =
    "bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover group block h-full rounded-2xl border p-5 transition-colors";

  const inner = (
    <>
      <div className="flex items-center justify-between">
        <span className="font-heading text-cc-heading text-h6 tracking-tight">
          {label}
        </span>
        <motion.span
          aria-hidden="true"
          className="text-cc-ink-dim group-hover:text-cc-accent inline-block text-base"
          whileHover={{ x: 3 }}
          transition={{ type: "spring", stiffness: 320, damping: 20 }}
        >
          &rarr;
        </motion.span>
      </div>
      <p className="text-cc-ink mt-2 text-sm leading-relaxed">{description}</p>
    </>
  );

  if (href.startsWith("/")) {
    return (
      <NextLink href={href} className={classes}>
        {inner}
      </NextLink>
    );
  }

  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className={classes}
    >
      {inner}
    </a>
  );
}

// -----------------------------------------------------------------------------
// Engage band
// -----------------------------------------------------------------------------

function EngageBand() {
  const reduced = useReducedMotion();
  return (
    <section className="py-16 sm:py-20">
      <motion.div
        initial={reduced ? false : { opacity: 0, y: 14 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, margin: "-15% 0px" }}
        transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}
        className="bg-cc-card-bg border-cc-card-border rounded-3xl border p-10 text-center sm:p-14"
      >
        <p className="text-cc-nav-label mb-3 font-mono text-xs tracking-[0.2em] uppercase">
          Work with ChilliCream
        </p>
        <h2 className="font-heading text-cc-heading text-h2 mx-auto max-w-3xl tracking-tight">
          Need GraphQL experts on your side?
        </h2>
        <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base sm:text-lg">
          From hourly advisory and architecture reviews to enterprise support
          with SLAs, we engage at the level your team needs.
        </p>
        <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/services/support/contact">Contact us</SolidButton>
          <OutlineButton href="/services">Browse services</OutlineButton>
          <OutlineButton href="/pricing">See pricing</OutlineButton>
        </div>
      </motion.div>
    </section>
  );
}
