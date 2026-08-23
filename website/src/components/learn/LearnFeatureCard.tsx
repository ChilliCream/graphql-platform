import Link from "next/link";
import type { LearnItemSummary } from "@/src/data/learn/types";
import { ContentTypeBadge } from "./ContentTypeBadge";
import { learnItemHref } from "./learnItemHref";
import { TemplateStackArt } from "./TemplateStackArt";

interface LearnFeatureCardProps {
  readonly item: LearnItemSummary;
}

/**
 * Horizontal lead card for a catalog grid: `TemplateStackArt` beside the
 * badge, title, and tagline, instead of the uniform `LearnCard` tile
 * (learn-harmonization.md section 2.5.3/2.6.2/D8/D16). Used to give the
 * "Start building" band, the browse catalog's default view, and a detail
 * page's related band one lead item instead of a wall of identical cards.
 */
export function LearnFeatureCard({ item }: LearnFeatureCardProps) {
  const href = learnItemHref(item);
  const external = !href.startsWith("/");
  const inner = (
    <>
      <div className="aspect-[16/10] sm:aspect-auto">
        <TemplateStackArt products={item.products} drinkBase={64} />
      </div>
      <div className="flex flex-col justify-center p-6">
        <ContentTypeBadge type={item.type} />
        <h3 className="font-heading text-cc-heading text-h5 group-hover:text-cc-accent mt-3 font-semibold transition-colors">
          {item.title}
        </h3>
        <p className="text-cc-ink-dim mt-2 line-clamp-3 text-sm leading-relaxed">{item.tagline}</p>
      </div>
    </>
  );
  const className =
    "border-cc-card-border hover:border-cc-card-border-hover group grid overflow-hidden rounded-2xl border no-underline transition-[border-color,transform] duration-200 hover:-translate-y-1 sm:grid-cols-2";

  if (external) {
    return (
      <a href={href} target="_blank" rel="noopener noreferrer" className={className}>
        {inner}
      </a>
    );
  }
  return (
    <Link href={href} prefetch={false} className={className}>
      {inner}
    </Link>
  );
}
