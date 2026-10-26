import NextLink from "next/link";

import { Section } from "./shared";

export function ComparisonsSection() {
  return (
    <Section id="federation-comparisons">
      <div className="max-w-2xl">
        <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold text-balance">
          How GraphQL Federation compares
        </h2>
        <p className="text-cc-ink mt-4 text-base">
          GraphQL Federation is particularly well suited to large APIs with
          multiple independently developed services. Other approaches may be
          simpler depending on your architecture and requirements.
        </p>
      </div>
      <div className="mt-10 grid gap-6 sm:grid-cols-2">
        <NextLink
          id="vs-single-server"
          href="/comparison/graphql-federation-vs-monolith"
          className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover hover:bg-cc-surface focus-visible:outline-cc-accent block scroll-mt-24 rounded-2xl border p-6 no-underline transition-colors focus-visible:outline-2 focus-visible:outline-offset-4"
        >
          <h3 className="font-heading text-cc-heading text-lg font-semibold text-balance">
            GraphQL Federation vs. a single GraphQL server
          </h3>
          <p className="text-cc-ink mt-2 text-base">
            A single GraphQL server is often the simplest choice for smaller
            applications. Federation becomes valuable as APIs grow and teams
            need independent ownership, development, and deployment.
          </p>
        </NextLink>
        <NextLink
          id="vs-schema-stitching"
          href="/comparison/graphql-federation-vs-schema-stitching"
          className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover hover:bg-cc-surface focus-visible:outline-cc-accent block scroll-mt-24 rounded-2xl border p-6 no-underline transition-colors focus-visible:outline-2 focus-visible:outline-offset-4"
        >
          <h3 className="font-heading text-cc-heading text-lg font-semibold text-balance">
            GraphQL Federation vs. Schema Stitching
          </h3>
          <p className="text-cc-ink mt-2 text-base">
            Both approaches combine multiple GraphQL schemas into one API.
            Federation emphasizes explicit ownership and relationships defined
            by subgraphs, making it a natural fit for independently evolving
            services.
          </p>
        </NextLink>
        <NextLink
          id="vs-bff"
          href="/comparison/graphql-federation-vs-bff"
          className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover hover:bg-cc-surface focus-visible:outline-cc-accent block scroll-mt-24 rounded-2xl border p-6 no-underline transition-colors focus-visible:outline-2 focus-visible:outline-offset-4"
        >
          <h3 className="font-heading text-cc-heading text-lg font-semibold text-balance">
            GraphQL Federation vs. Backend for Frontend (BFF)
          </h3>
          <p className="text-cc-ink mt-2 text-base">
            BFFs create APIs tailored to individual applications, while
            federation provides a shared graph across backend services. The two
            approaches can complement each other.
          </p>
        </NextLink>
        <NextLink
          id="vs-individual-apis"
          href="/comparison/graphql-federation-vs-individual-apis"
          className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover hover:bg-cc-surface focus-visible:outline-cc-accent block scroll-mt-24 rounded-2xl border p-6 no-underline transition-colors focus-visible:outline-2 focus-visible:outline-offset-4"
        >
          <h3 className="font-heading text-cc-heading text-lg font-semibold text-balance">
            GraphQL Federation vs. individual APIs
          </h3>
          <p className="text-cc-ink mt-2 text-base">
            Calling each service&apos;s API directly is often simplest when
            clients need only a few services. Federation brings those APIs
            together into one unified GraphQL API, so clients no longer
            coordinate data across services themselves.
          </p>
        </NextLink>
      </div>
    </Section>
  );
}
