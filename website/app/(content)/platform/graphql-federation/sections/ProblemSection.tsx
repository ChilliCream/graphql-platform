import { Eyebrow } from "@/src/design-system/Eyebrow";
import {
  CompositionWindow,
  ReleaseQueueWindow,
} from "../visuals/ProblemWindows";
import { Section } from "./shared";

export function ProblemSection() {
  return (
    <Section id="problem">
      <div className="grid lg:grid-cols-12 lg:gap-16">
        {/* An h3 so the page keeps exactly four section-level H2s; the
            classes match SectionHeading's own h2 at size="md". */}
        <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold text-balance lg:col-span-5">
          What problem does GraphQL Federation solve?
        </h3>
      </div>
      <div className="mt-10 grid items-center gap-10 lg:grid-cols-12 lg:gap-16">
        <div className="min-w-0 lg:col-span-5">
          <Eyebrow color="ink-dim">One schema, one release process</Eyebrow>
          <div className="text-cc-ink mt-3 space-y-4 text-base">
            <p>
              Many GraphQL APIs start with a single server and a single schema.
              This approach is simple to develop, deploy, and maintain,
              especially when one team owns the entire API.
            </p>
            <p>
              As an organization grows, multiple teams may need to contribute to
              the same graph. With a shared server and deployment process,
              changes often require coordination across teams. Releases become
              more complex, and teams can find themselves waiting on one
              another.
            </p>
          </div>
        </div>
        <div className="min-w-0 lg:col-span-7">
          <ReleaseQueueWindow />
        </div>
      </div>
      <div className="mt-16 grid items-center gap-10 sm:mt-24 lg:grid-cols-12 lg:gap-16">
        <div className="min-w-0 lg:order-2 lg:col-span-5">
          <Eyebrow color="ink-dim">With GraphQL Federation</Eyebrow>
          <div className="text-cc-ink mt-3 space-y-4 text-base">
            <p>
              GraphQL Federation removes the need for every team to contribute
              to the same server. Instead, each team can own a subgraph with its
              own schema, implementation, and deployment lifecycle.
            </p>
            <p>
              Schema composition combines these independently maintained schemas
              into one composite schema. It also detects conflicts and
              incompatible changes before a new composition is deployed, helping
              protect the integrity of the overall API.
            </p>
            <p>
              Teams can evolve their services independently while clients
              continue to interact with one unified GraphQL API.
            </p>
          </div>
        </div>
        <div className="min-w-0 lg:order-1 lg:col-span-7">
          <CompositionWindow />
        </div>
      </div>
    </Section>
  );
}
