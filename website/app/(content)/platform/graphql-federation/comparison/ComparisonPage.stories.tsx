import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { SubHeading } from "../sections/shared";
import { ComparisonPage } from "./ComparisonPage";

const meta = {
  title: "Pages/Platform/GraphQLFederation/Comparison",
  component: ComparisonPage,
  parameters: { layout: "fullscreen" },
  tags: ["no-snapshot"],
  args: {
    slug: "vs-bff",
    eyebrow: "Placeholder comparison",
    title: "GraphQL Federation vs the placeholder alternative",
    intro: (
      <p>
        Placeholder intro paragraph. It says in two sentences what the two
        approaches have in common and where they part ways.
      </p>
    ),
    sections: [
      {
        id: "the-alternative",
        heading: "How the alternative works",
        body: (
          <>
            <p>
              Placeholder body paragraph describing the alternative approach and
              the shape of the work it asks for.
            </p>
            <p>A second placeholder paragraph, for spacing.</p>
          </>
        ),
      },
      {
        id: "how-federation-differs",
        heading: "How GraphQL Federation differs",
        body: (
          <>
            <p>
              Placeholder body paragraph describing what changes once the
              schemas, rather than the services, are composed.
            </p>
            <SubHeading id="a-sub-block">A sub-block heading</SubHeading>
            <p>Placeholder paragraph under the sub-heading.</p>
          </>
        ),
      },
    ],
    verdict: {
      federationWins: [
        "Placeholder case for GraphQL Federation.",
        "A second placeholder case, longer, so the column shows how a wrapped list item reads.",
      ],
      alternativeWins: [
        "Placeholder case for the alternative.",
        "A second placeholder case for the alternative.",
      ],
      alternativeLabel: "the placeholder alternative",
    },
  },
} satisfies Meta<typeof ComparisonPage>;

export default meta;
type Story = StoryObj<typeof meta>;

/** The layout with its default related links: the other three comparisons. */
export const Default: Story = {};
