import { ClientImpactMatrix } from "@/src/components/ClientImpactMatrix";
import { SectionShell } from "@/src/components/SectionShell";

const CLIENT_ROWS = [
  { client: "web", environment: "production", ok: 5, total: 5, status: "ok" },
  {
    client: "mobile",
    environment: "production",
    ok: 3,
    total: 5,
    status: "risk",
  },
  {
    client: "partner",
    environment: "sandbox",
    ok: 0,
    total: 0,
    status: "outside",
  },
  {
    client: "internal-admin",
    environment: "staging",
    ok: 6,
    total: 6,
    status: "ok",
  },
] as const;

export function ImpactSection() {
  return (
    <SectionShell
      title="See which clients a change would break."
      lead="Validation runs against the operations your client versions have published to that environment. Each client gets its own result: a change can be safe for web and still break mobile, and you see that before you merge."
      artifact={
        <ClientImpactMatrix
          title={
            <>
              <span>client registry</span>
              <span className="text-cc-nav-label">·</span>
              <span className="text-cc-prose">impact of #482</span>
            </>
          }
          rows={CLIENT_ROWS}
        />
      }
    />
  );
}
