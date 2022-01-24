import { Meta, Story } from "@storybook/react";
import React from "react";
import { Annotation, Code, ExampleTabs, Schema } from "./example-tabs";

export default {
  title: "Components/ExampleTabs",
  component: ExampleTabs,
} as Meta;

const Template: Story = () => (
  <ExampleTabs>
    <Annotation>
      <p>Annotation</p>
    </Annotation>
    <Code>
      <p>Code</p>
    </Code>
    <Schema>
      <p>Schema</p>
    </Schema>
  </ExampleTabs>
);

export const Basic = Template.bind({});
