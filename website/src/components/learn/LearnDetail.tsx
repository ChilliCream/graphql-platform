import { CopyCommand } from "@/src/components/CopyCommand";
import { clientLabel, languageLabel, productLabel, topologyLabel, useCaseLabel } from "@/src/data/learn/facets";
import type { DetailItem, LearnItemSummary } from "@/src/data/learn/types";
import { CodeBlock } from "@/src/design-system/CodeBlock";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Tag } from "@/src/design-system/Tag";
import { GitHubIcon } from "@/src/icons/GitHub";
import { ContentTypeBadge } from "./ContentTypeBadge";
import { Detail } from "./Detail";
import { LearnCard } from "./LearnCard";
import { LearnFeatureCard } from "./LearnFeatureCard";
import { stackLabel } from "./stackIcons";
import { TemplateStackArt } from "./TemplateStackArt";

interface LearnDetailProps {
  readonly item: DetailItem;
  readonly related: readonly LearnItemSummary[];
}

/** Primary hero CTA label for non-template types that fall back to `externalUrl` (no `githubUrl`). */
const EXTERNAL_CTA_LABEL: Record<Exclude<DetailItem["type"], "template">, string> = {
  tutorial: "Read the tutorial",
  example: "View the example",
  workshop: "Read the workshop",
};

interface PrimaryLink {
  readonly href: string;
  readonly label: string;
  readonly isGithub: boolean;
}

/**
 * The hero's primary CTA: a GitHub "View source" button when the item has a
 * `githubUrl` (every template, and the tutorial/example/workshop items that
 * are themselves repos), else a type-appropriate button to `externalUrl`
 * (e.g. a docs-hosted tutorial), else nothing (an item with neither, until
 * website-kbx.26 backfills one).
 */
function primaryLink(item: DetailItem): PrimaryLink | undefined {
  if (item.type === "template") {
    return { href: item.githubUrl, label: "View source", isGithub: true };
  }
  if (item.githubUrl) {
    return { href: item.githubUrl, label: "View source", isGithub: true };
  }
  if (item.externalUrl) {
    return { href: item.externalUrl, label: EXTERNAL_CTA_LABEL[item.type], isGithub: false };
  }
  return undefined;
}

/** The sidebar `dl`: template-only axes (language, use cases, clients, stack, license) plus the fields every type carries (products, level, updated). */
function DetailFacts({ item }: { readonly item: DetailItem }) {
  if (item.type === "template") {
    return (
      <>
        <Detail label="Language" value={languageLabel(item.language)} />
        <Detail label="Use cases" value={item.useCases.map(useCaseLabel).join(", ")} />
        <Detail label="Clients" value={item.clients.map(clientLabel).join(", ")} />
        <Detail label="Products" value={item.products.map(productLabel).join(", ")} />
        {item.stack.length > 0 && <Detail label="Stack" value={item.stack.map(stackLabel).join(", ")} />}
        <Detail label="License" value={item.license} />
        <Detail label="Updated" value={item.updatedRelative} />
      </>
    );
  }
  return (
    <>
      <Detail label="Products" value={item.products.map(productLabel).join(", ")} />
      {item.level && <Detail label="Level" value={item.level} className="capitalize" />}
      <Detail label="Updated" value={item.updatedRelative} />
    </>
  );
}

/**
 * Shared detail-page layout for every catalog content type except video
 * (which keeps its own player-centric `LearnVideoDetail`): a hero with a
 * type kicker and product artwork, an optional prose body, and a sidebar
 * with the GitHub/external CTA, CLI commands, and fact list, generalized
 * from the original template-only layout (website-kbx.25). Fields templates
 * always carry (`cli`, `body`) are optional on the other three types, so a
 * seed item without body content (the 8 tutorial/example/workshop items,
 * until website-kbx.26 backfills them) still renders a complete page from
 * its tagline, products, and external link alone.
 */
export function LearnDetail({ item, related }: LearnDetailProps) {
  const [leadRelated, ...restRelated] = related;
  const cta = primaryLink(item);
  const cli = item.cli ?? [];
  const body = item.body ?? [];
  const hasBody = body.length > 0;

  return (
    <>
      <header className="py-8 sm:py-10">
        <div className="grid items-center gap-10 lg:grid-cols-[1fr_minmax(0,28rem)]">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <ContentTypeBadge type={item.type} />
              {item.type === "template" && <Tag>{topologyLabel(item.topology)}</Tag>}
              {item.type === "template" && item.agentReady && (
                <Tag className="border-cc-warning/40 text-cc-warning">Agent-ready</Tag>
              )}
              {item.type !== "template" && item.level && <Tag className="capitalize">{item.level}</Tag>}
            </div>
            <h1 className="font-heading text-cc-heading text-h3 mt-6 font-semibold tracking-[-0.02em] text-balance">
              {item.title}
            </h1>
            <p className="text-cc-prose mt-5 text-lg leading-relaxed">{item.tagline}</p>
            <div className="mt-8 flex flex-wrap gap-3">
              {cta && (
                <SolidButton
                  href={cta.href}
                  track={
                    cta.isGithub
                      ? {
                          name: "repo_click",
                          params: { repo_url: cta.href, item_type: item.type, item_slug: item.slug },
                        }
                      : undefined
                  }
                >
                  {cta.isGithub && <GitHubIcon className="mr-2 size-4 fill-current" />}
                  {cta.label}
                </SolidButton>
              )}
              {item.type === "template" && item.demoUrl && <OutlineButton href={item.demoUrl}>Live demo</OutlineButton>}
            </div>
          </div>
          <div className="overflow-hidden rounded-2xl">
            <div className="aspect-[16/10]">
              <TemplateStackArt products={item.products} drinkBase={88} />
            </div>
          </div>
        </div>
      </header>

      <div
        className={`border-cc-card-border border-t py-12 ${
          hasBody ? "grid gap-12 lg:grid-cols-[1fr_19rem] lg:gap-16" : "flex justify-start"
        }`}
      >
        {hasBody && (
          <article className="min-w-0">
            {body.map((section) => (
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
        )}

        <aside className={hasBody ? "lg:order-none" : "w-full max-w-sm"}>
          <div className="border-cc-card-border bg-cc-card-bg sticky top-28 overflow-hidden rounded-2xl border backdrop-blur-sm">
            <div className="p-5">
              {cli.length > 0 && (
                <>
                  <p className="text-cc-heading font-heading text-lg font-semibold">Get started</p>
                  <div className="mt-4 space-y-3">
                    {cli.map((command) => (
                      <div key={command.key}>
                        <p className="text-cc-ink-dim mb-1.5 font-mono text-[0.6875rem] tracking-wider uppercase">
                          {command.label}
                        </p>
                        <CopyCommand
                          command={command.code}
                          size="sm"
                          className="bg-cc-code-bg"
                          track={{ commandKey: command.key, itemSlug: item.slug }}
                        />
                      </div>
                    ))}
                  </div>
                </>
              )}
              <dl className={`space-y-4 text-sm ${cli.length > 0 ? "border-cc-card-border mt-6 border-t pt-5" : ""}`}>
                <DetailFacts item={item} />
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
                {restRelated.map((relatedItem) => (
                  <LearnCard key={relatedItem.slug} item={relatedItem} />
                ))}
              </div>
            ) : null}
          </div>
        </section>
      )}
    </>
  );
}
