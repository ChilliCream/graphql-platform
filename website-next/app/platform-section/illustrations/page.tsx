import { AgenticIllu } from "@/src/components/home/platform/illustrations/AgenticIllu";
import { BuildIllu } from "@/src/components/home/platform/illustrations/BuildIllu";
import { GuardrailsIllu } from "@/src/components/home/platform/illustrations/GuardrailsIllu";
import { ObserveIllu } from "@/src/components/home/platform/illustrations/ObserveIllu";
import { WorkflowsIllu } from "@/src/components/home/platform/illustrations/WorkflowsIllu";

const ITEMS = [
  ["build", "Build", BuildIllu],
  ["agentic", "Agentic", AgenticIllu],
  ["observe", "Monitoring", ObserveIllu],
  ["workflows", "Event-driven", WorkflowsIllu],
  ["guardrails", "Schema evolution", GuardrailsIllu],
] as const;

export default function IllustrationsPreview() {
  return (
    <div className="mx-auto grid max-w-6xl grid-cols-1 gap-8 px-5 py-16 sm:grid-cols-2 sm:px-12">
      {ITEMS.map(([id, label, C]) => (
        <div
          key={id}
          id={`illu-${id}`}
          className="border-cc-card-border flex flex-col gap-4 rounded-2xl border p-6"
        >
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            {label}
          </p>
          <div className="flex min-h-56 items-center justify-center">
            <C />
          </div>
        </div>
      ))}
    </div>
  );
}
