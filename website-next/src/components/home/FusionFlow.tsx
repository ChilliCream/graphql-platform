interface Source {
  readonly label: string;
  readonly color: string;
  /** Source-square anchor, in the design's 1360x8000 coordinate space. */
  readonly x: number;
  readonly y: number;
  /** Exact stream path lifted from the design SVG (design coordinates). */
  readonly d: string;
}

// Streams lifted verbatim from the design SVG: each path is the rounded source
// square plus the curving ribbon that flows down to the convergence point
// (680, 4461.9). Staggered exactly as designed (Catalog highest, then Shipping,
// Billing, Ordering, User lowest).
const SOURCES: readonly Source[] = [
  {
    label: "Catalog",
    color: "#f27765",
    x: 248.3,
    y: 3342.2,
    d: "M248.32,3342.2c2.21,0,4,1.79,4,4v11.61c0,2.21-1.79,4-4,4h-3.92v364.52c0,48.51,7.63,96.04,22.68,141.27,13.52,40.62,34.17,81.3,61.39,120.91,30.42,44.26,68.46,86.2,113.05,124.63,66.77,57.55,111.02,100.15,143.47,138.11,28.75,33.63,49.27,64.91,68.6,104.58,16.07,32.97,25.52,69.86,27.38,106.8h-2.78c-1.85-36.52-11.21-72.99-27.09-105.58-19.23-39.45-39.63-70.54-68.22-103.99-32.36-37.85-76.52-80.36-143.18-137.81-44.77-38.59-82.97-80.7-113.52-125.16-27.37-39.82-48.14-80.73-61.74-121.6-15.15-45.51-22.82-93.33-22.82-142.15v-364.52h-3.92c-2.21,0-4-1.79-4-4v-11.61c0-2.21,1.79-4,4-4h10.61Z",
  },
  {
    label: "Billing",
    color: "#eabd21",
    x: 446.0,
    y: 3456.6,
    d: "M445.98,3456.63c2.21,0,4,1.79,4,4v11.61c0,2.21-1.79,4-4,4h-3.84s-.08.03-.08.08v249.33c0,53.74,2.96,102.73,8.81,145.6,5.12,37.57,11.42,64.55,21.73,93.11,11.22,31.07,27.7,64.79,53.43,109.34,13.21,22.86,26.95,42.96,40.24,62.4,27.23,39.83,55.39,81.01,82.47,149.96,18.83,47.95,30.09,131.33,32.22,176.48,0,.04-.03.08-.08.08h-2.62s-.08-.03-.08-.07c-2.12-44.85-13.32-127.81-32.03-175.47-26.96-68.66-55.03-109.71-82.17-149.41-13.32-19.48-27.09-39.63-40.35-62.58-25.82-44.7-42.37-78.55-53.64-109.78-10.38-28.76-16.72-55.9-21.87-93.68-5.86-43-8.84-92.11-8.84-145.98v-249.33s-.03-.08-.08-.08h-3.84c-2.21,0-4-1.79-4-4v-11.61c0-2.21,1.79-4,4-4h10.61Z",
  },
  {
    label: "Ordering",
    color: "#66be77",
    x: 591.6,
    y: 3612.7,
    d: "M591.62,3612.67c2.21,0,4,1.79,4,4v11.61c0,2.21-1.79,4-4,4h-3.92v93.36c0,67.59,2.81,197.14,21.65,305.45,8.51,48.94,24.17,103.9,37.99,152.4,13.75,48.25,25.62,89.93,29.19,119.24,5.58,45.91,4.97,103.54,4.53,145.62v.29c-.05,4.82-.1,9.5-.14,13.96h-2.78c.04-4.56.09-9.35.14-14.28.44-42,1.05-99.52-4.51-145.25-3.54-29.1-15.95-72.68-29.1-118.83-13.83-48.55-29.51-103.58-38.05-152.67-8.8-50.58-15.04-110.85-18.54-179.12-2.09-40.78-3.15-83.45-3.15-126.81v-93.36h-3.92c-2.21,0-4-1.79-4-4v-11.61c0-2.21,1.79-4,4-4h10.61Z",
  },
  {
    label: "Shipping",
    color: "#00bce5",
    x: 893.3,
    y: 3428.6,
    d: "M893.31,3428.64c2.21,0,4,1.79,4,4v11.61c0,2.21-1.79,4-4,4h-3.92v277.39c0,49.37-4.49,95.84-13.72,142.06-21.14,105.85-46.06,198.34-98.98,305.7l-.02.05c-21.71,39.79-44.59,82.66-64.86,126.46-9.73,21.02-16.67,43.06-20.63,65.52-5.83,33.04-9.01,64.85-9.7,97.19h-2.78c.69-32.5,3.88-64.47,9.74-97.67,4.01-22.69,11.02-44.97,20.85-66.2,20.3-43.86,43.19-86.75,64.91-126.57v-.03s.03-.03.03-.03c52.77-107.06,77.63-199.34,98.72-304.96,9.19-46.04,13.66-92.33,13.66-141.52v-277.39h-3.92c-2.21,0-4-1.79-4-4v-11.61c0-2.21,1.79-4,4-4h10.61Z",
  },
  {
    label: "User",
    color: "#a983ba",
    x: 1122.2,
    y: 3654.3,
    d: "M1122.17,3654.28c2.21,0,4,1.79,4,4v11.61c0,2.21-1.79,4-4,4h-3.92v52.43c0,48.81-7.68,96.64-22.82,142.15-13.6,40.87-34.37,81.78-61.74,121.6-30.56,44.46-68.75,86.57-113.52,125.16-66.66,57.46-110.82,99.96-143.18,137.81-28.59,33.44-48.99,64.54-68.22,103.99-15.88,32.59-25.24,69.06-27.09,105.58h-2.78c1.85-36.94,11.31-73.83,27.38-106.8,19.34-39.67,39.85-70.94,68.6-104.58,32.45-37.96,76.7-80.56,143.47-138.11,44.59-38.43,82.63-80.37,113.05-124.63,27.22-39.6,47.88-80.28,61.39-120.91,15.05-45.23,22.68-92.75,22.68-141.27v-52.43h-3.92c-2.21,0-4-1.79-4-4v-11.61c0-2.21,1.79-4,4-4h10.61Z",
  },
];

