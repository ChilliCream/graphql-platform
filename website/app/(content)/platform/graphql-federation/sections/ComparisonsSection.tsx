import NextLink from "next/link";

import { ArrowRightIcon } from "@/src/icons/ArrowRight";

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
      <div className="mt-10 grid gap-10 sm:grid-cols-2">
        <div id="vs-single-server" className="scroll-mt-24">
          <h3 className="font-heading text-cc-heading text-lg font-semibold text-balance">
            <NextLink
              href="/comparison/graphql-federation-vs-monolith"
              className="hover:text-cc-accent text-inherit no-underline transition-colors"
            >
              GraphQL Federation vs. a single GraphQL server
              <ArrowRightIcon className="ml-1.5 inline size-3.5 align-baseline" />
            </NextLink>
          </h3>
          <p className="text-cc-ink mt-2 text-base">
            A single GraphQL server is often the simplest choice for smaller
            applications. Federation becomes valuable as APIs grow and teams
            need independent ownership, development, and deployment.
          </p>
        </div>
        <div id="vs-schema-stitching" className="scroll-mt-24">
          <h3 className="font-heading text-cc-heading text-lg font-semibold text-balance">
            <NextLink
              href="/comparison/graphql-federation-vs-schema-stitching"
              className="hover:text-cc-accent text-inherit no-underline transition-colors"
            >
              GraphQL Federation vs. Schema Stitching
              <ArrowRightIcon className="ml-1.5 inline size-3.5 align-baseline" />
            </NextLink>
          </h3>
          <p className="text-cc-ink mt-2 text-base">
            Both approaches combine multiple GraphQL schemas into one API.
            Federation emphasizes explicit ownership and relationships defined
            by subgraphs, making it a natural fit for independently evolving
            services.
          </p>
        </div>
        <div id="vs-bff" className="scroll-mt-24">
          <h3 className="font-heading text-cc-heading text-lg font-semibold text-balance">
            <NextLink
              href="/comparison/graphql-federation-vs-bff"
              className="hover:text-cc-accent text-inherit no-underline transition-colors"
            >
              GraphQL Federation vs. Backend for Frontend (BFF)
              <ArrowRightIcon className="ml-1.5 inline size-3.5 align-baseline" />
            </NextLink>
          </h3>
          <p className="text-cc-ink mt-2 text-base">
            BFFs create APIs tailored to individual applications, while
            federation provides a shared graph across backend services. The two
            approaches can complement each other.
          </p>
        </div>
        <div id="vs-api-gateways" className="scroll-mt-24">
          <h3 className="font-heading text-cc-heading text-lg font-semibold text-balance">
            GraphQL Federation vs. traditional API gateways
          </h3>
          <p className="text-cc-ink mt-2 text-base">
            Traditional API gateways focus on routing, security, and traffic
            management. GraphQL federation gateways additionally understand
            relationships between services and execute queries across them.
          </p>
        </div>
      </div>
    </Section>
  );
}
