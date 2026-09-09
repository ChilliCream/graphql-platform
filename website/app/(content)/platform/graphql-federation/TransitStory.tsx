import { Section } from "./sections/shared";

export function TransitStory() {
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
            A product page shows a name, a price, a delivery estimate, and who
            is signed in. Inside the company those fields live in different
            services, each owned by a team that ships on its own schedule. The
            page does not care about the split: it wants one API.
          </p>
          <p>
            The usual answers give up one side. Let every app call every service
            and assemble the answers itself, and every app repeats that work and
            breaks whenever a service changes. Put one API and one team in front
            of everything, and every change from every team waits in that
            team&apos;s queue.
          </p>
          <p>
            GraphQL Federation keeps both. Each team publishes a source schema
            for its subgraph. Composition combines those source schemas into one
            composite schema before anything deploys, so a conflict fails the
            build instead of the gateway. A gateway serves that schema, and
            clients send one query while teams keep shipping alone.
          </p>
        </div>
      </div>
    </Section>
  );
}