// Design coordinate window cropped to the streams band, plus a tail below the
// convergence point so the composition's single output line runs all the way to
// the next section. The convergence point (the glowing composition node) sits
// within it.
const VIEW = { x: 0, y: 3320, w: 1360, h: 1420 };
const GLOW = { x: 680, y: 4461.9 };

// Each source's `x` is the design square's anchor, but the stream's descending
// stem runs 5.3 design units to its left. Centering the bright marker (and its
// label) on the stem keeps the square square and the stem flush under it.
const STEM_DX = -5.3;

const pctX = (x: number) => `${((x - VIEW.x) / VIEW.w) * 100}%`;
const pctY = (y: number) => `${((y - VIEW.y) / VIEW.h) * 100}%`;

/**
 * The Fusion narrative, rendered as a tall top-to-bottom "river": independent
 * subgraphs (Catalog, Billing, ...) flow down and converge into a single
 * glowing composition node, which then speaks every protocol (gRPC, GraphQL,
 * OpenAPI, MCP) and fans back out to every consumer.
 *
 * The stream/glow artwork is lifted verbatim from the design SVG (its 1360x8000
 * coordinate space, cropped via `VIEW`), while the headings and source labels
 * are real HTML positioned by the same coordinates so they stay crisp.
 */
export function FusionFlow() {
  return (
    <section className="mx-auto max-w-6xl px-5 sm:px-12">
      {/* Built apart / queried together */}
      <div className="py-16 text-center sm:py-20">
        <h2 className="font-heading text-cc-heading text-h2 sm:text-h1 leading-[1.05] font-semibold text-balance">
          Built apart.
          <br />
          Queried together.
        </h2>
        <p className="text-cc-ink mx-auto mt-6 max-w-4xl text-lg text-pretty sm:text-xl">
          Let teams split the backend where it makes sense: catalog, billing,
          orders, shipping, identity. Fusion composes the service contracts into
          one API, so apps keep one place to query while each service can keep
          moving on its own.
        </p>
      </div>

      {/* Sources converge into the composition node. */}
      <div className="relative mx-auto aspect-[1360/1420] w-full">
        <svg
          viewBox={`${VIEW.x} ${VIEW.y} ${VIEW.w} ${VIEW.h}`}
          fill="none"
          aria-hidden="true"
          className="absolute inset-0 h-full w-full"
        >
          <defs>
            <radialGradient
              id="ff-halo-soft"
              cx={GLOW.x}
              cy={GLOW.y}
              fx={GLOW.x}
              fy={GLOW.y}
              r={180}
              gradientUnits="userSpaceOnUse"
            >
              <stop offset="0.09" stopColor="#fff" stopOpacity="0.9" />
              <stop offset="1" stopColor="#0e1522" stopOpacity="0" />
            </radialGradient>
            <radialGradient
              id="ff-halo-core"
              cx={GLOW.x}
              cy={GLOW.y}
              fx={GLOW.x}
              fy={GLOW.y}
              r={180}
              gradientUnits="userSpaceOnUse"
            >
              <stop offset="0" stopColor="#fff" />
              <stop offset="0.4" stopColor="#0e1522" stopOpacity="0" />
            </radialGradient>
            {/* The composed graph leaving the node: one rainbow line carrying the
                stream colors back out, then fading to transparent as it reaches
                the copy below so it dissolves into the heading instead of
                stopping hard. */}
            <linearGradient
              id="ff-out"
              x1={GLOW.x}
              y1={GLOW.y}
              x2={GLOW.x}
              y2={VIEW.y + VIEW.h}
              gradientUnits="userSpaceOnUse"
            >
              <stop offset="0" stopColor="#f27765" />
              <stop offset="0.55" stopColor="#eabd21" />
              <stop offset="0.78" stopColor="#66be77" />
              <stop offset="1" stopColor="#66be77" stopOpacity="0" />
            </linearGradient>
          </defs>

          {/* The streams (with their built-in squares) sit at the bottom layer
              so the scrim can dim them behind the copy. */}
          {SOURCES.map((s) => (
            <path key={s.label} d={s.d} fill={s.color} />
          ))}

          {/* Output line: leaves the composition node going straight down, the
              way the five colored streams arrive into it. Drawn before the glow so
              the halo sits on top and the line appears to emerge from the node. */}
          <rect
            x={GLOW.x - 0.75}
            y={GLOW.y}
            width={1.5}
            height={VIEW.y + VIEW.h - GLOW.y}
            fill="url(#ff-out)"
          />

          <circle cx={GLOW.x} cy={GLOW.y} r={180} fill="url(#ff-halo-soft)" />
          <circle cx={GLOW.x} cy={GLOW.y} r={180} fill="url(#ff-halo-core)" />
          <circle cx={GLOW.x} cy={GLOW.y} r={15} fill="#fff" />
        </svg>

        {/* Soft dark scrim that ONLY dims the streams behind the copy. It sits
            above the lines but below the squares, labels and headline, so those
            stay at full strength. Broad and feathered, as in the design. */}
        <div
          aria-hidden="true"
          className="pointer-events-none absolute top-[36%] left-1/2 h-[52%] w-[min(1500px,96vw)] -translate-x-1/2 -translate-y-1/2"
          style={{
            background:
              "radial-gradient(ellipse 50% 50% at 50% 50%, rgba(11,15,26,0.9) 0%, rgba(11,15,26,0.82) 38%, rgba(11,15,26,0.5) 62%, rgba(11,15,26,0) 85%)",
          }}
        />

        {/* Larger square markers, redrawn on top of the scrim so they stay
            bright (and bigger than the design's hairline path squares). */}
        <svg
          viewBox={`${VIEW.x} ${VIEW.y} ${VIEW.w} ${VIEW.h}`}
          fill="none"
          aria-hidden="true"
          className="pointer-events-none absolute inset-0 h-full w-full"
        >
          {SOURCES.map((s) => (
            <rect
              key={s.label}
              x={s.x + STEM_DX - 11}
              y={s.y - 1}
              width={22}
              height={22}
              rx={5}
              fill={s.color}
            />
          ))}
        </svg>

        {/* Source labels (HTML for crisp text), on top of the scrim. Sized to
            sit flush with the top and bottom of the square. */}
        {SOURCES.map((s) => (
          <div
            key={s.label}
            className="text-cc-ink-dim absolute hidden -translate-y-1/2 font-mono text-base leading-none tracking-[0.15em] whitespace-nowrap uppercase sm:block sm:text-xl"
            style={{ left: pctX(s.x + STEM_DX + 28), top: pctY(s.y + 10) }}
          >
            {s.label}
          </div>
        ))}

        {/* "No matter the format" sits over the stream region. It breaks out of
            the section's max width so the line stays single, as in the design. */}
        <div className="absolute top-[36%] left-1/2 w-screen max-w-[100vw] -translate-x-1/2 -translate-y-1/2 px-4 text-center">
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 font-semibold sm:whitespace-nowrap">
            Compose before it runs.
          </h2>
          <p className="text-cc-ink mx-auto mt-6 max-w-4xl px-4 text-lg text-pretty sm:text-xl">
            Each part publishes its contract. Fusion checks that the pieces fit,
            catches missing lookups and incompatible fields, and produces the
            gateway artifact your runtime loads.
          </p>
        </div>

        {/* "Fusion Composition" caption with a dashed line running to the glow. */}
        <div
          className="absolute right-1/2 left-0 hidden -translate-y-1/2 items-center gap-4 pr-10 sm:flex"
          style={{ top: pctY(GLOW.y) }}
        >
          <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] whitespace-nowrap uppercase sm:text-sm">
            Fusion Composition
          </span>
          <span className="h-px flex-1 border-t border-dashed border-[rgba(245,241,234,0.3)]" />
        </div>
      </div>

      {/* One API for every consumer. The composition's output line (in the box
          above) now runs the whole way down to this heading; a single line then
          continues below the copy and on into the protocol box. */}
      <div className="text-center">
        <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 font-semibold text-balance">
          One API for every consumer.
        </h2>
        <p className="text-cc-ink mx-auto mt-6 max-w-4xl text-lg text-pretty sm:text-xl">
          Apps, tools, and agents ask for what they need through one gateway.
          Fusion plans the request across the backend and returns one response.
        </p>
        {/* Line below the copy, on into the protocol box. It fades in from the
            copy and settles on #66be77 (green), the color the box shows where the
            centered line meets it. */}
        <div
          aria-hidden="true"
          className="mx-auto mt-10 h-28 w-px sm:h-36"
          style={{
            backgroundImage:
              "linear-gradient(to bottom, transparent 0%, #66be77 30%, #66be77 100%)",
          }}
        />
      </div>
    </section>
  );
}
