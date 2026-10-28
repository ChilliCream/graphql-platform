import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { expect, fn, userEvent, within } from "storybook/test";

import { ComparisonsSection } from "./ComparisonsSection";

const meta = {
  title: "Pages/Platform/GraphQLFederation/ComparisonsSection",
  component: ComparisonsSection,
  parameters: { layout: "fullscreen" },
  tags: ["no-snapshot"],
} satisfies Meta<typeof ComparisonsSection>;

export default meta;
type Story = StoryObj<typeof meta>;

export const WholeCardLinks: Story = {
  play: async ({ canvasElement }) => {
    // arrange
    const comparisons = [
      {
        href: "/comparison/graphql-federation-vs-monolith",
        heading: "GraphQL Federation vs. GraphQL Monolith",
        paragraph:
          "A single GraphQL server is often the simplest choice for smaller applications. Federation becomes valuable as APIs grow and teams need independent ownership, development, and deployment.",
      },
      {
        href: "/comparison/graphql-federation-vs-schema-stitching",
        heading: "GraphQL Federation vs. Schema Stitching",
        paragraph:
          "Both approaches combine multiple GraphQL schemas into one API. Federation emphasizes explicit ownership and relationships defined by subgraphs, making it a natural fit for independently evolving services.",
      },
      {
        href: "/comparison/graphql-federation-vs-bff",
        heading: "GraphQL Federation vs. Backend for Frontend (BFF)",
        paragraph:
          "BFFs create APIs tailored to individual applications, while federation provides a shared graph across backend services. The two approaches can complement each other.",
      },
      {
        href: "/comparison/graphql-federation-vs-individual-apis",
        heading: "GraphQL Federation vs. Individual APIs",
        paragraph:
          "Calling each service's API directly is often simplest when clients need only a few services. Federation brings those APIs together into one unified GraphQL API, so clients no longer coordinate data across services themselves.",
      },
    ];
    const links = within(canvasElement).getAllByRole("link");

    // assert
    expect(links.map((link) => link.getAttribute("href"))).toEqual(
      comparisons.map(({ href }) => href),
    );

    for (const [index, comparison] of comparisons.entries()) {
      const link = links[index];
      const card = within(link);
      const heading = card.getByRole("heading", {
        level: 3,
        name: comparison.heading,
      });
      expect(heading).toBeVisible();
      expect(heading.textContent).toBe(comparison.heading);
      expect(heading.childElementCount).toBe(0);
      const paragraph = card.getByText(comparison.paragraph, {
        exact: true,
        selector: "p",
      });
      const activateLink = fn((event: Event) => event.preventDefault());
      link.addEventListener("click", activateLink, { capture: true });

      try {
        // act
        await userEvent.click(paragraph);

        // assert
        expect(activateLink).toHaveBeenCalledTimes(1);
      } finally {
        link.removeEventListener("click", activateLink, { capture: true });
      }
    }
  },
};
