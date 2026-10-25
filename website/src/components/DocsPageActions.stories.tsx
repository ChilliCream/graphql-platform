import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { expect, fn, userEvent, within } from "storybook/test";
import { DocsPageActions } from "./DocsPageActions";

const markdown = `# Fusion subscriptions

> [!NOTE]
> Events can be resumed.

\`\`\`graphql
subscription { onProductChanged { id } }
\`\`\`
`;

const meta = {
  title: "Components/DocsPageActions",
  component: DocsPageActions,
  parameters: { layout: "fullscreen" },
  args: {
    fallbackMarkdown: markdown,
    markdownUrl: "/docs/fusion/subscriptions.md",
    title: "Fusion subscriptions",
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark bg-cc-bg min-h-48 p-8">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof DocsPageActions>;

export default meta;
type Story = StoryObj<typeof meta>;

export const CopiesGeneratedMarkdown: Story = {
  args: {
    fallbackMarkdown: undefined,
  },
  play: async ({ canvasElement }) => {
    const writeText = fn();
    const fetchMarkdown = fn(() =>
      Promise.resolve(
        new Response(markdown, {
          status: 200,
          headers: { "Content-Type": "text/markdown; charset=utf-8" },
        }),
      ),
    );
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: { writeText },
    });
    Object.defineProperty(window, "fetch", {
      configurable: true,
      value: fetchMarkdown,
    });
    const canvas = within(canvasElement);
    const copyButton = canvas.getByRole("button", {
      name: "Copy as Markdown",
    });

    copyButton.focus();
    expect(copyButton).toHaveFocus();
    await userEvent.keyboard("{Enter}");

    expect(fetchMarkdown).toHaveBeenCalledWith(
      "/docs/fusion/subscriptions.md",
      { headers: { Accept: "text/markdown" } },
    );
    expect(writeText).toHaveBeenCalledWith(markdown);
    expect(
      canvas.getByRole("button", { name: "Markdown copied" }),
    ).toHaveAttribute("data-copy-status", "copied");
  },
};

export const SharesWithNativeShare: Story = {
  play: async ({ canvasElement }) => {
    const share = fn(() => Promise.resolve());
    Object.defineProperty(navigator, "share", {
      configurable: true,
      value: share,
    });
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole("button", { name: "Share" }));

    expect(share).toHaveBeenCalledWith({
      title: "Fusion subscriptions",
      url: window.location.href,
    });
    expect(canvas.getByRole("button", { name: "Shared" })).toHaveAttribute(
      "data-share-status",
      "shared",
    );
  },
};

export const FallsBackWhenGeneratedMarkdownIsUnavailable: Story = {
  play: async ({ canvasElement }) => {
    const writeText = fn();
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: { writeText },
    });
    Object.defineProperty(window, "fetch", {
      configurable: true,
      value: fn(() =>
        Promise.resolve(
          new Response("Not found", {
            status: 404,
            headers: { "Content-Type": "text/plain" },
          }),
        ),
      ),
    });
    const canvas = within(canvasElement);

    await userEvent.click(
      canvas.getByRole("button", { name: "Copy as Markdown" }),
    );

    expect(writeText).toHaveBeenCalledWith(markdown);
    expect(
      canvas.getByRole("button", { name: "Markdown copied" }),
    ).toHaveAttribute("data-copy-status", "copied");
  },
};
