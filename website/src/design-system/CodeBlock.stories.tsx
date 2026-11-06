import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import type { ReactElement } from "react";
import { expect, fn, userEvent, within } from "storybook/test";
import { CodeBlock } from "./CodeBlock";
import { CodeStep } from "./CodeStep";
import { lowestContrast, renderBlock, storyTheme } from "./codeBlockStoryUtils";

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

export const LightWithFilename: Story = {
  beforeEach: storyTheme("light"),
  loaders: [
    async () => ({
      rendered: await renderBlock("tsx", tsxSample, 'filename="Counter.tsx"'),
    }),
  ],
  render: (_args, ctx) => ctx.loaded.rendered as ReactElement,
  play: async ({ canvasElement }) => {
    expect(lowestContrast(canvasElement, ".shiki span")).toBeGreaterThanOrEqual(
      4.5,
    );
    expect(
      lowestContrast(canvasElement, ".code-lang-badge, figcaption span"),
    ).toBeGreaterThanOrEqual(4.5);
  },
};

export const LightWithLineHighlights: Story = {
  beforeEach: storyTheme("light"),
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
  play: async ({ canvasElement }) => {
    const highlighted = canvasElement.querySelectorAll(".line-highlighted");
    expect(highlighted).toHaveLength(3);
    for (const line of highlighted) {
      expect(
        lowestContrast(line, ".shiki span"),
        `${line.textContent} contrast`,
      ).toBeGreaterThanOrEqual(4.5);
    }
  },
};

export const LightCodeStepActive: Story = {
  beforeEach: storyTheme("light"),
  loaders: WithCodeSteps.loaders,
  render: WithCodeSteps.render,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.hover(
      canvas.getAllByText("setCount", { selector: "span.code-step" })[0],
    );

    const figure = canvasElement.querySelector("figure")!;
    expect(figure).toHaveClass("code-step-dim");
    expect(
      [...canvasElement.querySelectorAll(".code-step-active")].map(
        (el) => el.textContent,
      ),
    ).toEqual(["setCount", "setCount"]);
    expect(
      lowestContrast(canvasElement, "span.code-step, .shiki [data-step]"),
    ).toBeGreaterThanOrEqual(4.5);
  },
};

export const AutoWithLineHighlights: Story = {
  beforeEach: storyTheme("auto"),
  loaders: WithLineHighlights.loaders,
  render: WithLineHighlights.render,
};

export const LightUnknownLanguage: Story = {
  beforeEach: storyTheme("light"),
  loaders: [
    async () => ({
      rendered: await renderBlock("not-a-language", "plain unknown source"),
    }),
  ],
  render: (_args, ctx) => ctx.loaded.rendered as ReactElement,
  play: async ({ canvasElement }) => {
    expect(
      lowestContrast(canvasElement, ".shiki code span"),
    ).toBeGreaterThanOrEqual(4.5);
  },
};

const csharpSample = `builder.Services
    .AddNitro()
    .AddOpenTelemetry();`;

export const FixedThemeLight: Story = {
  beforeEach: storyTheme("light"),
  loaders: [
    async () => ({
      rendered: await renderBlock(
        "csharp",
        csharpSample,
        'filename="Program.cs" {2}',
        "poimandres",
      ),
    }),
  ],
  render: (_args, ctx) => ctx.loaded.rendered as ReactElement,
};

export const FixedThemeDark: Story = {
  loaders: FixedThemeLight.loaders,
  render: FixedThemeLight.render,
};
