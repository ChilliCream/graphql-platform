import type { ReactNode } from "react";

const EDGE = "rgba(236,232,245,0.55)";

interface NodeBox {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
}

function Node({ x, y, w, h }: NodeBox) {
  return (
    <rect
      x={x - w / 2}
      y={y - h / 2}
      width={w}
      height={h}
      rx={8}
      fill="#e3dfee"
      filter="url(#node-shadow)"
    />
  );
}

interface Leg {
  /** Satellite (node) center. */
  readonly x: number;
  readonly y: number;
  /** Where the spoke attaches to the hub perimeter. */
  readonly ax: number;
  readonly ay: number;
}

/**
 * Hub-and-spoke graph used by both feature rows: one larger node in the center
 * with three satellites above and three below. The six spokes radiate from
 * points spread around the hub's perimeter (left side, top, right side, ...)
 * like an asterisk, each leaving the hub along its own diagonal and arriving at
 * its satellite as a short vertical stub.
 */
function Hub({ className }: { readonly className?: string }) {
  const cx = 210;
  const cy = 190;
  const hubW = 56;
  const hubH = 42;
  const satW = 34;
  const satH = 26;
  const topY = 40;
  const botY = 340;
  const cols = [56, 210, 364];

  const hubTop = cy - hubH / 2;
  const hubBot = cy + hubH / 2;
  const hubLeft = cx - hubW / 2;
  const hubRight = cx + hubW / 2;
  // Upper/lower attach points on the hub's side edges for the diagonal legs.
  const sideUp = cy - hubH / 4;
  const sideDown = cy + hubH / 4;

  const topRow = topY + satH / 2;
  const botRow = botY - satH / 2;

  // Attach the centre legs to the top/bottom edge and the diagonal legs to the
  // side edges, so the spokes splay around the box instead of pinching to a
  // single point.
  const legs: readonly Leg[] = [
    { x: cols[0], y: topRow, ax: hubLeft, ay: sideUp },
    { x: cols[1], y: topRow, ax: cx, ay: hubTop },
    { x: cols[2], y: topRow, ax: hubRight, ay: sideUp },
    { x: cols[0], y: botRow, ax: hubLeft, ay: sideDown },
    { x: cols[1], y: botRow, ax: cx, ay: hubBot },
    { x: cols[2], y: botRow, ax: hubRight, ay: sideDown },
  ];

  // Cubic from the hub anchor to the satellite: the first control point pushes
  // out along the anchor->node diagonal (radial departure); the second sits
  // directly above/below the node so the line lands as a short vertical stub.
  const spoke = ({ x, y, ax, ay }: Leg) => {
    const stub = y < cy ? 40 : -40;
    const c1x = ax + (x - ax) * 0.55;
    const c1y = ay + (y - ay) * 0.2;
    return `M ${ax} ${ay} C ${c1x} ${c1y} ${x} ${y + stub} ${x} ${y}`;
  };

  return (
    <svg
      viewBox="0 0 420 380"
      fill="none"
      aria-hidden="true"
      className={className}
    >
      <defs>
        <filter id="node-shadow" x="-40%" y="-40%" width="180%" height="180%">
          <feDropShadow
            dx="0"
            dy="3"
            stdDeviation="4"
            floodColor="#000"
            floodOpacity="0.35"
          />
        </filter>
      </defs>

      <g stroke={EDGE} strokeWidth={1.5}>
        {legs.map((leg) => (
          <path key={`${leg.x}-${leg.y}`} d={spoke(leg)} />
        ))}
      </g>

      {cols.map((x) => (
        <Node key={`tn${x}`} x={x} y={topY} w={satW} h={satH} />
      ))}
      <Node x={cx} y={cy} w={hubW} h={hubH} />
      {cols.map((x) => (
        <Node key={`bn${x}`} x={x} y={botY} w={satW} h={satH} />
      ))}
    </svg>
  );
}

interface StackRowProps {
  readonly title: string;
  readonly diagram: ReactNode;
  /** When true, the diagram sits on the left and the copy on the right. */
  readonly reverse?: boolean;
}

function StackRow({ title, diagram, reverse }: StackRowProps) {
  return (
    <div
      className={`flex flex-col items-center gap-8 md:gap-20 ${
        reverse ? "md:flex-row-reverse" : "md:flex-row"
      }`}
    >
      <div className="max-w-md flex-1 text-center md:text-left">
        <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold">
          {title}
        </h3>
        <p className="text-cc-ink mt-4 text-base/relaxed">
          Pick what you need for your stack. Whether you&rsquo;re building a
          monolithic or federated GraphQL API, a message-intensive service, or
          an application, we&rsquo;ve got the right tools for you.
        </p>
      </div>
      <div className="flex flex-1 justify-center">
        <div className="w-full max-w-sm">{diagram}</div>
      </div>
    </div>
  );
}

/**
 * Two alternating feature rows that introduce the platform's shape: one graph
 * for the "Platform" story and a mirrored one for the "Agentic Era".
 */
export function StackDiagrams() {
  return (
    <section className="mx-auto flex max-w-7xl flex-col gap-20 px-5 py-16 sm:px-12 sm:py-24 lg:gap-28">
      <StackRow title="Platform" diagram={<Hub className="h-auto w-full" />} />
      <StackRow
        title="Agentic Era"
        diagram={<Hub className="h-auto w-full" />}
        reverse
      />
    </section>
  );
}
