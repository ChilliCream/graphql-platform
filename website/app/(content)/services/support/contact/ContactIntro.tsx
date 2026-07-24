import { CheckList } from "@/src/components/CheckList";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { SlackIcon } from "@/src/icons/brands/Slack";

const POINTS = [
  "A reply within one business day",
  "Straight to the core engineers",
  "No sales runaround",
];

/**
 * The left side of the contact panel: the pitch and what to expect, with the
 * direct channels pinned to the bottom for people who would rather not use the
 * form.
 */
export function ContactIntro() {
  return (
    <div className="flex h-full flex-col">
      <Eyebrow color="accent">Talk to us</Eyebrow>
      <h1 className="font-heading text-cc-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
        Tell us what you need.
      </h1>
      <p className="text-cc-ink-dim mt-5 text-base leading-relaxed text-pretty">
        Wherever you are and wherever you&rsquo;re headed, you&rsquo;ll hear
        back from the core team, not a first-line queue.
      </p>
      <CheckList className="mt-8" items={POINTS} />
      <div className="mt-auto pt-10">
        <div className="border-cc-card-border flex flex-col items-start gap-3 border-t pt-6 text-sm">
          <a
            href="mailto:contact@chillicream.com"
            className="text-cc-ink hover:text-cc-accent transition-colors"
          >
            contact@chillicream.com
          </a>
          <a
            href="https://slack.chillicream.com/"
            target="_blank"
            rel="noopener noreferrer"
            className="text-cc-ink-dim hover:text-cc-accent inline-flex items-center gap-2 transition-colors"
          >
            <SlackIcon className="h-4 w-4 flex-none fill-current" />
            Join the community Slack
          </a>
        </div>
      </div>
    </div>
  );
}
