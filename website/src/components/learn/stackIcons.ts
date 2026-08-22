// Stack brand marks for /learn template cards. Mirrors
// src/components/templates/stackIcons.ts (kept as-is for the old /templates
// route until it is retired), retargeted at the StackKey exported by
// src/data/learn/types.

import type { ComponentType } from "react";
import type { StackKey } from "@/src/data/learn/types";
import { BlazorIcon } from "@/src/icons/Blazor";
import { DotNetIcon } from "@/src/icons/DotNet";
import { McpIcon } from "@/src/icons/Mcp";
import { NextJsIcon } from "@/src/icons/NextJs";
import { NodeJsIcon } from "@/src/icons/NodeJs";
import { OpenTelemetryIcon } from "@/src/icons/OpenTelemetry";
import { PostgresIcon } from "@/src/icons/Postgres";
import { RabbitMqIcon } from "@/src/icons/RabbitMq";
import { ReactIcon } from "@/src/icons/ReactLogo";
import { RedisIcon } from "@/src/icons/Redis";

interface StackEntry {
  readonly label: string;
  readonly Icon: ComponentType<{ readonly className?: string }>;
}

/** Brand mark and display label for every technology a template can list in its stack. */
export const STACK_ICONS: Record<StackKey, StackEntry> = {
  postgres: { label: "PostgreSQL", Icon: PostgresIcon },
  redis: { label: "Redis", Icon: RedisIcon },
  react: { label: "React", Icon: ReactIcon },
  nextjs: { label: "Next.js", Icon: NextJsIcon },
  nodejs: { label: "Node.js", Icon: NodeJsIcon },
  blazor: { label: "Blazor", Icon: BlazorIcon },
  opentelemetry: { label: "OpenTelemetry", Icon: OpenTelemetryIcon },
  rabbitmq: { label: "RabbitMQ", Icon: RabbitMqIcon },
  mcp: { label: "MCP", Icon: McpIcon },
  dotnet: { label: ".NET", Icon: DotNetIcon },
};
