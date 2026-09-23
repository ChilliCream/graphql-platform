import { AppWindow } from "@/src/components/AppWindow";
import { SectionShell } from "@/src/components/SectionShell";

interface ClientRow {
  readonly name: string;
  readonly env: string;
  readonly ok: number;
  readonly total: number;
  readonly status: "ok" | "risk" | "outside";
}

const CLIENT_ROWS: readonly ClientRow[] = [
  { name: "web", env: "production", ok: 5, total: 5, status: "ok" },
  { name: "mobile", env: "production", ok: 3, total: 5, status: "risk" },
  { name: "partner", env: "sandbox", ok: 0, total: 0, status: "outside" },
  { name: "internal-admin", env: "staging", ok: 6, total: 6, status: "ok" },
];

interface ImpactBarProps {
  readonly ok: number;
  readonly total: number;
  readonly status: ClientRow["status"];
}

function ImpactBar({ ok, total, status }: ImpactBarProps) {
  const cells = Array.from({ length: total });
  const color =
    status === "ok"
      ? "bg-cc-success"
      : status === "risk"
        ? "bg-cc-warning"
        : "bg-cc-ink-dim/50";
  return (
    <span className="flex gap-1">
      {cells.map((_, i) => (
        <span
          key={i}
          className={`h-2 w-5 rounded-[2px] ${i < ok ? color : "bg-cc-ink-faint"}`}
        />
      ))}
    </span>
  );
}

function ClientImpactMatrix() {
  const statusLabel: Record<
    ClientRow["status"],
    { text: string; cls: string }
  > = {
    ok: { text: "OK", cls: "text-cc-success" },
    risk: { text: "at risk", cls: "text-cc-warning" },
    outside: { text: "outside result", cls: "text-cc-ink-dim" },
  };
  return (
    <AppWindow
      title={
        <>
          <span>client registry</span>
          <span className="text-cc-nav-label">·</span>
          <span className="text-cc-prose">impact of #482</span>
        </>
      }
    >
      <table className="w-full table-fixed border-collapse">
        <colgroup>
          <col className="w-[43.3%]" />
          <col className="w-[33.3%]" />
          <col className="w-[26.6%]" />
        </colgroup>
        <thead>
          <tr className="border-cc-card-border text-cc-ink-dim border-b font-mono text-[0.6rem] tracking-[0.14em] uppercase">
            <th scope="col" className="px-4 py-2 text-left font-normal">
              client
            </th>
            <th scope="col" className="px-4 py-2 text-left font-normal">
              operations passing
            </th>
            <th scope="col" className="px-4 py-2 text-right font-normal">
              status
            </th>
          </tr>
        </thead>
        <tbody>
          {CLIENT_ROWS.map((c) => {
            const s = statusLabel[c.status];
            return (
              <tr
                key={c.name}
                className="border-cc-card-border border-b last:border-b-0"
              >
                <td className="min-w-0 px-4 py-3">
                  <div className="text-cc-heading truncate font-mono text-[0.78rem]">
                    {c.name}
                  </div>
                  <div className="text-cc-ink-dim font-mono text-[0.62rem]">
                    {c.env}
                  </div>
                </td>
                <td className="px-4 py-3">
                  <div className="flex items-center gap-3">
                    {c.total === 0 ? (
                      <span className="text-cc-heading font-mono text-[0.68rem]">
                        none published
                      </span>
                    ) : (
                      <>
                        <ImpactBar
                          ok={c.ok}
                          total={c.total}
                          status={c.status}
                        />
                        <span className="text-cc-ink-dim font-mono text-[0.68rem]">
                          {c.ok}/{c.total}
                        </span>
                      </>
                    )}
                  </div>
                </td>
                <td
                  className={`px-4 py-3 text-right font-mono text-[0.72rem] font-semibold ${s.cls}`}
                >
                  {s.text}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </AppWindow>
  );
}

export function ImpactSection() {
  return (
    <SectionShell
      title="See which clients a change would break."
      lead="Validation runs against the set of operations your client versions have published to that environment. Each client gets its own result: a change that is safe for web can still break mobile, and you see that before you merge."
      artifact={<ClientImpactMatrix />}
    />
  );
}
