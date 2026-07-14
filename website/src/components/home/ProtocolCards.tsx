import { PageSection } from "@/src/components/PageSection";
import { Icon, IconName } from "@/src/icons/Icon";

interface Protocol {
  readonly icon: IconName;
  readonly title: string;
  readonly subtitle: string;
  readonly tags: readonly string[];
}

const CHIPS = ["gRPC", "GraphQL", "OpenAPI", "MCP"] as const;

const PROTOCOLS: readonly Protocol[] = [
  {
    icon: "browser",
    title: "Web",
    subtitle: "SPA / MPA",
    tags: ["GraphQL"],
  },
  {
    icon: "mobile-screen",
    title: "Mobile",
    subtitle: "Android / iOS",
    tags: ["GraphQL"],
  },
  {
    icon: "robot",
    title: "AI Agents",
    subtitle: "MCP Tools",
    tags: ["GraphQL", "MCP"],
  },
  {
    icon: "handshake",
    title: "Partners",
    subtitle: "Federated API",
    tags: ["OpenAPI", "gRPC"],
  },
];

// Connector geometry in the 1000-wide layer. The chip box is narrower than the
// card grid, so the chip anchors sit inboard of the card columns; every line
// therefore runs on a diagonal and the bundle weaves on its way down.
const CHIP_X = [250, 417, 583, 750];
// Card-column centers in the 0..1000 viewBox. The tile grid is 4 columns with a
// fixed gap, so the centers sit slightly outboard of even quarters: with the
// layer at its desktop width (1056px) and gap-x-10 (40px), each column is 234px
// wide and centers land at 110.8 / 370.3 / 629.7 / 889.2. Matching these lets a
// vertical spoke meet the exact top-middle of every tile.
const CARD_X = [110.8, 370.3, 629.7, 889.2];
// The connector layer is stretched over the whole region (chip box bottom to
// the card row). LINK_Y is the y, in the 0..1000 viewBox, of the icon tiles' top
// edge, so spokes land cleanly on the top-middle of each tile.
const LINK_Y = 694;

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
    <span className="border-cc-nav-text/30 text-cc-nav-text inline-flex items-center rounded-full border px-3 py-1 font-mono text-[0.7rem] tracking-[0.12em] uppercase">
      {label}
    </span>
  );
}

/**
 * "Different protocols" section: the protocol chip box feeds connector lines
 * down through the headline and into four caller archetypes, each tile showing
 * the protocols it speaks.
 */
export function ProtocolCards() {
  return (
    <PageSection className="pb-16 sm:pb-20">
      {/* Protocol chip box. The gradient runs left→right so the centered connector
          line coming down from the Fusion section meets the midpoint color
          (#66be77) for a seamless join. */}
      <div
        className="mx-auto max-w-3xl rounded-2xl p-px"
        style={{
          backgroundImage:
            "linear-gradient(to right,#f27765 0%,#eabd21 25%,#66be77 50%,#00bce5 75%,#a983ba 100%)",
        }}
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
          className="pointer-events-none absolute inset-0 hidden h-full w-full md:block"
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
          <div
            aria-hidden="true"
            className="pointer-events-none absolute top-1/2 left-1/2 h-[74%] w-[min(980px,96vw)] -translate-x-1/2 -translate-y-1/2"
            style={{
              background:
                "radial-gradient(ellipse 50% 50% at 50% 50%, rgba(11,15,26,0.92) 0%, rgba(11,15,26,0.8) 42%, rgba(11,15,26,0.45) 68%, rgba(11,15,26,0) 88%)",
            }}
          />
          <div className="relative">
            <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 leading-[1.1] font-semibold text-balance">
              Different protocols.
              <br />
              One source.
            </h2>
            <p className="text-cc-ink mx-auto mt-6 max-w-4xl text-lg text-pretty sm:text-xl">
              Let each caller use the protocol that fits: GraphQL for apps, gRPC
              for services, OpenAPI for HTTP integrations, and MCP for agents.
              The API model stays in one place, so every surface can evolve from
              the same source.
            </p>
          </div>
        </div>

        <div className="relative grid grid-cols-2 gap-x-6 gap-y-12 sm:gap-x-10 md:grid-cols-4">
          {PROTOCOLS.map(({ icon, title, subtitle, tags }) => (
            <div key={title} className="flex flex-col items-center text-center">
              <div className="border-cc-card-border bg-cc-card-bg flex size-20 items-center justify-center rounded-2xl border">
                <Icon className="text-cc-nav-text size-11" icon={icon} />
              </div>
              <h3 className="font-heading text-cc-heading mt-5 text-xl font-semibold">
                {title}
              </h3>
              <p className="text-cc-ink mt-1 text-sm">{subtitle}</p>
              <div className="mt-4 flex flex-wrap justify-center gap-2">
                {tags.map((tag) => (
                  <ProtocolTag key={tag} label={tag} />
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </PageSection>
  );
}
