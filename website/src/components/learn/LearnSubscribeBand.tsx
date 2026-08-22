import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * The landing's single closing band (learn-editorial.md section 3.8).
 *
 * Design-review decision (website-5yo.10, per the .9 review comment):
 * section 3.8 stacked `LearnSubscribeBand` directly above `NextStepsSection`,
 * two visually identical band+heading+buttons units back to back. They
 * collapse into this one band, with every action (subscribe, browse, docs)
 * in a single `ButtonRow` rather than two differentiated skins, since one
 * "here's how to keep learning" unit reads more coherently than two.
 *
 * Phase 1 (no newsletter provider; strategy section 1.3): "Subscribe via
 * RSS" links the feed directly. No fake email form: a form without a
 * backend is worse than a link.
 */
export function LearnSubscribeBand() {
  return (
    <Band skin="card" layout="centered" className="py-16 sm:py-20">
      <SectionHeading
        align="center"
        title="Keep up with GraphQL in .NET"
        description="Subscribe via RSS or YouTube, or keep exploring the catalog and the docs."
      />
      <ButtonRow align="center" className="mt-8">
        <SolidButton href="/blog/rss.xml">Subscribe via RSS</SolidButton>
        <OutlineButton href="https://www.youtube.com/c/ChilliCream">YouTube</OutlineButton>
        <OutlineButton href="/learn/browse">Browse the catalog</OutlineButton>
        <OutlineButton href="/docs">Read the docs</OutlineButton>
      </ButtonRow>
    </Band>
  );
}
