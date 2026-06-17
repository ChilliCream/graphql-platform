import type { ComponentType } from "react";

import { BrowserIcon } from "@/src/icons/BrowserIcon";
import { HandshakeIcon } from "@/src/icons/HandshakeIcon";
import { PhoneIcon } from "@/src/icons/PhoneIcon";
import { RobotIcon } from "@/src/icons/RobotIcon";

interface IconProps {
  readonly className?: string;
}

interface Protocol {
  readonly Icon: ComponentType<IconProps>;
  readonly title: string;
  readonly subtitle: string;
  readonly tags: readonly string[];
}

const CHIPS = ["gRPC", "GraphQL", "OpenAPI", "MCP"] as const;

/** Brand spectrum used for the chip-box border. */
const SPECTRUM =
  "linear-gradient(100deg,#16b9e4 0%,#7c92c6 33%,#b681a9 63%,#f0786a 100%)";

const PROTOCOLS: readonly Protocol[] = [
  {
    Icon: BrowserIcon,
    title: "Web",
    subtitle: "Browser SPA",
    tags: ["GraphQL"],
  },
  {
    Icon: PhoneIcon,
    title: "Mobile",
    subtitle: "Android / iOS",
    tags: ["GraphQL"],
  },
  {
    Icon: RobotIcon,
    title: "AI Agents",
    subtitle: "MCP Tools",
    tags: ["GraphQL", "gRPC"],
  },
  {
    Icon: HandshakeIcon,
    title: "Partners",
    subtitle: "Federated API",
    tags: ["OpenAPI", "gRPC"],
  },
];

// Connector geometry in the 1000-wide layer. The chip box is narrower than the
// card grid, so the chip anchors sit inboard of the card columns; every line
// therefore runs on a diagonal and the bundle weaves on its way down.
const CHIP_X = [250, 417, 583, 750];
const CARD_X = [125, 375, 625, 875];
// The connector layer is stretched over the whole region (chip box bottom to
// the card row). LINK_Y is the y, in the 0..1000 viewBox, where the spokes meet
// the icon tiles; tuned against the icon row on desktop.
const LINK_Y = 720;

/**
 * One spoke per (card, protocol) pairing, derived from each card's tags. The
 * line drops vertically out of the chip, crosses through the middle, and rises
 * vertically into the destination icon tile.
 */
const LINKS = PROTOCOLS.flatMap((card, to) =>
  card.tags
    .map((tag) => CHIPS.indexOf(tag as (typeof CHIPS)[number]))
    .filter((from) => from >= 0)
    .map((from) => {
      const x0 = CHIP_X[from];
      const x1 = CARD_X[to];
      const mid = LINK_Y / 2;
      return {
        key: `${from}-${to}`,
        d: `M ${x0} 0 C ${x0} ${mid} ${x1} ${mid} ${x1} ${LINK_Y}`,
      };
    }),
);

function ProtocolTag({ label }: { readonly label: string }) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim inline-flex items-center rounded-full border px-3 py-1 font-mono text-[0.7rem] tracking-[0.12em] uppercase">
      {label}
    </span>
  );
}

/**
 * "Choose your Protocol" section: the protocol chip box feeds connector lines
 * down through the headline and into four consumer archetypes (Web, Mobile, AI
 * Agents, Partners), each tile showing the protocols it speaks.
 */
export function ProtocolCards() {
  return (
    <section className="mx-auto max-w-6xl px-5 py-16 sm:px-12 sm:py-20">
      {/* Protocol chip box with the brand-spectrum border. */}
      <div
        className="mx-auto max-w-3xl rounded-2xl p-px"
        style={{ backgroundImage: SPECTRUM }}
      >
        <div className="bg-cc-surface grid grid-cols-2 gap-3 rounded-[15px] p-3 sm:grid-cols-4 sm:p-4">
          {CHIPS.map((chip) => (
            <div
              key={chip}
              className="border-cc-card-border/70 bg-cc-card-bg text-cc-ink-dim rounded-xl border px-4 py-3 text-center font-mono text-sm"
            >
              {chip}
            </div>
          ))}
        </div>
      </div>

      {/* Connector layer + headline + consumer tiles share one positioning
          context so the spokes can run from the chip box into each tile. */}
      <div className="relative">
        <svg
          viewBox="0 0 1000 1000"
          fill="none"
          aria-hidden="true"
          preserveAspectRatio="none"
          className="pointer-events-none absolute inset-0 hidden h-full w-full lg:block"
        >
          {LINKS.map((link) => (
            <path
              key={link.key}
              d={link.d}
              stroke="rgba(245,241,234,0.22)"
              strokeWidth={1.5}
              vectorEffect="non-scaling-stroke"
            />
          ))}
        </svg>

        <div className="relative px-4 pt-16 pb-20 text-center sm:pt-24 sm:pb-28">
          <h2 className="font-heading text-cc-ink text-h4 sm:text-h3 leading-[1.1] font-semibold text-balance">
            Choose your Protocol.
            <br />
            One Source of Truth.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base text-pretty sm:text-lg">
            Unify all your APIs into a comprehensive company graph, streamlining
            data accessibility and enhancing integration. Transform the way you
            manage and interact with your data.
          </p>
        </div>

        <div className="relative grid grid-cols-2 gap-x-6 gap-y-12 sm:gap-x-10 lg:grid-cols-4">
          {PROTOCOLS.map(({ Icon, title, subtitle, tags }) => (
            <div key={title} className="flex flex-col items-center text-center">
              <div className="border-cc-card-border bg-cc-card-bg flex size-20 items-center justify-center rounded-2xl border">
                <Icon className="text-cc-nav-text size-9" />
              </div>
              <h3 className="font-heading text-cc-ink mt-5 text-xl font-semibold">
                {title}
              </h3>
              <p className="text-cc-ink-dim mt-1 text-sm">{subtitle}</p>
              <div className="mt-4 flex flex-wrap justify-center gap-2">
                {tags.map((tag) => (
                  <ProtocolTag key={tag} label={tag} />
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
