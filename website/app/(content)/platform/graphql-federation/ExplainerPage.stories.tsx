import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ExplainerPage } from "./ExplainerPage";

const meta = {
  title: "Pages/Platform/GraphQLFederation",
  component: ExplainerPage,
  parameters: { layout: "fullscreen" },
  tags: ["no-snapshot"],
} satisfies Meta<typeof ExplainerPage>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
