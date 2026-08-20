import Link from "next/link";
import type { Template, TemplateSummary } from "@/src/data/templates/templates";
import { clientLabel, languageLabel, productLabel, topologyLabel, useCaseLabel } from "@/src/data/templates/filters";
import { CopyCommand } from "@/src/components/CopyCommand";
import { TemplateCard } from "./TemplateCard";
import { TemplateStackArt } from "./TemplateStackArt";
import { stackLabel } from "./stackIcons";
import { CodeBlock } from "@/src/design-system/CodeBlock";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Tag } from "@/src/design-system/Tag";
import { GitHubIcon } from "@/src/icons/GitHub";

interface TemplateDetailProps {
  readonly template: Template;
  readonly related: readonly TemplateSummary[];
}

export function TemplateDetail({ template, related }: TemplateDetailProps) {
  return (
    <>
      <header className="py-10 sm:py-16">
        <nav className="text-cc-ink-dim mb-8 flex items-center gap-2 text-sm" aria-label="Breadcrumb">
          <Link href="/templates" className="hover:text-cc-heading no-underline transition-colors">
            Templates
          </Link>
          <span aria-hidden="true">/</span>
          <span className="text-cc-heading">{template.title}</span>
        </nav>
        <div className="grid items-center gap-10 lg:grid-cols-[1fr_0.9fr]">
          <div>
            <div className="flex flex-wrap gap-2">
              <Tag>{topologyLabel(template.topology)}</Tag>
              {template.agentReady && <Tag className="border-cc-warning/40 text-cc-warning">Agent-ready</Tag>}
            </div>
            <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-6 font-semibold tracking-[-0.02em] text-balance">
              {template.title}
            </h1>
            <p className="text-cc-prose mt-5 max-w-2xl text-lg leading-relaxed">{template.tagline}</p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href={template.githubUrl}>
                <GitHubIcon className="mr-2 size-4 fill-current" />
                View source
              </SolidButton>
              {template.demoUrl && <OutlineButton href={template.demoUrl}>Live demo</OutlineButton>}
            </div>
          </div>
          <div className="border-cc-card-border overflow-hidden rounded-2xl border">
            <div className="aspect-[16/10]">
              <TemplateStackArt products={template.products} drinkBase={88} />
            </div>
          </div>
        </div>
      </header>

      <div className="border-cc-card-border grid gap-12 border-t py-12 lg:grid-cols-[minmax(0,1fr)_19rem] lg:gap-16">
        <article className="min-w-0">
          {template.body.map((section) => (
            <section key={section.heading} className="mb-14 last:mb-0">
              <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">{section.heading}</h2>
              <div className="mt-5 space-y-4">
                {section.paragraphs.map((paragraph) => (
                  <p key={paragraph} className="text-cc-prose leading-7">
                    {paragraph}
                  </p>
                ))}
              </div>
              {section.code && (
                <CodeBlock>
                  <code className={`language-${section.code.language}`}>{section.code.code}</code>
                </CodeBlock>
              )}
            </section>
          ))}
        </article>

        <aside className="lg:order-none">
          <div className="border-cc-card-border bg-cc-card-bg sticky top-28 overflow-hidden rounded-2xl border backdrop-blur-sm">
            <div className="p-5">
              <p className="text-cc-heading font-heading text-lg font-semibold">Get started</p>
              <div className="mt-4 space-y-3">
                {template.cli.map((command) => (
                  <div key={command.key}>
                    <p className="text-cc-ink-dim mb-1.5 font-mono text-[0.65rem] tracking-wider uppercase">
                      {command.label}
                    </p>
                    <CopyCommand command={command.code} size="sm" className="bg-cc-code-bg" />
                  </div>
                ))}
              </div>
              <dl className="border-cc-card-border mt-6 space-y-4 border-t pt-5 text-sm">
                <Detail label="Language" value={languageLabel(template.language)} />
                <Detail label="Use cases" value={template.useCases.map(useCaseLabel).join(", ")} />
                <Detail label="Clients" value={template.clients.map(clientLabel).join(", ")} />
                <Detail label="Products" value={template.products.map(productLabel).join(", ")} />
                {template.stack.length > 0 && (
                  <Detail label="Stack" value={template.stack.map(stackLabel).join(", ")} />
                )}
                <Detail label="License" value={template.license} />
                <Detail label="Updated" value={template.updatedRelative} />
              </dl>
            </div>
          </div>
        </aside>
      </div>

      <section className="border-cc-card-border border-t py-16 sm:py-24">
        <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold">Related templates</h2>
        <div className="mt-8 grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {related.map((item) => (
            <TemplateCard key={item.slug} template={item} />
          ))}
        </div>
      </section>
    </>
  );
}

function Detail({ label, value }: { readonly label: string; readonly value: string }) {
  return (
    <div>
      <dt className="text-cc-ink-dim font-mono text-[0.65rem] tracking-wider uppercase">{label}</dt>
      <dd className="text-cc-heading mt-1">{value}</dd>
    </div>
  );
}
