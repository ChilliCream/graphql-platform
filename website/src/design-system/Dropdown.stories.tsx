import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Dropdown, DropdownItem } from "./Dropdown";

const meta = {
  title: "Design System/Dropdown",
  component: Dropdown,
  parameters: {
    layout: "padded",
  },
  decorators: [
    (Story) => (
      <div style={{ width: 280 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof Dropdown>;

export default meta;
type Story = StoryObj<typeof meta>;

const PRODUCTS = [
  { label: "Hot Chocolate", active: true },
  { label: "Fusion", active: false },
  { label: "Nitro", active: false },
];

export const Closed: Story = {
  args: {
    trigger: <span className="font-medium">Hot Chocolate</span>,
    children: (
      <ul className="m-0 flex list-none flex-col p-1">
        {PRODUCTS.map((item) => (
          <DropdownItem key={item.label} href="#" active={item.active}>
            {item.label}
          </DropdownItem>
        ))}
      </ul>
    ),
  },
};

export const Open: Story = {
  args: {
    ...Closed.args,
    defaultOpen: true,
  },
};

export const WithLabel: Story = {
  args: {
    ...Closed.args,
    label: "Product",
  },
};

const PRODUCTS_WITH_DESCRIPTIONS = [
  {
    title: "Hot Chocolate",
    description: "GraphQL Server / Gateway",
    active: true,
  },
  {
    title: "Fusion",
    description: "Federated GraphQL Gateway",
    active: false,
  },
  {
    title: "Strawberry Shake",
    description: "GraphQL Client for .NET",
    active: false,
  },
];

export const WithDescriptions: Story = {
  args: {
    trigger: <span className="font-medium">Hot Chocolate</span>,
    defaultOpen: true,
    children: (
      <ul className="m-0 flex list-none flex-col p-1">
        {PRODUCTS_WITH_DESCRIPTIONS.map((p) => (
          <DropdownItem
            key={p.title}
            href="#"
            active={p.active}
            description={p.description}
          >
            {p.title}
          </DropdownItem>
        ))}
      </ul>
    ),
  },
};
