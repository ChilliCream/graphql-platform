import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { KeyValueChip } from "./KeyValueChip";

const meta = {
  title: "Components/KeyValueChip",
  component: KeyValueChip,
  parameters: { layout: "fullscreen" },
  argTypes: {
    label: { control: "text" },
    value: { control: "text" },
    valueAs: { control: "select", options: ["code", "span"] },
    order: { control: "select", options: ["label-first", "value-first"] },
    justify: { control: "select", options: ["between", "start"] },
    density: { control: "select", options: ["compact", "cozy"] },
    labelTracking: { control: "select", options: ["wide", "normal"] },
    labelTruncate: { control: "boolean" },
    labelWidth: { control: "text" },
    className: { control: "text" },
  },
  args: {
    label: "Query",
    value: "[Query]",
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="max-w-sm">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof KeyValueChip>;

export default meta;
type Story = StoryObj<typeof meta>;

/** PatternsTile row: justify-between, wide tracking, truncating label, code value. */
export const PatternRow: Story = {
  args: {
    label: "DataLoader",
    value: "[DataLoader]",
    labelTruncate: true,
  },
};

/** FeedbackTile row: cozy density, fixed label width, normal tracking, plain span value. */
export const FeedbackRow: Story = {
  args: {
    label: "agent patch",
    value: "remove Product.price",
    valueAs: "span",
    justify: "start",
    density: "cozy",
    labelTracking: "normal",
    labelWidth: "6rem",
  },
};

/** McpSection row: value first, plain ink span, normal tracking, no truncation. */
export const McpToolRow: Story = {
  args: {
    label: "idempotent",
    value: "getProduct",
    valueAs: "span",
    order: "value-first",
    labelTracking: "normal",
  },
};
