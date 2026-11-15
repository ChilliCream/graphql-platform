import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { expect, within } from "storybook/test";

import Footer from "./Footer";

const meta = {
  title: "Components/Footer",
  component: Footer,
  parameters: { layout: "fullscreen" },
  tags: ["no-snapshot"],
} satisfies Meta<typeof Footer>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const platformColumn = canvas.getByRole("heading", {
      level: 3,
      name: "Platform",
    }).parentElement;

    if (platformColumn === null) {
      throw new Error("The Platform heading must have a link column.");
    }

    expect(
      within(platformColumn)
        .getAllByRole("link")
        .map((link) => [link.textContent, link.getAttribute("href")]),
    ).toEqual([
      ["Analytics", "/platform/analytics"],
      ["Release Safety", "/platform/release-safety"],
      ["Agentic Development", "/platform/agentic-coding"],
      ["GraphQL Federation", "/platform/graphql-federation"],
      ["Ecosystem", "/platform/ecosystem"],
      ["Nitro", "/products/nitro"],
      ["Fusion", "/products/fusion"],
      ["Mocha", "/products/mocha"],
    ]);

    const documentationColumn = canvas.getByRole("heading", {
      level: 3,
      name: "Documentation",
    }).parentElement;

    if (documentationColumn === null) {
      throw new Error("The Documentation heading must have a link column.");
    }

    expect(
      within(documentationColumn).getByRole("link", { name: "Fusion" }),
    ).toHaveAttribute("href", "/docs/fusion");
  },
};
