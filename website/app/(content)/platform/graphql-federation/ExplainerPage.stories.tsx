import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { expect, within } from "storybook/test";

import { ExplainerPage } from "./ExplainerPage";

const meta = {
  title: "Pages/Platform/GraphQLFederation",
  component: ExplainerPage,
  parameters: { layout: "fullscreen" },
  tags: ["no-snapshot"],
} satisfies Meta<typeof ExplainerPage>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const page = canvasElement.firstElementChild;
    const sections = Array.from(page?.children ?? []);

    expect(sections.map((section) => section.tagName)).toEqual(
      Array(8).fill("SECTION"),
    );
    expect(sections.map((section) => section.id)).toEqual([
      "",
      "what-is",
      "problem",
      "how-it-works",
      "from-apollo-federation",
      "ai-driven-development",
      "federation-comparisons",
      "fusion",
    ]);
    expect(
      within(sections[0] as HTMLElement).getByRole("heading", {
        level: 1,
        name: "GraphQL Federation",
      }),
    ).toBeVisible();
    const closingSection = within(sections[7] as HTMLElement);
    expect(
      closingSection.getByRole("heading", {
        level: 2,
        name: "Get started with GraphQL Federation",
      }),
    ).toBeVisible();
    expect(
      closingSection.getByRole("link", { name: "Get started with Fusion" }),
    ).toHaveAttribute("href", "/docs/fusion/getting-started");
    expect(
      sections[7].querySelectorAll(
        'svg[viewBox="0 0 400 400"][aria-hidden="true"]',
      ),
    ).toHaveLength(1);
  },
};
