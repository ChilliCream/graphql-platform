import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import type { ReactElement } from "react";
import { InlineCode } from "./InlineCode";

const meta = {
  title: "Design System/InlineCode",
  component: InlineCode,
} satisfies Meta<typeof InlineCode>;

export default meta;
type Story = StoryObj<typeof meta>;

// Highlighted InlineCode renders through an async server component (uses
// shiki's `codeToHtml`). Storybook's renderer doesn't render async components
// on the client, so these stories resolve the element tree in a `loaders` hook
// and render the captured node synchronously.
async function renderInlineCode(
  language: string,
  code: string,
): Promise<ReactElement> {
  let node: ReactElement = InlineCode({
    "data-language": language,
    children: code,
  });
  while (typeof node.type === "function") {
    const component = node.type as (
      props: unknown,
    ) => ReactElement | Promise<ReactElement>;
    node = await component(node.props);
  }
  return node;
}

export const InProse: Story = {
  args: { children: "" },
  render: () => (
    <p className="text-base text-stone-800">
      Use the <InlineCode>useState</InlineCode> hook to add local state to a
      component, then pass the value down via props or read it from{" "}
      <InlineCode>context</InlineCode>.
    </p>
  ),
};

export const Highlighted: Story = {
  args: { children: "" },
  loaders: [
    async () => ({
      sdl: await renderInlineCode("sdl", "directive @oneOf on INPUT_OBJECT"),
      csharp: await renderInlineCode(
        "csharp",
        'builder.AddGraphQL("products-api")',
      ),
      shell: await renderInlineCode("shell", "dotnet run -- schema export"),
    }),
  ],
  render: (_args, { loaded }) => (
    <p className="text-base text-stone-800">
      Declare {loaded.sdl as ReactElement}, register{" "}
      {loaded.csharp as ReactElement}, then run {loaded.shell as ReactElement}.
    </p>
  ),
};

export const UnknownLanguage: Story = {
  args: { children: "" },
  loaders: [
    async () => ({
      code: await renderInlineCode(
        "unknown",
        "directive @oneOf on INPUT_OBJECT",
      ),
    }),
  ],
  render: (_args, { loaded }) => (
    <p className="text-base text-stone-800">
      An unknown language renders as plain inline code:{" "}
      {loaded.code as ReactElement}.
    </p>
  ),
};
