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
        <li>
          <a
            className="block rounded px-2 py-2 no-underline hover:bg-slate-50"
            href="#"
          >
            Hot Chocolate
          </a>
        </li>
        <li>
          <a
            className="block rounded px-2 py-2 no-underline hover:bg-slate-50"
            href="#"
          >
            Fusion
          </a>
        </li>
        <li>
          <a
            className="block rounded px-2 py-2 no-underline hover:bg-slate-50"
            href="#"
          >
            Nitro
          </a>
        </li>
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
          { title: "Hot Chocolate", description: "GraphQL Server / Gateway" },
          { title: "Fusion", description: "Federated GraphQL Gateway" },
          { title: "Strawberry Shake", description: "GraphQL Client for .NET" },
        ].map((p) => (
          <li key={p.title}>
            <a
              href="#"
              className="block rounded px-3 py-2 no-underline transition-colors hover:bg-slate-50"
            >
              <div className="text-sm font-medium text-slate-900">
                {p.title}
              </div>
              <div className="text-xs text-slate-500">{p.description}</div>
            </a>
          </li>
        ))}
      </ul>
    ),
  },
};
