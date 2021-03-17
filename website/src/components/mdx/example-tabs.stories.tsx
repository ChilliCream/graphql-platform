import React from "react";
import { Story, Meta } from "@storybook/react";
import { ExampleTabs } from "./example-tabs";

export default {
  title: "Components/ExampleTabs",
  component: ExampleTabs,
} as Meta;

const Template: Story = () => (
  <ExampleTabs>
    <ExampleTabs.Annotation>
      <p>Annotation</p>
    </ExampleTabs.Annotation>
    <ExampleTabs.Code>
      <p>Code</p>
    </ExampleTabs.Code>
    <ExampleTabs.Schema>
      <p>Schema</p>
    </ExampleTabs.Schema>
  </ExampleTabs>
);

export const Basic = Template.bind({});
