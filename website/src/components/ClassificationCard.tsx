import { Card } from "@/src/design-system/Card";

type ChangeKind = "safe" | "dangerous" | "breaking";

const KIND_STYLE: Record<
  ChangeKind,
  { readonly label: string; readonly className: string }
> = {
  safe: {
    label: "SAFE",
    className: "text-cc-success border-cc-success/40 bg-cc-success/[0.08]",
  },
  dangerous: {
    label: "DANGEROUS",
    className: "text-cc-warning border-cc-warning/40 bg-cc-warning/[0.08]",
  },
  breaking: {
    label: "BREAKING",
    className: "text-cc-danger border-cc-danger/40 bg-cc-danger/[0.08]",
  },
};

interface KindPillProps {
  readonly kind: ChangeKind;
  readonly comfortable: boolean;
}

function KindPill({ kind, comfortable }: KindPillProps) {
  const s = KIND_STYLE[kind];
  const size = comfortable ? "text-[0.7rem]" : "text-[10px]";
  return (
    <span
      className={[
        `rounded border px-1.5 py-0.5 font-mono ${size} tracking-[0.12em]`,
        s.className,
      ].join(" ")}
    >
      {s.label}
    </span>
  );
}

interface ClassificationChange {
  readonly kind: ChangeKind;
  readonly text: string;
}

interface ClassificationCardProps {
  readonly title: string;
  readonly version: string;
  readonly verdict: string;
  readonly changes: readonly ClassificationChange[];
  readonly footer: string;
  readonly density?: "compact" | "comfortable";
}

export function ClassificationCard({
  title,
  version,
  verdict,
  changes,
  footer,
  density = "compact",
}: ClassificationCardProps) {
  const comfortable = density === "comfortable";
  const footerSize = comfortable ? "text-[0.7rem]" : "text-[11px]";
  return (
    <Card className="mt-1">
      <div className="relative z-10 flex h-full flex-col">
        <div className="flex items-center justify-between px-4 py-3">
          <span className="text-cc-ink-dim text-caption font-mono tracking-[0.16em] uppercase">
            {`${title} · ${version}`}
          </span>
          <span className="text-cc-danger text-caption font-mono">
            {verdict}
          </span>
        </div>
        <div className="divide-cc-card-border border-cc-card-border divide-y border-t">
          {changes.map((c) => (
            <div
              key={c.text}
              className="flex items-center justify-between gap-3 px-4 py-2.5"
            >
              <code
                className={
                  comfortable
                    ? "text-cc-ink min-w-0 font-mono text-xs break-words"
                    : "text-cc-ink truncate font-mono text-xs"
                }
              >
                {c.text}
              </code>
              <KindPill kind={c.kind} comfortable={comfortable} />
            </div>
          ))}
        </div>
        <div
          className={`text-cc-ink-dim border-cc-card-border border-t px-4 py-2.5 font-mono ${footerSize}`}
        >
          {footer}
        </div>
      </div>
    </Card>
  );
}
