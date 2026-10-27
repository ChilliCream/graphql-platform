import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { expect, waitFor, within } from "storybook/test";

import { InsightsWindow } from "./InsightsWindow";

const PANEL_WIDTHS = {
  375: 335,
  768: 672,
  1024: 514.65625,
  1440: 720,
  1920: 720,
} as const;

const meta = {
  title: "Products/Fusion/Visuals/InsightsWindow",
  component: InsightsWindow,
  parameters: { layout: "padded" },
  tags: ["visual-reduced-motion"],
  play: async ({ canvasElement }) => {
    if (!window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      return;
    }

    await document.fonts.ready;
    const canvas = within(canvasElement);
    await waitFor(() => {
      const cost = canvas.getByText("184", { exact: true });
      expect(cost).toBeVisible();
      expectContained(cost, canvasElement);

      const trace = canvas.getByRole("img", {
        name: "Trace waterfall: 5 spans over 150.0 ms",
      });
      for (const name of [
        "POST /graphql",
        "operation plan",
        "catalog",
        "orders",
        "billing",
      ]) {
        const label = within(trace).getByText(name, { exact: true });
        expect(label).toBeVisible();
        const caption = label.parentElement!;
        expect(getComputedStyle(caption).opacity).toBe("1");
        expectContained(caption, trace);
        const bar = caption.previousElementSibling as HTMLElement;
        expect(bar.getBoundingClientRect().width).toBeGreaterThan(1);
        expectContained(bar, trace);
      }

      const usage = canvas.getByRole("img", {
        name: /^Top 3 clients by request volume:/,
      });
      for (const name of ["web", "mobile", "partner-api"]) {
        const label = within(usage).getByText(name, { exact: true });
        expect(label).toBeVisible();
        expectContained(label, usage);
        const bar = label.nextElementSibling!.firstElementChild as HTMLElement;
        expect(bar.getBoundingClientRect().width).toBeGreaterThan(1);
        expect(bar.getBoundingClientRect().height).toBeGreaterThan(1);
        expectContained(bar, usage);
      }
    });
  },
} satisfies Meta<typeof InsightsWindow>;

export default meta;
type Story = StoryObj<typeof meta>;

function expectContained(element: HTMLElement, boundary: HTMLElement) {
  const rect = element.getBoundingClientRect();
  expect(rect.width).toBeGreaterThan(0);
  expect(rect.height).toBeGreaterThan(0);
  for (
    let ancestor = element.parentElement;
    ancestor;
    ancestor = ancestor.parentElement
  ) {
    const bounds = ancestor.getBoundingClientRect();
    const style = getComputedStyle(ancestor);
    if (style.overflowX !== "visible" || ancestor === boundary) {
      expect(rect.left).toBeGreaterThanOrEqual(bounds.left - 0.5);
      expect(rect.right).toBeLessThanOrEqual(bounds.right + 0.5);
    }
    if (style.overflowY !== "visible" || ancestor.id === "storybook-root") {
      expect(rect.top).toBeGreaterThanOrEqual(bounds.top - 0.5);
      expect(rect.bottom).toBeLessThanOrEqual(bounds.bottom + 0.5);
    }
  }
}

function panelStory(viewport: keyof typeof PANEL_WIDTHS): Story {
  return {
    decorators: [
      (Story) => (
        <div style={{ width: PANEL_WIDTHS[viewport], maxWidth: "100%" }}>
          <Story />
        </div>
      ),
    ],
  };
}

export const Panel375: Story = panelStory(375);
export const Panel768: Story = panelStory(768);
export const Panel1024: Story = panelStory(1024);
export const Panel1440: Story = panelStory(1440);
export const Panel1920: Story = panelStory(1920);
