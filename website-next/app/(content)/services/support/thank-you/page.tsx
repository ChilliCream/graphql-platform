import Link from "next/link";

import { PageHero } from "@/src/components/SectionTitle";

export default function ThankYouPage() {
  return (
    <>
      <PageHero
        title="Thank You!"
        teaser="We've received your message and will be in touch shortly. In the meantime, browse our docs or join the community Slack."
      />
      <div className="flex flex-wrap justify-center gap-4 pb-16">
        <Link
          href="/docs/hotchocolate"
          className="inline-flex items-center rounded-full bg-[var(--cc-ink)] px-7 py-3 text-sm font-medium text-[#0c1322] no-underline transition-colors hover:bg-white"
        >
          Read the Docs
        </Link>
        <Link
          href="https://slack.chillicream.com/"
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center rounded-full border border-[var(--cc-card-border)] px-7 py-3 text-sm font-medium text-[var(--cc-ink)] no-underline transition-colors hover:border-[var(--cc-card-border-hover)] hover:text-[var(--cc-ink)]"
        >
          Join Slack
        </Link>
      </div>
    </>
  );
}
