import { Band } from "@/src/components/Band";
import { CheckList } from "@/src/components/CheckList";
import { SectionHeading } from "@/src/components/SectionHeading";
import { CodeBlock } from "@/src/design-system/CodeBlock";
import { Tag } from "@/src/design-system/Tag";

type ChipKind = "graphql" | "rest" | "grpc" | "job" | "db";

const KIND_LABEL: Record<ChipKind, string> = {
  graphql: "GraphQL",
  rest: "REST",
  grpc: "gRPC",
  job: "job",
  db: "DB",
};

const STATEMENT_KINDS: readonly ChipKind[] = [
  "graphql",
  "rest",
  "grpc",
  "job",
  "db",
];

const OTEL_CHECKS: readonly string[] = [
  "Vendor-neutral OTLP in, no proprietary agent",
  "Hot Chocolate is auto-instrumented",
  "Works with any OpenTelemetry backend, not just Nitro",
];

export function FullOtelBand() {
  return (
    <Band
      className="mt-12"
      skin="card"
      layout="split"
      labelledBy="otel-title"
      main={
        <div>
          <SectionHeading
            titleId="otel-title"
            title={
              <>
                OpenTelemetry-native,{" "}
                <span className="text-cc-accent whitespace-nowrap">
                  end to end.
                </span>
              </>
            }
            description="Configured services export supported traces, metrics, and logs over plain OTLP. Nitro links reported operation signals to the related distributed traces for investigation."
          />
          <div className="mt-6 flex flex-wrap gap-2">
            {STATEMENT_KINDS.map((kind) => (
              <Tag key={kind}>{KIND_LABEL[kind]}</Tag>
            ))}
          </div>
          <CheckList items={OTEL_CHECKS} className="mt-7" />
        </div>
      }
      aside={
        <div className="[&>figure]:my-0">
          <CodeBlock theme="poimandres">
            <code className="language-csharp" data-meta='filename="Program.cs"'>
              {
                "builder.Services\n    .AddNitro()\n    .AddOpenTelemetry();\n\nbuilder.Services\n    .AddGraphQLServer()\n    .AddInstrumentation();"
              }
            </code>
          </CodeBlock>
        </div>
      }
    />
  );
}
