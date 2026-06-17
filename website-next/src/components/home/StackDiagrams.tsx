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

/**
 * Hub-and-spoke graph used by both feature rows: one larger node in the center,
 * three satellites above and three below, joined by S-curves that gather into
 * the center node's top and bottom edges. Matches the source design's node art.
 */
function Hub({ className }: { readonly className?: string }) {
  const cx = 180;
  const cy = 185;
  const topY = 58;
  const botY = 312;
  const cols = [80, 180, 280];

  return (
    <svg
      viewBox="0 0 360 370"
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
        {cols.map((x) => (
          <path
            key={`t${x}`}
            d={`M180 167 C180 120 ${x} 120 ${x} ${topY + 12}`}
          />
        ))}
        {cols.map((x) => (
          <path
            key={`b${x}`}
            d={`M180 203 C180 250 ${x} 250 ${x} ${botY - 12}`}
          />
        ))}
      </g>

      {cols.map((x) => (
        <Node key={`tn${x}`} x={x} y={topY} w={32} h={24} />
      ))}
      <Node x={cx} y={cy} w={54} h={40} />
      {cols.map((x) => (
        <Node key={`bn${x}`} x={x} y={botY} w={32} h={24} />
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
      className={`flex flex-col items-center gap-8 lg:gap-20 ${
        reverse ? "lg:flex-row-reverse" : "lg:flex-row"
      }`}
    >
      <div className="max-w-md flex-1 text-center lg:text-left">
        <h3 className="font-heading text-cc-ink text-h5 sm:text-h4 font-semibold">
          {title}
        </h3>
        <p className="text-cc-prose mt-4 text-base/relaxed">
          Pick what you need for your stack. Whether you&rsquo;re building a
          monolithic or federated GraphQL API, a message-intensive service, or
          an application, we&rsquo;ve got the right tools for you.
        </p>
      </div>
      <div className="w-full max-w-sm flex-1">{diagram}</div>
    </div>
  );
}

/**
 * Two alternating feature rows that introduce the platform's shape: one graph
 * for the "Platform" story and a mirrored one for the "Agentic Era".
 */
export function StackDiagrams() {
  return (
    <section className="mx-auto flex max-w-6xl flex-col gap-20 px-5 py-16 sm:px-12 sm:py-24 lg:gap-28">
      <StackRow title="Platform" diagram={<Hub className="h-auto w-full" />} />
      <StackRow
        title="Agentic Era"
        diagram={<Hub className="h-auto w-full" />}
        reverse
      />
    </section>
  );
}
