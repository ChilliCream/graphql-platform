import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Dropdown } from "./Dropdown";

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

export const Closed: Story = {
  args: {
    trigger: <span className="font-medium">Hot Chocolate</span>,
    children: (
      <ul className="m-0 list-none p-1">
        {[
          { label: "Hot Chocolate", active: true },
          { label: "Fusion", active: false },
          { label: "Nitro", active: false },
        ].map((item) => (
          <li key={item.label}>
            <a
              href="#"
              aria-current={item.active ? "page" : undefined}
              className={`block rounded px-2 py-2 no-underline transition-colors ${
                item.active
                  ? "bg-cc-accent/10 text-cc-accent"
                  : "text-cc-ink hover:bg-cc-hover"
              }`}
            >
              {item.label}
            </a>
          </li>
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

export const WithDescriptions: Story = {
  args: {
    trigger: <span className="font-medium">Hot Chocolate</span>,
    defaultOpen: true,
    children: (
      <ul className="m-0 list-none p-1">
        {[
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
        ].map((p) => (
          <li key={p.title}>
            <a
              href="#"
              aria-current={p.active ? "page" : undefined}
              className={`block rounded px-3 py-2 no-underline transition-colors ${
                p.active
                  ? "bg-cc-accent/10 text-cc-accent"
                  : "text-cc-ink hover:bg-cc-hover"
              }`}
            >
              <div className="text-sm font-medium">{p.title}</div>
              <div
                className={`text-xs ${
                  p.active ? "text-cc-accent/80" : "text-cc-ink-dim"
                }`}
              >
                {p.description}
              </div>
            </a>
          </li>
        ))}
      </ul>
    ),
  },
};
