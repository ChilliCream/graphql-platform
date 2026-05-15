import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import type { ReactElement } from "react";
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

export const WithLineHighlights: Story = {
  loaders: [
    async () => ({
      rendered: await renderBlock(
        "tsx",
        tsxSample,
        'filename="Counter.tsx" {4,6-7}'
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
        'filename="Counter.tsx" [[1, 1, "useState"], [1, 4, "useState"], [2, 4, "count"], [2, 6, "count"], [2, 7, "count"], [3, 4, "setCount"], [3, 6, "setCount"]]'
      ),
    }),
  ],
  render: (_args, ctx) => (
    <div>
      {ctx.loaded.rendered as ReactElement}
      <p className="my-4 text-base leading-7 text-stone-800">
        Hover each step to highlight the matching tokens above. Call{" "}
        <CodeStep step={1}>useState</CodeStep> to declare local state, read
        the current value via <CodeStep step={2}>count</CodeStep>, and update
        it by calling <CodeStep step={3}>setCount</CodeStep> from the click
        handler.
      </p>
    </div>
  ),
};

