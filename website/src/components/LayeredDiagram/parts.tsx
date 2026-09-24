import type { CSSProperties } from "react";

import { TYPE } from "./tokens";
import { MC } from "./palette";
import { BUS_Y } from "./diagram";
import type { BandFlow, Lane } from "./diagram";

/**
 * The pieces the Layered Diagram hero is drawn from: a panel card, the
 * orthogonal connector between a tier and the gateway, and the request pulse
 * that runs along it.
 */

/** Line height for the diagram's card and gateway text. */
export const LINE_HEIGHT = 1.3;

/** Corner radius of a connector elbow, in px. */
const ELBOW_RADIUS = 12;

/** A palette role at `percent` opacity. */
export function wash(color: string, percent: number): string {
  return `color-mix(in srgb, ${color} ${percent}%, transparent)`;
}

interface CardProps {
  readonly title: string;
  readonly detail: string;
  /** Spec badge or wire protocol; the clients carry none. */
  readonly badge?: string;
  readonly badgeColor?: string;
  readonly lit: boolean;
  /** Colour of the traffic that lights the card. */
  readonly tone: string;
}

/** One panel in a tier: a name, what it is, and the badge it carries. */
export function Card({
  title,
  detail,
  badge,
  badgeColor,
  lit,
  tone,
}: CardProps) {
  const tight = badge ? "@max-[359px]:px-1 @max-[312px]:px-0" : "";

  return (
    <div
      className={`rounded-lg border px-2 py-1.5 text-center transition-colors duration-500 md:py-2 ${tight}`}
      style={{
        background: MC.panel,
        borderColor: lit ? wash(tone, 80) : MC.panelEdge,
        boxShadow: lit ? `0 0 22px ${wash(tone, 20)}` : "none",
      }}
    >
      <p
        className="truncate font-mono @max-[312px]:whitespace-normal"
        style={{
          color: lit ? tone : MC.ink,
          fontSize: TYPE.caption,
          lineHeight: LINE_HEIGHT,
        }}
      >
        {title}
      </p>
      <p
        className="truncate font-mono @max-[312px]:whitespace-normal"
        style={{
          color: MC.dim,
          fontSize: TYPE.label,
          lineHeight: LINE_HEIGHT,
        }}
      >
        {detail}
      </p>
      {badge ? (
        <p
          className="mt-1 truncate rounded-sm px-1 font-mono tracking-[0.04em] @max-[359px]:px-0.5 @max-[359px]:tracking-[0.01em] @max-[312px]:whitespace-normal"
          style={{
            background: wash(badgeColor ?? MC.dim, 14),
            color: badgeColor ?? MC.dim,
            fontSize: TYPE.label,
            lineHeight: LINE_HEIGHT,
          }}
        >
          {badge}
        </p>
      ) : null}
    </div>
  );
}

interface ElbowProps {
  readonly lane: Lane;
  readonly flow: BandFlow;
  readonly tone: string;
}

/**
 * One orthogonal connector with a rounded corner: a vertical run at the
 * lane's column and a horizontal run to the gateway's centre line, drawn as
 * two borders of one box so the corner rounds itself.
 */
export function Elbow({ lane, flow, tone }: ElbowProps) {
  const color = lane.lit ? wash(tone, 85) : MC.line;
  const down = flow === "to-gateway";

  // The box spans from the lane's column to the gateway centre line; the two
  // borders it draws are the vertical run and the horizontal run, and the
  // radius between them is the corner.
  const shape: CSSProperties =
    lane.side === "center"
      ? { left: lane.x, width: 0, borderLeftWidth: 1 }
      : lane.side === "left"
        ? {
            left: lane.x,
            right: "50%",
            borderLeftWidth: 1,
            borderBottomWidth: down ? 1 : 0,
            borderTopWidth: down ? 0 : 1,
            borderBottomLeftRadius: down ? ELBOW_RADIUS : 0,
            borderTopLeftRadius: down ? 0 : ELBOW_RADIUS,
          }
        : {
            left: "50%",
            right: `calc(100% - ${lane.x})`,
            borderRightWidth: 1,
            borderBottomWidth: down ? 1 : 0,
            borderTopWidth: down ? 0 : 1,
            borderBottomRightRadius: down ? ELBOW_RADIUS : 0,
            borderTopRightRadius: down ? 0 : ELBOW_RADIUS,
          };

  return (
    <span
      className="absolute top-0 bottom-0 block border-solid transition-colors duration-500"
      style={{ ...shape, borderColor: color, zIndex: lane.lit ? 2 : 1 }}
    />
  );
}

interface PulseProps {
  readonly lane: Lane;
  readonly flow: BandFlow;
  readonly tone: string;
  /** Already gated `animation` shorthand, or `none` at rest. */
  readonly animation: string;
}

/**
 * A request travelling the elbow. At rest it parks on the corner, which is
 * the frame the server and reduced-motion renders show.
 */
export function Pulse({ lane, flow, tone, animation }: PulseProps) {
  return (
    <span
      className="absolute block h-[9px] w-[9px] rounded-full"
      style={
        {
          "--mc-layer-x": lane.x,
          zIndex: 3,
          left: lane.x,
          top: `${BUS_Y[flow]}%`,
          marginLeft: -4.5,
          marginTop: -4.5,
          background: tone,
          boxShadow: `0 0 12px ${wash(tone, 70)}`,
          animation,
        } as CSSProperties
      }
    />
  );
}
