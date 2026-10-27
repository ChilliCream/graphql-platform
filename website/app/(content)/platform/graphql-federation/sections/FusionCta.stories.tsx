import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { expect, within } from "storybook/test";

import { GraphSphere } from "@/src/icons/GraphSphere";
import { AppMotionConfig } from "@/src/nitro/lib/motion";

import { FusionCta } from "./FusionCta";

type CtaWidth = 375 | 768 | 1024 | 1440;

const meta = {
  title: "Pages/Platform/GraphQLFederation/FusionCta",
  component: FusionCta,
  parameters: { layout: "fullscreen" },
} satisfies Meta<typeof FusionCta>;

export default meta;
type Story = StoryObj<typeof meta>;

function ctaStory(width: CtaWidth): Story {
  return {
    decorators: [
      (Story) => (
        <AppMotionConfig reducedMotion="always">
          <div style={{ width, maxWidth: "100%" }}>
            <Story />
          </div>
        </AppMotionConfig>
      ),
    ],
    play: async ({ canvasElement }) => {
      // arrange
      const canvas = within(canvasElement);

      // assert
      expect(
        canvas.getByRole("heading", {
          level: 2,
          name: "Get started with GraphQL Federation",
        }),
      ).toBeVisible();
      expect(
        canvas.getByText(
          "Getting started with GraphQL Federation doesn't require rebuilding your existing APIs. With Fusion, you can bring together GraphQL, REST, and gRPC services into one unified graph.",
        ),
      ).toBeVisible();
      expect(
        canvas.getByText(
          "Fusion supports both Apollo Federation and the GraphQL Federation specification, allowing you to use existing subgraphs without modification and adopt the open standard at your own pace.",
        ),
      ).toBeVisible();
      expect(
        canvas.getByText(
          "Start with a single subgraph, connect additional services as your architecture grows, and let Fusion handle schema composition and distributed query execution.",
        ),
      ).toBeVisible();
      const links = canvas.getAllByRole("link", {
        name: "Get started with Fusion",
      });
      expect(links).toHaveLength(1);
      expect(links[0]).toHaveAttribute("href", "/docs/fusion/getting-started");
      const graphs = canvasElement.querySelectorAll(
        'svg[viewBox="0 0 400 400"]',
      );
      expect(graphs).toHaveLength(1);
      expect(graphs[0]).toHaveAttribute("aria-hidden", "true");
      expect(graphs[0].querySelectorAll(":scope > g")).toHaveLength(42);
    },
  };
}

export const Width375: Story = ctaStory(375);
export const Width768: Story = ctaStory(768);
export const Width1024: Story = ctaStory(1024);
export const Width1440: Story = ctaStory(1440);

export const WithSphere: Story = {
  decorators: [
    (Story) => (
      <AppMotionConfig reducedMotion="always">
        <div style={{ width: 1024, maxWidth: "100%" }}>
          <Story />
          <GraphSphere
            aria-hidden="true"
            className="mx-auto aspect-square w-full max-w-[380px]"
          />
        </div>
      </AppMotionConfig>
    ),
  ],
  play: async ({ canvasElement }) => {
    // arrange
    const graphs = canvasElement.querySelectorAll('svg[viewBox="0 0 400 400"]');
    const gradientIds = Array.from(
      canvasElement.querySelectorAll("radialGradient"),
    ).map((gradient) => gradient.id);
    const referencedIds = Array.from(graphs).flatMap((graph) =>
      Array.from(graph.querySelectorAll("[fill^='url(#']")).map((node) =>
        (node.getAttribute("fill") ?? "").slice(5, -1),
      ),
    );

    // assert
    expect(graphs).toHaveLength(2);
    expect(gradientIds).toHaveLength(4);
    expect(new Set(gradientIds).size).toBe(4);
    expect(new Set(referencedIds)).toEqual(new Set(gradientIds));
  },
};
