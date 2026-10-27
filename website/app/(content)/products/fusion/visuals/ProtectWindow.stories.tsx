import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { expect, within } from "storybook/test";

import { ProtectWindow } from "./ProtectWindow";

const PANEL_WIDTHS = {
  375: 335,
  768: 672,
  1024: 514.65625,
  1440: 720,
  1920: 720,
} as const;

const meta = {
  title: "Products/Fusion/Visuals/ProtectWindow",
  component: ProtectWindow,
  parameters: { layout: "padded" },
  tags: ["visual-reduced-motion"],
  play: async ({ canvasElement }) => {
    if (!window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      return;
    }

    await document.fonts.ready;
    const root = canvasElement.querySelector<HTMLElement>(".pw-root")!;
    const [animated, staticContent] = Array.from(root.children).filter(
      (child): child is HTMLElement => child instanceof HTMLDivElement,
    );
    expect(animated).not.toBeVisible();
    expect(staticContent).toBeVisible();
    const canvas = within(staticContent);

    for (const [version, verdict] of [
      ["v14", "breaking · publish blocked"],
      ["v15", "safe · published"],
    ]) {
      const label = canvas.getByText(version, { exact: true });
      const card = label.parentElement!.parentElement!;
      const outcome = within(card).getByText(verdict, { exact: true });
      expect(label).toBeVisible();
      expect(outcome).toBeVisible();
      expectContained(card, staticContent);
      expectContained(outcome, card);
    }

    for (const [client, outcome] of [
      ["web@2.4.0", "ok"],
      ["mobile@5.1.0", "4,213 req / 7d"],
      ["partner-api", "ok"],
    ]) {
      const label = canvas.getByText(client, { exact: true });
      const row = label.parentElement!;
      const result = within(row).getByText(outcome, { exact: true });
      expect(label).toBeVisible();
      expect(result).toBeVisible();
      expect(row.children).toHaveLength(2);
      expect(row.lastElementChild!.querySelectorAll("span")).toHaveLength(1);
      expectContained(label, row);
      expectContained(result, row);
      expect(label.getBoundingClientRect().right).toBeLessThanOrEqual(
        row.lastElementChild!.getBoundingClientRect().left,
      );
      expectContained(row, staticContent);
    }
  },
} satisfies Meta<typeof ProtectWindow>;

export default meta;
type Story = StoryObj<typeof meta>;

function expectContained(element: HTMLElement, boundary: HTMLElement) {
  const rect = element.getBoundingClientRect();
  const bounds = boundary.getBoundingClientRect();
  expect(rect.width).toBeGreaterThan(0);
  expect(rect.height).toBeGreaterThan(0);
  expect(rect.left).toBeGreaterThanOrEqual(bounds.left - 0.5);
  expect(rect.right).toBeLessThanOrEqual(bounds.right + 0.5);
  expect(rect.top).toBeGreaterThanOrEqual(bounds.top - 0.5);
  expect(rect.bottom).toBeLessThanOrEqual(bounds.bottom + 0.5);
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
