import Link from "next/link";
import type { ComponentPropsWithoutRef } from "react";
import { DrinkIcon } from "@/src/components/DrinkIcon";
import type { LearnItemSummary } from "@/src/data/learn/types";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";
import { ContentTypeBadge } from "./ContentTypeBadge";
import { CONTENT_TYPE_META } from "./contentTypeMeta";
import { learnItemHref } from "./learnItemHref";
import { PRODUCT_ART } from "./productArt";
import { STACK_ICONS } from "./stackIcons";

interface LearnCardProps {
  readonly item: LearnItemSummary;
}

/** Diagonal arrow-out-of-box glyph for items that open in a new tab. */
function ExternalArrowIcon(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      {...props}
    >
      <path d="M7 17 17 7" />
      <path d="M9 7h8v8" />
    </svg>
  );
}

function HeaderMeta({ item }: LearnCardProps) {
  if (item.type === "template") {
    if (!item.agentReady) {
      return null;
    }
    return (
      <span className="bg-cc-warning text-cc-surface rounded-full px-3 py-1 font-mono text-[0.65rem] font-semibold tracking-wider uppercase">
        Agent-ready
      </span>
    );
  }
  const meta = item.type === "video" ? item.duration : item.level;
  if (!meta) {
    return null;
  }
  return <span className="text-cc-ink-dim font-mono text-[0.65rem] tracking-wider uppercase">{meta}</span>;
}

/**
 * Unified card for every /learn content type: template, video, tutorial,
 * example, and workshop. The accent (badge, hover border, CTA color) is the
 * only place color varies; everything else stays neutral so a mixed grid
 * reads as one family.
 */
export function LearnCard({ item }: LearnCardProps) {
  const meta = CONTENT_TYPE_META[item.type];
  const href = learnItemHref(item);
  const external = !href.startsWith("/");

  const inner = (
    <>
      <div className="flex items-start justify-between gap-3">
        <ContentTypeBadge type={item.type} />
        <HeaderMeta item={item} />
      </div>
      <h3 className="font-heading text-cc-heading text-h6 mt-5 font-semibold">{item.title}</h3>
      <p className="text-cc-ink-dim mt-2 line-clamp-3 text-sm leading-relaxed">{item.tagline}</p>
      <div className="border-cc-card-border mt-auto flex items-center justify-between gap-3 border-t pt-4">
        <span className="flex items-end gap-2" aria-hidden="true">
          <span className="flex items-end gap-1.5">
            {item.products.map((product) => {
              const art = PRODUCT_ART[product];
              return <DrinkIcon key={product} Icon={art.Drink} name={art.drinkName} base={28} />;
            })}
          </span>
          {item.type === "template" && item.stack.length > 0 && (
            <span className="flex items-center gap-1.5">
              {item.stack.map((key) => {
                const { Icon, label } = STACK_ICONS[key];
                return (
                  <span
                    key={key}
                    title={label}
                    className="flex size-7 items-center justify-center rounded-lg bg-[#f5f0ea]"
                  >
                    <Icon className="size-4" />
                  </span>
                );
              })}
            </span>
          )}
        </span>
        <span className="text-cc-accent inline-flex shrink-0 items-center gap-2 text-sm font-medium">
          {meta.ctaLabel}
          {external ? (
            <ExternalArrowIcon className="size-3.5 transition-transform group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
          ) : (
            <ArrowRightIcon className="size-4 transition-transform group-hover:translate-x-1" />
          )}
        </span>
      </div>
    </>
  );

  const className = `border-cc-card-border bg-cc-card-bg group flex h-full flex-col rounded-2xl border p-6 no-underline backdrop-blur-sm transition-[border-color,transform] duration-200 hover:-translate-y-1 ${meta.hoverBorder}`;
  const ariaLabel = `${meta.ctaLabel}: ${item.title}`;

  if (external) {
    return (
      <a href={href} target="_blank" rel="noopener noreferrer" aria-label={ariaLabel} className={className}>
        {inner}
      </a>
    );
  }
  return (
    <Link href={href} prefetch={false} aria-label={ariaLabel} className={className}>
      {inner}
    </Link>
  );
}
