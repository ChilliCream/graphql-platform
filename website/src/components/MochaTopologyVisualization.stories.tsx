import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { expect, userEvent, waitFor, within } from "storybook/test";
import {
  storyTheme,
  textContrast,
} from "@/src/design-system/codeBlockStoryUtils";
import { MochaTopologyVisualization } from "./MochaTopologyVisualization";

const data = JSON.stringify({
  services: [
    {
      host: {
        serviceName: "OrderService",
        assemblyName: "OrderService.dll",
        instanceId: "order-svc-1",
      },
      messageTypes: [
        {
          identity: "msg:OrderPlaced",
          runtimeType: "OrderPlaced",
          runtimeTypeFullName: "MyApp.Messages.OrderPlaced",
          isInterface: false,
          isInternal: false,
        },
      ],
      consumers: [],
      routes: {
        inbound: [],
        outbound: [
          {
            kind: "publish",
            messageTypeIdentity: "msg:OrderPlaced",
            endpoint: {
              name: "e/order-placed",
              address: "rabbitmq://localhost/e/order-placed",
              transportName: "RabbitMQ",
            },
          },
        ],
      },
      sagas: [],
    },
    {
      host: {
        serviceName: "BillingService",
        assemblyName: "BillingService.dll",
        instanceId: "billing-svc-1",
      },
      messageTypes: [
        {
          identity: "msg:OrderPlaced",
          runtimeType: "OrderPlaced",
          runtimeTypeFullName: "MyApp.Messages.OrderPlaced",
          isInterface: false,
          isInternal: false,
        },
      ],
      consumers: [
        {
          name: "OrderPlacedHandler",
          identityType: "OrderPlacedHandler",
          identityTypeFullName: "MyApp.Handlers.OrderPlacedHandler",
        },
      ],
      routes: {
        inbound: [
          {
            kind: "subscribe",
            messageTypeIdentity: "msg:OrderPlaced",
            consumerName: "OrderPlacedHandler",
            endpoint: {
              name: "billing.order-placed",
              address: "rabbitmq://localhost/billing.order-placed",
              transportName: "RabbitMQ",
            },
          },
        ],
        outbound: [],
      },
      sagas: [],
    },
  ],
  transports: [
    {
      identifier: "rabbitmq://localhost:5672/",
      name: "RabbitMQ",
      schema: "rabbitmq",
      transportType: "RabbitMQMessagingTransport",
      receiveEndpoints: [
        {
          name: "billing.order-placed",
          kind: "default",
          address: "rabbitmq://localhost/billing.order-placed",
          source: {
            address: "rabbitmq://localhost:5672/q/billing.order-placed",
          },
        },
      ],
      dispatchEndpoints: [
        {
          name: "e/order-placed",
          kind: "default",
          address: "rabbitmq://localhost/e/order-placed",
          destination: {
            address: "rabbitmq://localhost:5672/e/order-placed",
          },
        },
      ],
      topology: {
        address: "rabbitmq://localhost:5672/",
        entities: [
          {
            kind: "exchange",
            name: "order-placed",
            address: "rabbitmq://localhost:5672/e/order-placed",
            flow: "inbound",
            properties: {
              type: "fanout",
              durable: true,
              autoDelete: false,
              autoProvision: true,
            },
          },
          {
            kind: "exchange",
            name: "billing.order-placed",
            address: "rabbitmq://localhost:5672/e/billing.order-placed",
            flow: "inbound",
            properties: {
              type: "fanout",
              durable: true,
              autoDelete: false,
              autoProvision: true,
            },
          },
          {
            kind: "queue",
            name: "billing.order-placed",
            address: "rabbitmq://localhost:5672/q/billing.order-placed",
            flow: "outbound",
            properties: {
              durable: true,
              exclusive: false,
              autoDelete: false,
              autoProvision: true,
            },
          },
        ],
        links: [
          {
            kind: "bind",
            address:
              "rabbitmq://localhost:5672/b/e/order-placed/e/billing.order-placed",
            source: "rabbitmq://localhost:5672/e/order-placed",
            target: "rabbitmq://localhost:5672/e/billing.order-placed",
            direction: "forward",
            properties: {
              routingKey: null,
              autoProvision: true,
            },
          },
          {
            kind: "bind",
            address:
              "rabbitmq://localhost:5672/b/e/billing.order-placed/q/billing.order-placed",
            source: "rabbitmq://localhost:5672/e/billing.order-placed",
            target: "rabbitmq://localhost:5672/q/billing.order-placed",
            direction: "forward",
            properties: {
              routingKey: null,
              autoProvision: true,
            },
          },
        ],
      },
    },
  ],
});

