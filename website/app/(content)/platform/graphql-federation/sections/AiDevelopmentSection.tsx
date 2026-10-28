import { Section } from "./shared";

export function AiDevelopmentSection() {
  return (
    <Section id="ai-driven-development">
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold text-balance">
        GraphQL Federation for AI-driven development
      </h2>
      <div className="text-cc-ink mt-4 max-w-2xl space-y-4 text-base">
        <p>
          As AI coding agents become part of everyday software development,
          clear architectural boundaries matter more than ever.
        </p>
        <p>
          Domain-Driven Design (DDD) has long helped teams manage complexity by
          organizing software around well-defined business domains. GraphQL
          Federation builds naturally on these principles, allowing each
          subgraph to represent a distinct domain with explicit ownership and a
          strongly typed API contract.
        </p>
        <p>
          These boundaries are particularly valuable when working with AI
          agents. Instead of navigating an entire application, an agent can
          focus on a specific subgraph, understand its responsibilities, and
          make changes within a well-defined context. The GraphQL schema
          provides a clear contract, while schema composition helps detect
          incompatible changes before they reach production.
        </p>
        <p>
          This makes GraphQL Federation a compelling architecture for large
          applications built by both developers and AI agents. Teams and agents
          can work independently within their domains while contributing to one
          coherent API.
        </p>
      </div>
    </Section>
  );
}
