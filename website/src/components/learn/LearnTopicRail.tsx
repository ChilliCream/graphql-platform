import { ArrowLink } from "@/src/components/ArrowLink";
import { BlogTeaser, type BlogTeaserData } from "@/src/components/BlogTeaser";
import { CardGrid } from "@/src/components/CardGrid";
import type { LearnItemSummary } from "@/src/data/learn/types";
import { LearnCard } from "./LearnCard";

export type TopicRailSlot =
  | { readonly kind: "post"; readonly post: BlogTeaserData }
  | { readonly kind: "catalog"; readonly item: LearnItemSummary };

interface LearnTopicRailProps {
  readonly heading: string;
  readonly moreHref: string;
  readonly slots: readonly TopicRailSlot[];
}

/**
 * One rail per topic with 3 or more items (learn-editorial.md section 3.4):
 * a fixed mix of `BlogTeaser`s ("read this") and `LearnCard`s ("use this")
 * in one row, so the deliberate mixing of the two card families reads as
 * intent. Topic membership and slot selection are computed by the caller;
 * this component only renders the given slots.
 */
export function LearnTopicRail({ heading, moreHref, slots }: LearnTopicRailProps) {
  if (slots.length === 0) {
    return null;
  }
  return (
    <section className="border-cc-card-border border-t py-14 sm:py-20">
      <div className="mb-8 flex items-center justify-between gap-4">
        <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">{heading}</h2>
        <ArrowLink href={moreHref}>More {heading}</ArrowLink>
      </div>
      <CardGrid cols={3} step="progressive" itemsStretch>
        {slots.map((slot) =>
          slot.kind === "post" ? (
            <BlogTeaser key={`post-${slot.post.href}`} post={slot.post} />
          ) : (
            <LearnCard key={`item-${slot.item.type}-${slot.item.slug}`} item={slot.item} />
          ),
        )}
      </CardGrid>
    </section>
  );
}
