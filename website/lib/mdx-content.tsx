"use client";

import React from "react";
import { MDXRemote, MDXRemoteSerializeResult } from "next-mdx-remote";

import { BlockQuote } from "@/components/mdx/block-quote";
import { CodeBlock } from "@/components/mdx/code-block";
import {
  Code,
  ExampleTabs,
  Implementation,
  Schema,
} from "@/components/mdx/example-tabs";
import { InlineCode } from "@/components/mdx/inline-code";
import { PackageInstallation } from "@/components/mdx/package-installation";
import { Video } from "@/components/mdx/video";
import { Warning } from "@/components/mdx/warning";
import { ApiChoiceTabs } from "@/components/mdx/api-choice-tabs";
import { InputChoiceTabs } from "@/components/mdx/input-choice-tabs";
import { List, Panel, Tab, Tabs } from "@/components/mdx/tabs";

const mdxComponents = {
  pre: CodeBlock,
  inlineCode: InlineCode,
  blockquote: BlockQuote,
  ExampleTabs,
  Code,
  Implementation,
  Schema,
  PackageInstallation,
  Video,
  Warning,
  ApiChoiceTabs,
  InputChoiceTabs,
  Tabs,
  Tab,
  List,
  Panel,
  // Hyphenated aliases for dotted component names (dots invalid in HTML element names)
  "InputChoiceTabs-CLI": InputChoiceTabs.CLI,
  "InputChoiceTabs-VisualStudio": InputChoiceTabs.VisualStudio,
  "ApiChoiceTabs-MinimalApis": ApiChoiceTabs.MinimalApis,
  "ApiChoiceTabs-Regular": ApiChoiceTabs.Regular,
  // Lowercase aliases: rehype-raw (used with format:"md") lowercases all HTML
  // tag names per the HTML spec, so <Video> becomes <video>, etc.
  video: Video,
  exampletabs: ExampleTabs,
  implementation: Implementation,
  schema: Schema,
  packageinstallation: PackageInstallation,
  warning: Warning,
  apichoicetabs: ApiChoiceTabs,
  inputchoicetabs: InputChoiceTabs,
  tabs: Tabs,
  tab: Tab,
  list: List,
  panel: Panel,
  "inputchoicetabs-cli": InputChoiceTabs.CLI,
  "inputchoicetabs-visualstudio": InputChoiceTabs.VisualStudio,
  "apichoicetabs-minimalapis": ApiChoiceTabs.MinimalApis,
  "apichoicetabs-regular": ApiChoiceTabs.Regular,
};

interface MdxContentProps {
  source: MDXRemoteSerializeResult;
}

export function MdxContent({ source }: MdxContentProps) {
  return <MDXRemote {...source} components={mdxComponents as any} />;
}
