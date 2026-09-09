import { Section } from "./shared";

export function ProblemSection() {
  return (
    <Section id="problem">
      <div className="max-w-2xl">
        {/* An h3 so the page keeps exactly four section-level H2s; the
            classes match SectionHeading's own h2 at size="md". */}
        <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold text-balance">
          What problem does GraphQL Federation solve?
        </h3>
        <div className="text-cc-ink mt-5 space-y-4 text-base">
          <p>
            Most GraphQL APIs start as one server with one schema. That works
            well for a while.
          </p>
          <p>
            Then the API grows, and several teams end up working on different
            parts of the same schema. Releasing becomes the hard part. Everyone
            shares one release queue, so every change needs coordination and
            every team waits on the same bottleneck.
          </p>
          <p>
            GraphQL Federation breaks that one big schema into smaller ones.
            Each service contributes a source schema, and a service that
            contributes a source schema is called a subgraph. One per team, or
            one per domain. Each team owns its subgraph: the source schema, the
            code, the release schedule.
          </p>
          <p>
            When a team ships a change, composition checks the source schemas
            against each other and merges them into one composite schema, which
            is what your gateway serves. If the change conflicts with another
            subgraph, composition fails at build time and the change never
            reaches production. Your API consumers never notice any of this.
          </p>
        </div>
      </div>
    </Section>
  );
}
