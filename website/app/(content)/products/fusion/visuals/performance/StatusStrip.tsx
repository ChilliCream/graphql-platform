import { token } from "@/src/nitro";

import { REQUESTS_LABEL, STREAM_LABEL, UPTIME_LABEL } from "./data";

interface Stat {
  readonly label: string;
  readonly value: string;
}

const STATS: readonly Stat[] = [
  { label: "uptime", value: UPTIME_LABEL },
  { label: "requests", value: REQUESTS_LABEL },
];

function Dot() {
  return (
    <span
      aria-hidden="true"
      className="inline-block h-[6px] w-[6px] shrink-0 rounded-full"
      style={{ background: token.cSuccess }}
    />
  );
}

export function StatusStrip() {
  return (
    <div
      className="flex flex-wrap items-center justify-between gap-x-4 gap-y-1.5 border-t px-1 pt-2.5"
      style={{ borderColor: token.border }}
      role="group"
      aria-label={`Status: ${UPTIME_LABEL} uptime, ${REQUESTS_LABEL} requests, ${STREAM_LABEL} enabled`}
    >
      <div className="flex items-center gap-4">
        {STATS.map((s) => (
          <span
            key={s.label}
            className="text-[11px] whitespace-nowrap"
            style={{ color: token.textSecondary, fontFamily: token.mono }}
          >
            <span className="uppercase" style={{ letterSpacing: "0.06em" }}>
              {s.label}
            </span>{" "}
            <span style={{ color: token.textStrong, fontWeight: 700 }}>
              {s.value}
            </span>
          </span>
        ))}
      </div>
      <span
        className="flex items-center gap-1.5 text-[11px] whitespace-nowrap"
        style={{ color: token.cSuccess, fontFamily: token.mono }}
      >
        <Dot />
        {STREAM_LABEL}
      </span>
    </div>
  );
}
