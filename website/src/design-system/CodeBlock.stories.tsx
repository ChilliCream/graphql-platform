import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import type { ReactElement } from "react";
import { expect, fn, userEvent, within } from "storybook/test";
import { CodeBlock } from "./CodeBlock";
import { CodeStep } from "./CodeStep";
import { renderBlock } from "./codeBlockStoryUtils";

const meta = {
  title: "Design System/CodeBlock",
  component: CodeBlock,
} satisfies Meta<typeof CodeBlock>;

export default meta;
type Story = StoryObj<typeof meta>;

const tsxSample = `import { useState } from "react";

export function Counter() {
  const [count, setCount] = useState(0);
  return (
    <button onClick={() => setCount(count + 1)}>
      Clicked {count} times
    </button>
  );
}`;

export const WithFilename: Story = {
  loaders: [
    async () => ({
      rendered: await renderBlock("tsx", tsxSample, 'filename="Counter.tsx"'),
    }),
  ],
  render: (_args, ctx) => ctx.loaded.rendered as ReactElement,
};

export const CopiesExactSource: Story = {
  loaders: [
    async () => ({
      rendered: await renderBlock("tsx", tsxSample, 'filename="Counter.tsx"'),
    }),
  ],
  render: (_args, ctx) => ctx.loaded.rendered as ReactElement,
  play: async ({ canvasElement }) => {
    const writeText = fn();
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: { writeText },
    });
    const canvas = within(canvasElement);
    const copyButton = canvas.getByRole("button", { name: "Copy code" });

    await userEvent.tab();
    expect(copyButton).toHaveFocus();
    await userEvent.keyboard("{Enter}");

    expect(writeText).toHaveBeenCalledWith(tsxSample);
    expect(canvas.getByRole("button", { name: "Code copied" })).toHaveAttribute(
      "data-copy-status",
      "copied",
    );
  },
};

export const ShowsClipboardError: Story = {
  loaders: [
    async () => ({
      rendered: await renderBlock("tsx", tsxSample, 'filename="Counter.tsx"'),
    }),
  ],
  render: (_args, ctx) => ctx.loaded.rendered as ReactElement,
  play: async ({ canvasElement }) => {
    const writeText = fn(() =>
      Promise.reject(new Error("Clipboard unavailable")),
    );
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: { writeText },
    });
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole("button", { name: "Copy code" }));

    expect(writeText).toHaveBeenCalledWith(tsxSample);
    expect(
      canvas.getByRole("button", { name: "Could not copy code" }),
    ).toHaveAttribute("data-copy-status", "error");
  },
};

export const WithLineHighlights: Story = {
  loaders: [
    async () => ({
      rendered: await renderBlock(
        "tsx",
        tsxSample,
        'filename="Counter.tsx" {4,6-7}',
      ),
    }),
  ],
  render: (_args, ctx) => ctx.loaded.rendered as ReactElement,
};

export const WithCodeSteps: Story = {
  loaders: [
    async () => ({
      rendered: await renderBlock(
        "tsx",
        tsxSample,
        'filename="Counter.tsx" [[1, 1, "useState"], [1, 4, "useState"], [2, 4, "count"], [2, 6, "count"], [2, 7, "count"], [3, 4, "setCount"], [3, 6, "setCount"], [4, 3, "Counter"]]',
      ),
    }),
  ],
  render: (_args, ctx) => (
    <div>
      {ctx.loaded.rendered as ReactElement}
      <p className="text-cc-ink-dim my-4 text-base leading-7">
        Hover each step to highlight the matching tokens above. Inside the{" "}
        <CodeStep step={4}>Counter</CodeStep> component, call{" "}
        <CodeStep step={1}>useState</CodeStep> to declare local state, read the
        current value via <CodeStep step={2}>count</CodeStep>, and update it by
        calling <CodeStep step={3}>setCount</CodeStep> from the click handler.
      </p>
    </div>
  ),
};
