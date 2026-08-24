import { AppWindow } from "@/src/components/AppWindow";
import { SectionShell } from "@/src/components/SectionShell";
import { STATUS_META, StatusChip } from "@/src/components/StatusChip";
import type { ChangeStatus } from "@/src/components/StatusChip";

interface VersionPoint {
  readonly v: string;
  readonly note: string;
  readonly status: ChangeStatus;
}

const VERSIONS: readonly VersionPoint[] = [
  { v: "v12", note: "add Cart.discount", status: "safe" },
  { v: "v13", note: "deprecate Order.placedAt", status: "dangerous" },
  { v: "v14", note: "remove Order.total — blocked", status: "breaking" },
  { v: "v14", note: "add Order.totalAmount", status: "safe" },
  { v: "v15", note: "drop Order.total — usage cleared", status: "dangerous" },
];

function VersionTimeline() {
  return (
    <AppWindow title={<span className="text-cc-prose">schema history</span>}>
      <div className="px-5 py-6">
        <div className="relative">
          <span aria-hidden className="bg-cc-card-border absolute top-2.5 left-2 h-[calc(100%-1.25rem)] w-px" />
          <ol className="space-y-5">
            {VERSIONS.map((p) => {
              const meta = STATUS_META[p.status];
              return (
                <li key={`${p.v}-${p.note}`} className="relative flex items-center gap-4 pl-7">
                  <span
                    className={`absolute left-0 flex h-4 w-4 items-center justify-center rounded-full ${meta.bg} ring-1 ring-inset ${meta.ring}`}
                  >
                    <span className={`h-1.5 w-1.5 rounded-full ${meta.dot}`} />
                  </span>
                  <span className="text-cc-heading w-10 shrink-0 font-mono text-[0.74rem] font-semibold">{p.v}</span>
                  <span className="text-cc-prose min-w-0 flex-1 truncate font-mono text-[0.74rem]">{p.note}</span>
                  <StatusChip status={p.status} />
                </li>
              );
            })}
          </ol>
        </div>
      </div>
    </AppWindow>
  );
}

export function TimelineSection() {
  return (
    <SectionShell
      title="Keep the full history of your schema."
      lead="Every upload and publish adds a version to the registry, giving you a browsable record of how the API evolved: what changed in each version, how severe it was, and when a blocked removal finally cleared. Answering 'when did this field change and why' no longer means digging through merge commits."
      artifact={<VersionTimeline />}
    />
  );
}
