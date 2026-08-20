import Link from "next/link";
import type { TemplateSummary } from "@/src/data/templates/templates";
import { topologyLabel } from "@/src/data/templates/filters";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";
import { DrinkIcon } from "@/src/components/DrinkIcon";
import { PRODUCT_ART } from "./productArt";
import { STACK_ICONS } from "./stackIcons";

interface TemplateCardProps {
  readonly template: TemplateSummary;
}

export function TemplateCard({ template }: TemplateCardProps) {
  return (
    <Link
      href={`/templates/${template.slug}`}
      prefetch={false}
      aria-label={`View ${template.title} template`}
      className="border-cc-card-border bg-cc-card-bg hover:border-cc-accent/60 group flex h-full flex-col rounded-2xl border p-6 no-underline backdrop-blur-sm transition-[border-color,transform] duration-200 hover:-translate-y-1"
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-end gap-3">
          <span className="flex items-end gap-1.5" aria-hidden="true">
            {template.products.map((product) => {
              const art = PRODUCT_ART[product];
              return <DrinkIcon key={product} Icon={art.Drink} name={art.drinkName} base={30} />;
            })}
          </span>
          <span className="flex items-center gap-1.5" aria-hidden="true">
            {template.stack.map((key) => {
              const { Icon, label } = STACK_ICONS[key];
              return (
                <span
                  key={key}
                  title={label}
                  className="flex size-8 items-center justify-center rounded-lg bg-[#f5f0ea]"
                >
                  <Icon className="size-4.5" />
                </span>
              );
            })}
          </span>
        </div>
        {template.agentReady && (
          <span className="bg-cc-warning text-cc-surface rounded-full px-3 py-1 font-mono text-[0.65rem] font-semibold tracking-wider uppercase">
            Agent-ready
          </span>
        )}
      </div>
      <h2 className="font-heading text-cc-heading text-h6 mt-5 font-semibold">{template.title}</h2>
      <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">{template.tagline}</p>
      <div className="mt-auto pt-6">
        <div className="border-cc-card-border flex items-center justify-between gap-3 border-t pt-4">
          <span className="text-cc-ink-dim font-mono text-[0.65rem] tracking-wider uppercase">
            {topologyLabel(template.topology)}
          </span>
          <span className="text-cc-accent inline-flex items-center gap-2 text-sm font-medium">
            View template
            <ArrowRightIcon className="size-4 transition-transform group-hover:translate-x-1" />
          </span>
        </div>
      </div>
    </Link>
  );
}
