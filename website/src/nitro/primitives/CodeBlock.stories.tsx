import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { CodeBlock } from "./CodeBlock";
import { ThemeProvider } from "../lib/theme";

const GRAPHQL_QUERY = `query GetOrderSummary($id: ID!) {
  order(id: $id) {
    id
    total
    items {
      product { id name price }
      quantity
    }
    customer { id name email }
  }
}`;

const JSON_RESPONSE = `{
  "data": {
    "orderById": {
      "id": "ord_8F2KQ7",
      "status": "PROCESSING",
      "total": 129.97,
      "customer": {
        "name": "Ada Lovelace",
        "email": "ada@eshops.io"
      },
      "items": [
        { "product": { "name": "Mechanical Keyboard", "price": 89.99 }, "quantity": 1 },
        { "product": { "name": "USB-C Cable", "price": 19.99 }, "quantity": 2 }
      ]
    }
  }
}`;

const meta = {
  title: "Nitro/Primitives/CodeBlock",
  component: CodeBlock,
  parameters: { layout: "centered" },
  argTypes: {
    progress: { control: false },
  },
  args: {
    code: GRAPHQL_QUERY,
    lang: "graphql",
    gutter: true,
    caret: false,
    fontSize: 13,
    ariaLabel: "GraphQL request editor",
  },
  decorators: [
    (Story) => (
      <ThemeProvider
        theme="dark"
        reducedMotion="always"
        className="w-[560px] max-w-full p-6"
      >
        <Story />
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof CodeBlock>;

export default meta;
type Story = StoryObj<typeof meta>;

export const GraphQL: Story = {};

export const Json: Story = {
  args: {
    code: JSON_RESPONSE,
    lang: "json",
    ariaLabel: "JSON response",
  },
};
