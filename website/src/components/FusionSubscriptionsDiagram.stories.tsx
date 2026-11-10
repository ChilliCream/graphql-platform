import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { expect, userEvent, waitFor, within } from "storybook/test";
import { textContrast } from "@/src/design-system/codeBlockStoryUtils";
import { storyThemePreference, withThemeProvider } from "./diagramStoryUtils";
import { FusionSubscriptionsDiagram } from "./FusionSubscriptionsDiagram";

const meta = {
  title: "Components/FusionSubscriptionsDiagram",
  component: FusionSubscriptionsDiagram,
  decorators: [withThemeProvider],
} satisfies Meta<typeof FusionSubscriptionsDiagram>;

export default meta;
type Story = StoryObj<typeof meta>;

async function waitForDiagram(canvasElement: HTMLElement) {
  const canvas = within(canvasElement);
  await waitFor(
    () => expect(canvas.getByText("Fusion Gateway")).toBeVisible(),
    { timeout: 15000 },
  );
  await waitFor(() => expect(canvas.getByText("event stream")).toBeVisible());
  return canvas;
}

export const Dark: Story = {
  beforeEach: storyThemePreference("dark"),
  play: async ({ canvasElement }) => {
    await waitForDiagram(canvasElement);
  },
};

export const Light: Story = {
  beforeEach: storyThemePreference("light"),
  play: async ({ canvasElement }) => {
    const canvas = await waitForDiagram(canvasElement);
    for (const label of [
      "Fusion Gateway",
      "Message Broker",
      "Client A",
      "Event stream (read)",
      "GraphQL over HTTP",
    ]) {
      expect(
        textContrast(canvas.getByText(label)),
        label,
      ).toBeGreaterThanOrEqual(4.5);
    }
  },
};

export const LightExpanded: Story = {
  beforeEach: storyThemePreference("light"),
  play: async ({ canvasElement }) => {
    const canvas = await waitForDiagram(canvasElement);
    await userEvent.click(
      canvas.getByRole("button", { name: "Expand diagram" }),
    );

    await canvas.findByRole("button", { name: "Close diagram" });
    await waitFor(() =>
      expect(canvas.getAllByText("Fusion Gateway")).toHaveLength(2),
    );
  },
};