const trace = JSON.stringify({
  traceId: "intro-event-flow-trace-001",
  activities: [
    {
      id: "ie-1",
      parentId: null,
      startTime: "2024-06-15T10:30:00.000Z",
      durationMs: 45,
      status: "ok",
      operation: "publish",
      messageType: "OrderPlaced",
      messageTypeIdentity: "msg:OrderPlaced",
      transport: "RabbitMQ",
    },
    {
      id: "ie-2",
      parentId: "ie-1",
      startTime: "2024-06-15T10:30:00.001Z",
      durationMs: 2,
      status: "ok",
      operation: "dispatch",
      messageType: "OrderPlaced",
      messageTypeIdentity: "msg:OrderPlaced",
      endpointName: "e/order-placed",
      endpointAddress: "rabbitmq://localhost/e/order-placed",
      transport: "RabbitMQ",
    },
    {
      id: "ie-3",
      parentId: "ie-2",
      startTime: "2024-06-15T10:30:00.012Z",
      durationMs: 3,
      status: "ok",
      operation: "receive",
      messageType: "OrderPlaced",
      messageTypeIdentity: "msg:OrderPlaced",
      endpointName: "billing.order-placed",
      endpointAddress: "rabbitmq://localhost/billing.order-placed",
      transport: "RabbitMQ",
    },
    {
      id: "ie-4",
      parentId: "ie-3",
      startTime: "2024-06-15T10:30:00.015Z",
      durationMs: 12,
      status: "ok",
      operation: "consume",
      messageType: "OrderPlaced",
      messageTypeIdentity: "msg:OrderPlaced",
      consumerName: "OrderPlacedHandler",
    },
  ],
});

const meta = {
  title: "Components/MochaTopologyVisualization",
  component: MochaTopologyVisualization,
  args: { data, trace },
} satisfies Meta<typeof MochaTopologyVisualization>;

export default meta;
type Story = StoryObj<typeof meta>;

async function waitForCanvas(canvasElement: HTMLElement) {
  const canvas = within(canvasElement);
  await waitFor(() => expect(canvas.getByText("OrderService")).toBeVisible(), {
    timeout: 15000,
  });
  await waitFor(() => expect(canvas.getByText("publish")).toBeVisible());
  return canvas;
}

export const Dark: Story = {
  beforeEach: storyTheme("dark"),
  play: async ({ canvasElement }) => {
    await waitForCanvas(canvasElement);
  },
};

export const Light: Story = {
  beforeEach: storyTheme("light"),
  play: async ({ canvasElement }) => {
    const canvas = await waitForCanvas(canvasElement);
    expect(
      getComputedStyle(canvas.getByTitle("Click to expand visualization"))
        .backgroundColor,
    ).toBe("rgb(13, 17, 23)");
  },
};

export const LightExpanded: Story = {
  beforeEach: storyTheme("light"),
  play: async ({ canvasElement }) => {
    const canvas = await waitForCanvas(canvasElement);
    await userEvent.click(
      canvas.getByRole("button", { name: "Expand visualization" }),
    );

    const steps = await canvas.findByText("Publish OrderPlaced");
    expect(textContrast(steps)).toBeGreaterThanOrEqual(4.5);
    expect(
      textContrast(canvas.getByText("Trace Steps")),
    ).toBeGreaterThanOrEqual(4.5);
  },
};
