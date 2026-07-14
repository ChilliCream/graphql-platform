import { SectionHeading } from "@/src/components/SectionHeading";
import { Card } from "@/src/design-system/Card";
import { Icon, IconName } from "@/src/icons/Icon";

/**
 * As monthly consumption grows, more unlocks. Thresholds are indicative and
 * still being finalized.
 */
const UNLOCKS: {
  spend: string;
  title: string;
  description: string;
  icon: IconName;
}[] = [
  {
    spend: "$2,000 / mo",
    title: "Business Support",
    description: "Faster response times and a named support contact.",
    icon: "life-ring",
  },
  {
    spend: "$4,000 / mo",
    title: "Enterprise Support",
    description: "Priority engineering and a dedicated solution architect.",
    icon: "shield",
  },
  {
    spend: "$6,000 / mo",
    title: "BYOC",
    description: "Bring your own cloud, run Nitro in your own account.",
    icon: "cloud",
  },
];

/**
 * "Unlock more as you grow": a vertical list of spend thresholds, each unlocking
 * more support or deployment options. Modeled on Railway's pricing progression.
 */
export function UnlockBand() {
  return (
    <section
      aria-labelledby="unlock-heading"
      className="mt-24 scroll-mt-24 sm:mt-28"
      id="unlock"
    >
      <div className="mx-auto max-w-2xl">
        <SectionHeading
          align="center"
          title="Unlock more as you grow"
          titleId="unlock-heading"
          description="Commit to a minimum monthly spend to unlock more support and deployment options, up to your spend."
        />
      </div>

      <ul className="mx-auto mt-10 flex max-w-3xl flex-col gap-3">
        {UNLOCKS.map((unlock) => (
          <UnlockRow key={unlock.title} unlock={unlock} />
        ))}
      </ul>
    </section>
  );
}

function UnlockRow({ unlock }: { readonly unlock: (typeof UNLOCKS)[number] }) {
  return (
    <Card as="li" className="flex items-center gap-4 p-5 sm:gap-5 sm:p-6">
      <span className="border-cc-card-border bg-cc-surface text-cc-accent flex size-11 shrink-0 items-center justify-center rounded-xl border">
        <Icon icon={unlock.icon} />
      </span>
      <div className="min-w-0 flex-1">
        <h3 className="font-heading text-cc-heading text-base font-semibold">
          {unlock.title}
        </h3>
        <p className="text-cc-ink-dim mt-1 text-sm text-pretty">
          {unlock.description}
        </p>
      </div>
      <span className="font-heading text-cc-heading shrink-0 text-lg font-semibold sm:text-xl">
        {unlock.spend}
      </span>
    </Card>
  );
}
