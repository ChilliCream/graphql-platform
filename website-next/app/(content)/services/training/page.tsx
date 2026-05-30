import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";

interface CorporateService {
  kind: string;
  description: string;
  perks: string[];
}

const SERVICES: CorporateService[] = [
  {
    kind: "Corporate Training",
    description:
      "Get your team trained in GraphQL, any of our products, and even React/Relay. Beginner Team? Advanced Team? Or Mixed? Don't panic! Our curriculum is designed to teach in-depth and works really well, but isn't set in stone.",
    perks: [
      "Level up their proficiency",
      "Catered to different skills",
      "Overcome challenges they've been wrestling with",
      "Get everybody on the same technical page",
    ],
  },
  {
    kind: "Corporate Workshop",
    description:
      "We will look at how to build a GraphQL server with ASP.NET Core 7 and Hot Chocolate. You will learn how to explore and manage large schemas. Further, we will dive into React and explore how to efficiently build fast and fluent web interfaces using Relay.",
    perks: [
      "Core concepts and advanced",
      "Deepen knowledge of GraphQL API",
      "Work on a real project",
      "Scale and production quirks",
      "Level up your entire team at once",
      "Have Lots of Fun!",
    ],
  },
];

export default function TrainingPage() {
  return (
    <>
      <PageHero
        title="Learning Is Easier From Experts"
        teaser="At ChilliCream, we want you to be successful. We'll tell you how it is, and what you need to get there."
      />
      <Section title="Corporate Offers">
        <div className="grid gap-6 md:grid-cols-2">
          {SERVICES.map((service) => (
            <div
              key={service.kind}
              className="flex flex-col rounded-xl border border-cc-card-border bg-cc-card-bg backdrop-blur-sm p-8 "
            >
              <h3 className="text-2xl font-semibold text-cc-ink">
                {service.kind}
              </h3>
              <p className="mt-3 text-sm text-cc-ink-dim">
                {service.description}
              </p>
              <ul className="mt-6 flex flex-1 flex-col gap-2 text-sm text-cc-ink">
                {service.perks.map((perk) => (
                  <li key={perk} className="flex items-start gap-2">
                    <span aria-hidden className="mt-1 text-fuchsia-400">
                      ✓
                    </span>
                    <span>{perk}</span>
                  </li>
                ))}
              </ul>
              <a
                href="mailto:contact@chillicream.com?subject=Corporate Offers"
                className="mt-8 inline-flex items-center justify-center rounded-md bg-cc-ink px-6 py-2.5 text-sm font-medium text-[#0c1322] no-underline transition-colors hover:bg-white"
              >
                Talk to us
              </a>
            </div>
          ))}
        </div>
      </Section>
    </>
  );
}
