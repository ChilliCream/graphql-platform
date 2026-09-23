import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { SectionHeading } from "@/src/components/SectionHeading";
import { Card } from "@/src/design-system/Card";

import { CARD_FOCUS_CLASSES } from "./cardFocus";

interface StandardsItemSpec {
  readonly role: "HOST" | "TWO SEATS" | "MEMBERS" | "ORGANIZERS";
  readonly accent: boolean;
  readonly name: string;
  readonly body: string;
  readonly href: string;
}

const STANDARDS_ITEMS: readonly StandardsItemSpec[] = [
  {
    role: "TWO SEATS",
    accent: true,
    name: "Technical Steering Committee",
    body: "The committee steering the GraphQL specification. Michael Staib and Pascal Senn both hold seats.",
    href: "https://github.com/graphql/graphql-wg/blob/main/GraphQL-TSC.md",
  },
  {
    role: "HOST",
    accent: true,
    name: "Composite Schemas Working Group",
    body: "Michael Staib hosts the Composite Schemas subcommittee, which develops an open standard for composing GraphQL services.",
    href: "https://github.com/graphql/composite-schemas-wg",
  },
  {
    role: "HOST",
    accent: true,
    name: "GraphQL/OpenTelemetry Working Group",
    body: "Pascal Senn hosts GraphQL/OTel, which develops OpenTelemetry conventions for GraphQL APIs.",
    href: "https://github.com/graphql/otel-wg",
  },
  {
    role: "ORGANIZERS",
    accent: true,
    name: "GraphQL Day",
    body: "Community events around the world, organized with the GraphQL Foundation team.",
    href: "https://graphql.org/day",
  },
  {
    role: "MEMBERS",
    accent: false,
    name: "GraphQL Working Group",
    body: "The main working group evolving the GraphQL specification itself.",
    href: "https://github.com/graphql/graphql-wg",
  },
  {
    role: "MEMBERS",
    accent: false,
    name: "GraphQL over HTTP",
    body: "The specification for transporting GraphQL over HTTP, so servers and clients interoperate.",
    href: "https://github.com/graphql/graphql-over-http",
  },
  {
    role: "MEMBERS",
    accent: false,
    name: "AI Working Group",
    body: "Best practices for GraphQL in AI systems and agent-powered applications.",
    href: "https://github.com/graphql/ai-wg",
  },
];

export function StandardsBand() {
  return (
    <section className="py-14 sm:py-20">
      <RevealOnScroll>
        <SectionHeading
          title="Helping write the standards we implement."
          description={
            <>
              ChilliCream contributors help shape the specifications and
              conventions the platform implements. See the{" "}
              <a
                href="https://graphql.org/community/team/"
                target="_blank"
                rel="noopener noreferrer"
                className="text-cc-accent hover:text-cc-accent-hover"
              >
                official GraphQL team page
              </a>{" "}
              for current roles and participation.
            </>
          }
        />
        <div className="mt-10 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {STANDARDS_ITEMS.map((item) => (
            <Card
              key={item.name}
              as="a"
              href={item.href}
              target="_blank"
              rel="noopener noreferrer"
              variant="tile"
              hoverBorder
              className={`group flex h-full flex-col no-underline ${CARD_FOCUS_CLASSES}`}
            >
              <p
                className={`font-mono text-[0.6rem] tracking-[0.18em] uppercase ${
                  item.accent ? "text-cc-accent" : "text-cc-ink-dim"
                }`}
              >
                {item.role}
              </p>
              <h3 className="font-heading text-cc-heading text-h6 mt-4 font-semibold">
                {item.name}
              </h3>
              <p className="text-cc-ink-dim mt-2 pb-6 text-sm">{item.body}</p>
              <div className="mt-auto flex items-center justify-between">
                <span className="text-cc-ink-dim font-mono text-[0.6rem] tracking-[0.14em] uppercase">
                  {new URL(item.href).host}
                </span>
                <span
                  aria-hidden="true"
                  className="text-cc-ink-dim group-hover:text-cc-heading transition-colors"
                >
                  ↗
                </span>
              </div>
            </Card>
          ))}
        </div>
      </RevealOnScroll>
    </section>
  );
}
