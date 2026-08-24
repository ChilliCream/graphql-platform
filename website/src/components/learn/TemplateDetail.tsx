import { CopyCommand } from "@/src/components/CopyCommand";
import { clientLabel, languageLabel, productLabel, topologyLabel, useCaseLabel } from "@/src/data/learn/facets";
import type { LearnItemSummary, TemplateItem } from "@/src/data/learn/types";
import { CodeBlock } from "@/src/design-system/CodeBlock";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Tag } from "@/src/design-system/Tag";
import { GitHubIcon } from "@/src/icons/GitHub";
import { ArticleBreadcrumb } from "./ArticleLayout";
import { ContentTypeBadge } from "./ContentTypeBadge";
import { Detail } from "./Detail";
import { LearnCard } from "./LearnCard";
import { LearnFeatureCard } from "./LearnFeatureCard";
import { stackLabel } from "./stackIcons";
import { TemplateStackArt } from "./TemplateStackArt";

interface TemplateDetailProps {
  readonly template: TemplateItem;
  readonly related: readonly LearnItemSummary[];
}

export function TemplateDetail({ template, related }: TemplateDetailProps) {
  const [leadRelated, ...restRelated] = related;

  return (
    <>
      <header className="py-8 sm:py-10">
        <div className="mb-8">
          <ArticleBreadcrumb
            items={[
              { label: "Learn", href: "/learn" },
              { label: "Templates", href: "/learn/browse?type=template" },
              { label: template.title },
            ]}
          />
        </div>
        <div className="grid items-center gap-10 lg:grid-cols-[1fr_minmax(0,28rem)]">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <ContentTypeBadge type="template" />
              <Tag>{topologyLabel(template.topology)}</Tag>
              {template.agentReady && <Tag className="border-cc-warning/40 text-cc-warning">Agent-ready</Tag>}
            </div>
            <h1 className="font-heading text-cc-heading text-h3 mt-6 font-semibold tracking-[-0.02em] text-balance">
              {template.title}
            </h1>
            <p className="text-cc-prose mt-5 text-lg leading-relaxed">{template.tagline}</p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href={template.githubUrl}>
                <GitHubIcon className="mr-2 size-4 fill-current" />
                View source
              </SolidButton>
              {template.demoUrl && <OutlineButton href={template.demoUrl}>Live demo</OutlineButton>}
            </div>
          </div>
          <div className="overflow-hidden rounded-2xl">
            <div className="aspect-[16/10]">
              <TemplateStackArt products={template.products} drinkBase={88} />
            </div>
          </div>
        </div>
      </header>

      <div className="border-cc-card-border grid gap-12 border-t py-12 lg:grid-cols-[1fr_19rem] lg:gap-16">
        <article className="min-w-0">
          {template.body.map((section) => (
            <section key={section.heading} className="mb-14 last:mb-0">
              <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">{section.heading}</h2>
              <div className="mt-5 space-y-4">
                {section.paragraphs.map((paragraph) => (
                  <p key={paragraph} className="text-cc-prose text-lg leading-8">
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
                    <p className="text-cc-ink-dim mb-1.5 font-mono text-[0.6875rem] tracking-wider uppercase">
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

      {related.length > 0 && (
        <section className="border-cc-card-border border-t py-8 sm:py-10">
          <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">More from Learn</h2>
          <div className="mt-8 grid gap-6 lg:grid-cols-2">
            {leadRelated ? <LearnFeatureCard item={leadRelated} /> : null}
            {restRelated.length > 0 ? (
              <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-1">
                {restRelated.map((item) => (
                  <LearnCard key={item.slug} item={item} />
                ))}
              </div>
            ) : null}
          </div>
        </section>
      )}
    </>
  );
}
