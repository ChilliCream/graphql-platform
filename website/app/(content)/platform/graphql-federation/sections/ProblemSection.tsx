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
            Most GraphQL APIs start as one server with one schema, and that
            holds up until the API grows. More teams change the same schema,
            every deploy carries everyone&apos;s changes, and one mistake takes
            the whole API down. Each change waits on coordination, so shipping
            slows down.
          </p>
          <p>
            GraphQL Federation splits that API into subgraphs, one per team or
            domain; a GraphQL schema in front of an existing service makes that
            service a subgraph too. Each team owns its subgraph&apos;s schema,
            code, and release schedule. Composition checks the source schemas
            against each other and combines them into one composite schema
            before anything deploys, so a conflict fails the build instead of
            production.
          </p>
          <p>
            Clients see none of the split. They send one query to one endpoint,
            served by a gateway whose distributed executor fetches from the
            subgraphs and returns one response.
          </p>
        </div>
      </div>
    </Section>
  );
}
