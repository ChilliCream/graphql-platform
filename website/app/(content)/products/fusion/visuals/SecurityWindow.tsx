import { AppWindow } from "@/src/components/AppWindow";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { CheckGlyph } from "@/src/icons/CheckGlyph";
import { CrossGlyph } from "@/src/icons/CrossGlyph";

type Tone = "success" | "danger";

const TONE_TEXT: Record<Tone, string> = {
  success: "text-cc-success",
  danger: "text-cc-danger",
};

interface DecisionRowSpec {
  readonly tone: Tone;
  readonly request: string;
  readonly rule: string;
  readonly verdict: string;
  readonly note?: string;
}

const ROWS: readonly DecisionRowSpec[] = [
  {
    tone: "success",
    request: "query GetOrder",
    rule: "trusted operation",
    verdict: "allowed",
  },
  {
    tone: "success",
    request: "mutation UpdatePrice",
    rule: "OPA · orders.write",
    verdict: "allowed",
  },
  {
    tone: "success",
    request: "query GetInvoice",
    rule: "custom policy · billing",
    verdict: "allowed",
  },
  {
    tone: "danger",
    request: "mutation DeleteCustomer",
    rule: "OPA · customers.delete",
    verdict: "denied · 403",
    note: "blocked at the gateway · never reached a service",
  },
  {
    tone: "danger",
    request: "query 7f3a…c21",
    rule: "not in safelist",
    verdict: "rejected",
    note: "blocked at the gateway · never reached a service",
  },
];

const ROW_DELAYS = ["", "delay-75", "delay-150", "delay-200", "delay-300"];

const ROW_CLASS = "border-cc-card-border border-b px-4 py-3 last:border-b-0";
const ROW_STACK_CLASS =
  "flex min-w-0 flex-1 flex-col gap-0.5 @min-[500px]:flex-row @min-[500px]:items-baseline @min-[500px]:justify-between @min-[500px]:gap-3";

interface DecisionRowProps extends DecisionRowSpec {
  readonly delayClassName: string;
}

function DecisionRow({
  tone,
  request,
  rule,
  verdict,
  note,
  delayClassName,
}: DecisionRowProps) {
  const Glyph = tone === "success" ? CheckGlyph : CrossGlyph;
  return (
    <RevealOnScroll className={`${ROW_CLASS} ${delayClassName}`}>
      <div className="flex items-start gap-3">
        <span
          className={`mt-0.5 flex h-4 w-4 shrink-0 items-center justify-center ${TONE_TEXT[tone]}`}
        >
          <Glyph width={12} height={12} />
        </span>
        <div className="min-w-0 flex-1">
          <div className={ROW_STACK_CLASS}>
            <span className="text-cc-heading font-mono text-[0.7rem]">
              {request}
              <span className="text-cc-ink-dim"> · {rule}</span>
            </span>
            <span
              className={`font-mono text-[0.7rem] ${TONE_TEXT[tone]} @min-[500px]:text-right`}
            >
              {verdict}
            </span>
          </div>
          {note !== undefined && (
            <div className={`mt-1 font-mono text-[0.7rem] ${TONE_TEXT[tone]}`}>
              {note}
            </div>
          )}
        </div>
      </div>
    </RevealOnScroll>
  );
}

export function SecurityWindow() {
  return (
    <AppWindow
      title={
        <span className="text-cc-prose">fusion gateway · policy decisions</span>
      }
      footer={
        <span className="text-cc-ink-dim font-mono text-[0.7rem]">
          decided at the gateway · every decision in the Nitro audit trail
        </span>
      }
    >
      <div className="@container">
        {ROWS.map((row, i) => (
          <DecisionRow
            key={row.request}
            {...row}
            delayClassName={ROW_DELAYS[i]}
          />
        ))}
      </div>
    </AppWindow>
  );
}
