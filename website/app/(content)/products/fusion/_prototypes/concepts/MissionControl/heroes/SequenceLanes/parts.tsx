import { Fragment } from "react";

import { anim } from "../../hooks";
import { MC, specTag } from "../../palette";
import type { Lane, Sequence } from "./lanes";
import {
  GATEWAY,
  LANES,
  SEQ_MS,
  SPEC_ACCENT,
  T,
  hopDelay,
  laneTop,
  returnAt,
  wash,
} from "./lanes";

/**
 * The pieces the sequence diagram is drawn from: the lane head plates, one
 * hop arrow, one activation bar, and the column that assembles a whole
 * request out of them. Every animation goes through `anim`, and the frame a
 * stopped animation leaves behind is the finished hop.
 */

interface LanePlateProps {
  readonly lane: Lane;
  /** True while this client's request is the one entering the trace. */
  readonly active: boolean;
}

/** Lane head: the participant name, its language and its spec badge. */
export function LanePlate({ lane, active }: LanePlateProps) {
  const gateway = lane.kind === "gateway";

  return (
    <div
      className="flex shrink-0 flex-col justify-center gap-px overflow-hidden rounded-md border px-2 py-0.5 leading-none transition-colors duration-300"
      style={{
        height: "calc(var(--mc-seq-lane) - 4px)",
        margin: "2px 0",
        background: gateway
          ? wash(MC.phosphor, 10)
          : active
            ? wash(MC.signal, 10)
            : MC.panel,
        borderColor: gateway
          ? wash(MC.phosphor, 55)
          : active
            ? wash(MC.signal, 60)
            : MC.panelEdge,
      }}
    >
      <span
        className="truncate text-[11px] tracking-wide sm:text-[12px] lg:text-[13px]"
        style={{ color: gateway ? MC.phosphor : MC.ink }}
      >
        {lane.name}
      </span>
      <span className="truncate text-[11px]" style={{ color: MC.dim }}>
        {lane.meta}
      </span>
      {lane.note && (
        <span className="truncate text-[11px]" style={{ color: MC.dim }}>
          {lane.note}
        </span>
      )}
      {lane.spec && (
        <span className="flex items-center gap-1" style={{ color: MC.dim }}>
          <span
            className="size-[5px] shrink-0 rounded-full"
            style={{ background: SPEC_ACCENT[lane.spec] }}
          />
          <span className="hidden truncate text-[11px] lg:inline">
            {lane.spec}
          </span>
          <span className="truncate text-[11px] lg:hidden">
            {specTag(lane.spec)}
          </span>
        </span>
      )}
    </div>
  );
}

interface ArrowProps {
  readonly from: number;
  readonly to: number;
  /** Position in the column, as a fraction of its width. */
  readonly at: number;
  readonly color: string;
  /** Responses are dashed, requests solid. */
  readonly dashed?: boolean;
  readonly delayMs: number;
  readonly running: boolean;
}

/** One hop: a vertical line between two lifelines with a head on the end. */
function Arrow({
  from,
  to,
  at,
  color,
  dashed = false,
  delayMs,
  running,
}: ArrowProps) {
  const down = to > from;

  return (
    <div
      className="absolute w-2"
      style={{
        left: `${at * 100}%`,
        marginLeft: "-4px",
        top: laneTop(Math.min(from, to)),
        height: `calc(var(--mc-seq-lane) * ${Math.abs(to - from)})`,
      }}
    >
      <span
        className="absolute top-0 bottom-0 w-px"
        style={{
          left: "3.5px",
          background: dashed
            ? `repeating-linear-gradient(to bottom, ${color} 0 3px, transparent 3px 7px)`
            : color,
          transformOrigin: down ? "top" : "bottom",
          animation: anim(
            running,
            `mc-seq-draw ${SEQ_MS}ms linear ${delayMs}ms infinite backwards`,
          ),
        }}
      />
      <span
        className="absolute left-0 size-0"
        style={{
          top: down ? undefined : "-1px",
          bottom: down ? "-1px" : undefined,
          borderLeft: "4px solid transparent",
          borderRight: "4px solid transparent",
          borderTop: down ? `5px solid ${color}` : undefined,
          borderBottom: down ? undefined : `5px solid ${color}`,
          animation: anim(
            running,
            `mc-seq-head ${SEQ_MS}ms linear ${delayMs}ms infinite backwards`,
          ),
        }}
      />
    </div>
  );
}

interface ActivationBarProps {
  readonly lane: number;
  readonly from: number;
  readonly to: number;
  readonly color: string;
  readonly delayMs: number;
  readonly running: boolean;
}

/** The stretch of a lifeline a participant is busy for. */
function ActivationBar({
  lane,
  from,
  to,
  color,
  delayMs,
  running,
}: ActivationBarProps) {
  return (
    <div
      className="absolute h-1.5 rounded-[2px]"
      style={{
        left: `${from * 100}%`,
        width: `${(to - from) * 100}%`,
        top: laneTop(lane),
        marginTop: "-3px",
        background: wash(color, 34),
        borderLeft: `1px solid ${wash(color, 75)}`,
        transformOrigin: "left",
        animation: anim(
          running,
          `mc-seq-fill ${SEQ_MS}ms linear ${delayMs}ms infinite backwards`,
        ),
      }}
    />
  );
}

interface SequenceBlockProps {
  readonly sequence: Sequence;
  /** Phase of this column, a quarter of a sequence behind the one left of it. */
  readonly delayMs: number;
  readonly running: boolean;
}

/** One column of the trace: a whole request, from client back to client. */
export function SequenceBlock({
  sequence,
  delayMs,
  running,
}: SequenceBlockProps) {
  const { client, targets } = sequence;
  const at = (fraction: number) => delayMs + hopDelay(fraction);

  return (
    <div className="relative h-full w-1/8 shrink-0">
      <ActivationBar
        lane={client}
        from={T.request}
        to={T.response}
        color={MC.signal}
        delayMs={at(T.request)}
        running={running}
      />
      <ActivationBar
        lane={GATEWAY}
        from={T.request}
        to={T.response}
        color={MC.phosphor}
        delayMs={at(T.request)}
        running={running}
      />
      <Arrow
        from={client}
        to={GATEWAY}
        at={T.request}
        color={MC.signal}
        delayMs={at(T.request)}
        running={running}
      />

      {targets.map((target, i) => (
        <Fragment key={target}>
          <ActivationBar
            lane={target}
            from={T.fanOut}
            to={returnAt(i)}
            color={LANES[target].accent}
            delayMs={at(T.fanOut)}
            running={running}
          />
          <Arrow
            from={GATEWAY}
            to={target}
            at={T.fanOut}
            color={MC.phosphor}
            delayMs={at(T.fanOut)}
            running={running}
          />
          <Arrow
            from={target}
            to={GATEWAY}
            at={returnAt(i)}
            color={LANES[target].accent}
            dashed
            delayMs={at(returnAt(i))}
            running={running}
          />
        </Fragment>
      ))}

      <Arrow
        from={GATEWAY}
        to={client}
        at={T.response}
        color={MC.signal}
        dashed
        delayMs={at(T.response)}
        running={running}
      />
    </div>
  );
}
