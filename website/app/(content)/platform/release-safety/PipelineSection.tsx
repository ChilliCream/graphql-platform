import type { ReactNode } from "react";

import NextLink from "next/link";

import { AppWindow } from "@/src/components/AppWindow";
import { SectionHeading } from "@/src/components/SectionHeading";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";

import { tk } from "./syntaxTokens";

const CARD_FOCUS_CLASSES =
  "focus-visible:ring-cc-accent/30 focus-visible:ring-2 focus-visible:outline-hidden";

interface PipelineCardSpec {
  readonly kicker: string;
  readonly file: string;
  readonly code: ReactNode;
  readonly status: string;
  readonly href: string;
  readonly action: string;
}

const PIPELINE_CARDS: readonly PipelineCardSpec[] = [
  {
    kicker: "GitHub Actions",
    file: ".github/workflows/ci.yml",
    code: (
      <>
        {tk.punc("- ")}
        {tk.kw("uses")}
        {tk.punc(": ")}ChilliCream/nitro-schema-validate{tk.ty("@v16")}
        {"\n"}
        {"  "}
        {tk.kw("with")}
        {tk.punc(":")}
        {"\n"}
        {"    "}
        {tk.kw("api-id")}
        {tk.punc(": ")}
        {tk.dir("${{ vars.NITRO_API_ID }}")}
        {"\n"}
        {"    "}
        {tk.kw("api-key")}
        {tk.punc(": ")}
        {tk.dir("${{ secrets.NITRO_API_KEY }}")}
        {"\n"}
        {"    "}
        {tk.kw("schema-file")}
        {tk.punc(": ")}./schema.graphql
        {"\n"}
        {"    "}
        {tk.kw("stage")}
        {tk.punc(": ")}
        {tk.ty("production")}
        {"\n"}
        {"    "}
        {tk.kw("comment-mode")}
        {tk.punc(": ")}review
      </>
    ),
    status: "✓ check passed",
    href: "https://github.com/marketplace?query=nitro",
    action: "GitHub Marketplace",
  },
  {
    kicker: "Azure Pipelines",
    file: "azure-pipelines.yml",
    code: (
      <>
        {tk.punc("- ")}
        {tk.kw("task")}
        {tk.punc(": ")}NitroSchemaValidate{tk.ty("@16")}
        {"\n"}
        {"  "}
        {tk.kw("inputs")}
        {tk.punc(":")}
        {"\n"}
        {"    "}
        {tk.kw("authenticationType")}
        {tk.punc(": ")}serviceConnection
        {"\n"}
        {"    "}
        {tk.kw("nitroServiceConnection")}
        {tk.punc(": ")}nitro-prod
        {"\n"}
        {"    "}
        {tk.kw("apiId")}
        {tk.punc(": ")}
        {tk.dir("$(NITRO_API_ID)")}
        {"\n"}
        {"    "}
        {tk.kw("schemaFile")}
        {tk.punc(": ")}./schema.graphql
        {"\n"}
        {"    "}
        {tk.kw("stage")}
        {tk.punc(": ")}
        {tk.ty("production")}
      </>
    ),
    status: "✓ task succeeded",
    href: "https://marketplace.visualstudio.com/items?itemName=ChilliCream.nitro-azure-pipelines-tasks",
    action: "Visual Studio Marketplace",
  },
  {
    kicker: "Any other CI",
    file: "shell",
    code: (
      <>
        {tk.punc("$ ")}
        {tk.fld("nitro schema validate")} {tk.punc("\\")}
        {"\n"}
        {"    "}
        {tk.kw("--api-id")} {tk.dir("$NITRO_API_ID")} {tk.punc("\\")}
        {"\n"}
        {"    "}
        {tk.kw("--schema-file")} schema.graphql {tk.punc("\\")}
        {"\n"}
        {"    "}
        {tk.kw("--stage")} {tk.ty("production")}
        {"\n"}
        {"\n"}
        {tk.punc("validating against production…")}
        {"\n"}
        <span className="text-cc-success">✓ no breaking changes</span>
      </>
    ),
    status: "✓ exit 0",
    href: "/docs/nitro/cli/schema",
    action: "CLI reference",
  },
];

interface PipelineCardProps {
  readonly kicker: string;
  readonly file: string;
  readonly code: ReactNode;
  readonly status: string;
  readonly href: string;
  readonly action: string;
}

function PipelineCard({
  kicker,
  file,
  code,
  status,
  href,
  action,
}: PipelineCardProps) {
  const external = !href.startsWith("/");
  const linkClassName = `group block min-w-0 rounded-xl no-underline transition-transform duration-200 hover:-translate-y-1 ${CARD_FOCUS_CLASSES}`;
  const card = (
    <AppWindow
      disclosure={kicker}
      title={<span className="text-cc-prose">{file}</span>}
      footer={
        <div className="flex items-center justify-between gap-2">
          <span className="text-cc-success font-mono text-[0.66rem]">
            {status}
          </span>
          <span className="text-cc-accent group-hover:text-cc-accent-hover inline-flex items-center gap-1.5 text-sm font-medium transition-colors">
            {action}
            <ArrowRightIcon className="size-3.5 transition-transform group-hover:translate-x-0.5" />
          </span>
        </div>
      }
    >
      <div className="text-cc-prose overflow-x-auto px-4 py-4 font-mono text-[0.7rem] leading-relaxed whitespace-pre">
        {code}
      </div>
    </AppWindow>
  );
  return external ? (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className={linkClassName}
    >
      {card}
    </a>
  ) : (
    <NextLink href={href} className={linkClassName}>
      {card}
    </NextLink>
  );
}

export function PipelineSection() {
  return (
    <section aria-labelledby="pipelines-title">
      <SectionHeading
        titleId="pipelines-title"
        title="Run the checks in the CI you already have."
        description="The validate, upload, and publish steps ship as ready-made GitHub Actions and Azure Pipelines tasks, both wrapping the Nitro CLI."
      />
      <div className="mt-10 grid items-start gap-5 lg:grid-cols-3">
        {PIPELINE_CARDS.map((card) => (
          <PipelineCard key={card.kicker} {...card} />
        ))}
      </div>
    </section>
  );
}
