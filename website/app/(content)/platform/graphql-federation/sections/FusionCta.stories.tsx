import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { expect, waitFor, within } from "storybook/test";

import { GraphSphere } from "@/src/icons/GraphSphere";
import { AppMotionConfig } from "@/src/nitro/lib/motion";

import { FusionCta } from "./FusionCta";

type CtaWidth = 375 | 768 | 1024 | 1440;

interface PreviewWindow {
  readonly __STORYBOOK_PREVIEW__?: {
    readonly currentRender?: { readonly id: string; readonly phase: string };
  };
}

const EMBEDDED_STORY_ID =
  "pages-platform-graphqlfederation-fusioncta--embedded";

const meta = {
  title: "Pages/Platform/GraphQLFederation/FusionCta",
  component: FusionCta,
  parameters: { layout: "fullscreen" },
  decorators: [
    (Story) => (
      <AppMotionConfig reducedMotion="always">
        <Story />
      </AppMotionConfig>
    ),
  ],
} satisfies Meta<typeof FusionCta>;

export default meta;
type Story = StoryObj<typeof meta>;

/** The section on its own, loaded by the width stories inside a viewport-sized frame. */
export const Embedded: Story = {
  tags: ["no-snapshot"],
};

function frameTitle(width: CtaWidth): string {
  return `Federation CTA at ${width}px`;
}

async function loadFrame(
  canvasElement: HTMLElement,
  width: CtaWidth,
): Promise<HTMLElement> {
  const frame = within(canvasElement).getByTitle(
    frameTitle(width),
  ) as HTMLIFrameElement;
  await waitFor(
    () => {
      const preview = (frame.contentWindow as PreviewWindow | null)
        ?.__STORYBOOK_PREVIEW__;
      expect(preview?.currentRender?.id).toBe(EMBEDDED_STORY_ID);
      expect(preview?.currentRender?.phase).toBe("finished");
    },
    { timeout: 15000 },
  );
  const frameDocument = frame.contentDocument;
  if (!frameDocument) {
    throw new Error("The embedded story document is not accessible.");
  }
  await frameDocument.fonts.ready;
  frame.style.height = `${frameDocument.documentElement.scrollHeight}px`;
  return frameDocument.body;
}

function ctaStory(width: CtaWidth): Story {
  return {
    tags: ["visual-reduced-motion"],
    decorators: [
      () => (
        <iframe
          title={frameTitle(width)}
          src={`/iframe.html?id=${EMBEDDED_STORY_ID}&viewMode=story`}
          width={width}
          height={100}
          className="block max-w-none border-0"
        />
      ),
    ],
    play: async ({ canvasElement }) => {
      // arrange
      const body = await loadFrame(canvasElement, width);
      const section = within(body);

      // assert
      expect(
        section.getByRole("heading", {
          level: 2,
          name: "Get started with GraphQL Federation",
        }),
      ).toBeVisible();
      expect(
        section.getByText(
          "Getting started with GraphQL Federation doesn't require rebuilding your existing APIs. With Fusion, you can bring together GraphQL, REST, and gRPC services into one unified graph.",
        ),
      ).toBeVisible();
      expect(
        section.getByText(
          "Fusion supports both Apollo Federation and the GraphQL Federation specification, allowing you to use existing subgraphs without modification and adopt the open standard at your own pace.",
        ),
      ).toBeVisible();
      expect(
        section.getByText(
          "Start with a single subgraph, connect additional services as your architecture grows, and let Fusion handle schema composition and distributed query execution.",
        ),
      ).toBeVisible();
      const links = section.getAllByRole("link", {
        name: "Get started with Fusion",
      });
      expect(links).toHaveLength(1);
      expect(links[0]).toHaveAttribute("href", "/docs/fusion/getting-started");
      const graphs = body.querySelectorAll('svg[viewBox="0 0 400 400"]');
      expect(graphs).toHaveLength(1);
      expect(graphs[0]).toHaveAttribute("aria-hidden", "true");
      expect(graphs[0].querySelectorAll(":scope > g")).toHaveLength(42);
      expect(graphs[0]).toBeVisible();
    },
  };
}

export const Width375: Story = ctaStory(375);
export const Width768: Story = ctaStory(768);
export const Width1024: Story = ctaStory(1024);
export const Width1440: Story = ctaStory(1440);

export const WithSphere: Story = {
  tags: ["visual-reduced-motion"],
  decorators: [
    (Story) => (
      <div style={{ width: 1024, maxWidth: "100%" }}>
        <Story />
        <GraphSphere
          aria-hidden="true"
          className="mx-auto aspect-square w-full max-w-[380px]"
        />
      </div>
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
