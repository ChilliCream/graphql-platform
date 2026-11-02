import { SpecLineageTimeline } from "../visuals/SpecLineageTimeline";
import { Section } from "./shared";

export function FromApolloSection() {
  return (
    <Section id="from-apollo-federation">
      <div className="grid items-center gap-10 lg:grid-cols-12 lg:gap-16">
        <div className="min-w-0 lg:col-span-7">
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold text-balance">
            From Apollo Federation to GraphQL Federation
          </h2>
          <div className="text-cc-ink mt-4 space-y-4 text-base">
            <p>
              Apollo introduced federation in 2019, establishing the foundation
              for building distributed GraphQL APIs. GraphQL Federation takes
              these ideas forward as an open, vendor-neutral specification
              developed under the GraphQL Foundation, with contributions from
              Apollo, ChilliCream, IBM, Netflix, The Guild, WunderGraph, and the
              broader GraphQL community.
            </p>
            <p>
              The new specification builds on Apollo Federation and GraphQL
              Fusion while establishing a common standard for schema composition
              and distributed query execution. It enables different GraphQL
              servers, gateways, and tools to work together without depending on
              a single vendor&apos;s technology.
            </p>
            <p>
              Existing Apollo Federation subgraphs can continue to work without
              modification. Implementations such as Fusion support both
              approaches, allowing organizations to adopt GraphQL Federation
              incrementally, combine existing Apollo Federation subgraphs with
              new ones, and evolve their architecture at their own pace.
            </p>
          </div>
        </div>
        <div className="min-w-0 justify-self-center lg:col-span-5">
          <SpecLineageTimeline />
        </div>
      </div>
    </Section>
  );
}
