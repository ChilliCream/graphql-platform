interface Source {
  readonly label: string;
  readonly color: string;
  /** Center x as a percentage of the band width. */
  readonly x: number;
  /** Top offset as a percentage of the band height (the stagger). */
  readonly top: number;
}

const SOURCES: readonly Source[] = [
  { label: "Catalog", color: "#f27765", x: 11, top: 6 },
  { label: "Billing", color: "#eabd21", x: 30.5, top: 14 },
  { label: "Ordering", color: "#66be77", x: 50, top: 22 },
  { label: "Shipping", color: "#36c6e0", x: 69.5, top: 14 },
  { label: "User", color: "#b07fd0", x: 89, top: 6 },
];

/**
 * Long vertical drops that sweep inward to the convergence point (500,770) in
 * the 1000x820 band, mirroring the design's tall "river" of subgraph streams.
 */
const CONVERGENCE: readonly { color: string; d: string }[] = [
  { color: "#f27765", d: "M110 70 L110 300 C110 520 500 560 500 770" },
  { color: "#eabd21", d: "M305 135 L305 360 C305 560 500 600 500 770" },
  { color: "#66be77", d: "M500 195 L500 770" },
  { color: "#36c6e0", d: "M695 135 L695 360 C695 560 500 600 500 770" },
  { color: "#b07fd0", d: "M890 70 L890 300 C890 520 500 560 500 770" },
];

const CHIPS = ["gRPC", "GraphQL", "OpenAPI", "MCP"] as const;

/** Fan-out from the chip box down to the four protocol columns below. */
const FANOUT: readonly string[] = [
  "M500 0 C500 90 125 70 125 180",
  "M500 0 C500 110 375 70 375 180",
  "M500 0 C500 110 625 70 625 180",
  "M500 0 C500 90 875 70 875 180",
];

/** Brand spectrum used for the headline accent and the chip-box border. */
const SPECTRUM =
  "linear-gradient(100deg,#16b9e4 0%,#7c92c6 33%,#b681a9 63%,#f0786a 100%)";

function GlowBeam({ className }: { readonly className?: string }) {
  return (
    <div
      aria-hidden="true"
      className={`mx-auto h-16 w-px bg-gradient-to-b from-[rgba(245,241,234,0.7)] to-transparent sm:h-24 ${className ?? ""}`}
    />
  );
}

/**
 * The Fusion narrative, rendered as a tall top-to-bottom "river": independent
 * subgraphs (Catalog, Billing, ...) flow down and converge into a single
 * glowing composition node, which then speaks every protocol (gRPC, GraphQL,
 * OpenAPI, MCP) and fans back out to every consumer.
 *
 * The line art is SVG (scaled by `viewBox`), while every label and heading is
 * real HTML positioned by the same percentage columns the SVG uses, so the
 * graphic stays aligned and legible at any width. The decorative fan-out lines
 * drop away on small screens where the layout reflows.
 */
export function FusionFlow() {
  return (
    <section className="mx-auto max-w-6xl px-5 sm:px-12">
      {/* Built apart / queried together */}
      <div className="py-16 text-center sm:py-20">
        <h2 className="font-heading text-cc-ink text-h4 sm:text-h3 leading-[1.1] font-semibold text-balance">
          Built apart.
          <br />
          Queried together.
        </h2>
        <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base text-pretty sm:text-lg">
          Unify all your APIs into a comprehensive company graph, streamlining
          data accessibility and enhancing integration. Transform the way you
          manage and interact with your data.
        </p>
      </div>

      {/* Sources converge into the composition node */}
      <div className="relative mx-auto aspect-[1000/780] w-full sm:aspect-[1000/820]">
        {/* Source labels (HTML), aligned to the SVG columns. */}
        {SOURCES.map((s) => (
          <div
            key={s.label}
            className="absolute flex -translate-x-1/2 -translate-y-7 items-center gap-1.5 sm:gap-2"
            style={{ left: `${s.x}%`, top: `${s.top}%` }}
          >
            <span
              className="size-2 flex-none rounded-[3px] sm:size-2.5"
              style={{ backgroundColor: s.color }}
            />
            <span className="text-cc-ink-dim font-mono text-[0.6rem] tracking-[0.12em] uppercase sm:text-xs">
              {s.label}
            </span>
          </div>
        ))}

        {/* Converging line art. */}
        <svg
          viewBox="0 0 1000 820"
          fill="none"
          aria-hidden="true"
          preserveAspectRatio="none"
          className="absolute inset-0 h-full w-full"
        >
          {CONVERGENCE.map((c) => (
            <path
              key={c.color}
              d={c.d}
              stroke={c.color}
              strokeOpacity={0.7}
              strokeWidth={1.5}
              vectorEffect="non-scaling-stroke"
            />
          ))}
        </svg>

        {/* "No matter the format" sits in the clear center band. */}
        <div className="absolute inset-x-0 top-[46%] px-4 text-center">
          <h2 className="font-heading text-cc-ink text-h5 sm:text-h4 font-semibold text-balance [text-shadow:0_2px_24px_#0b0f1a]">
            No Matter the Format. Fusion transforms.
          </h2>
        </div>

        {/* Composition glow + label at the convergence point. */}
        <div className="absolute top-[94%] left-1/2 -translate-x-1/2 -translate-y-1/2">
          <div className="relative grid place-items-center">
            <div
              className="size-44 rounded-full sm:size-64"
              style={{
                background:
                  "radial-gradient(circle, rgba(255,255,255,0.95) 0%, rgba(214,224,255,0.35) 14%, rgba(124,146,198,0.12) 34%, transparent 64%)",
              }}
            />
            <span className="absolute size-1.5 rounded-full bg-white shadow-[0_0_14px_5px_rgba(255,255,255,0.85)]" />
          </div>
        </div>
        <div className="absolute top-[94%] left-0 hidden -translate-y-1/2 items-center gap-2 sm:flex">
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.15em] uppercase">
            Fusion Composition
          </span>
          <span className="h-px w-16 border-t border-dashed border-[rgba(245,241,234,0.3)] lg:w-28" />
        </div>
      </div>

      {/* The API that speaks any language */}
      <div className="text-center">
        <GlowBeam />
        <h2 className="font-heading text-cc-ink text-h4 sm:text-h3 font-semibold text-balance">
          The API that speaks any Language.
        </h2>
        <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base text-pretty sm:text-lg">
          Unify all your APIs into a comprehensive company graph, streamlining
          data accessibility and enhancing integration. Transform the way you
          manage and interact with your data.
        </p>
        <GlowBeam className="mt-10" />
      </div>

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

      {/* Fan-out toward the protocol cards below. */}
      <svg
        viewBox="0 0 1000 180"
        fill="none"
        aria-hidden="true"
        preserveAspectRatio="none"
        className="mt-px hidden h-24 w-full lg:block"
      >
        {FANOUT.map((d) => (
          <path
            key={d}
            d={d}
            stroke="rgba(245,241,234,0.22)"
            strokeWidth={1.5}
            vectorEffect="non-scaling-stroke"
          />
        ))}
      </svg>
    </section>
  );
}
