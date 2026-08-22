// The LearnItem model backing /learn: a discriminated union over the content
// types the hub carries (template, video, tutorial, example, workshop),
// keyed on `type`. Every item shares a base shape (slug, title, tagline,
// products, updated info, optional external URL); each type then adds its
// own payload. Templates carry the full shape the old /templates pages
// render (topology, language, clients, stack, agentReady, cli, body).
// Video/tutorial/example/workshop payloads are intentionally minimal until
// real content is seeded (see website-5yo.6).

import type { LearnContentType, ProductKey } from "./facets";
import type { LanguageKey, TopologyKey } from "./facets";
import type { ClientKey } from "./facets";
import type { UseCaseKey } from "./facets";

/** Third-party technologies a template ships with, shown as brand logos on its card. */
export type StackKey =
  | "postgres"
  | "redis"
  | "react"
  | "nextjs"
  | "nodejs"
  | "blazor"
  | "opentelemetry"
  | "rabbitmq"
  | "mcp"
  | "dotnet";

/** Coarse difficulty signal for non-template content types. */
export type LearnLevel = "beginner" | "intermediate" | "advanced";

export interface CodeBlock {
  readonly language: string;
  readonly code: string;
}

export interface CliCommand {
  readonly key: string;
  readonly label: string;
  readonly code: string;
}

export interface TemplateSection {
  readonly heading: string;
  readonly paragraphs: readonly string[];
  readonly code?: CodeBlock;
}

interface LearnItemBase {
  readonly slug: string;
  readonly title: string;
  readonly tagline: string;
  readonly products: readonly ProductKey[];
  readonly updatedRelative: string;
  readonly externalUrl?: string;
}

export interface TemplateItem extends LearnItemBase {
  readonly type: "template";
  readonly useCases: readonly UseCaseKey[];
  readonly topology: TopologyKey;
  readonly language: LanguageKey;
  readonly clients: readonly ClientKey[];
  readonly stack: readonly StackKey[];
  readonly agentReady: boolean;
  readonly featured?: boolean;
  readonly githubUrl: string;
  readonly demoUrl?: string;
  readonly license: string;
  readonly cli: readonly CliCommand[];
  readonly body: readonly TemplateSection[];
}

export interface VideoItem extends LearnItemBase {
  readonly type: "video";
  readonly level?: LearnLevel;
  readonly url: string;
  readonly duration: string;
}

export interface TutorialItem extends LearnItemBase {
  readonly type: "tutorial";
  readonly level?: LearnLevel;
}

export interface ExampleItem extends LearnItemBase {
  readonly type: "example";
  readonly level?: LearnLevel;
}

export interface WorkshopItem extends LearnItemBase {
  readonly type: "workshop";
  readonly level?: LearnLevel;
}

export type LearnItem = TemplateItem | VideoItem | TutorialItem | ExampleItem | WorkshopItem;

export type LearnItemOfType<T extends LearnContentType> = Extract<LearnItem, { type: T }>;

export type TemplateSummary = Pick<
  TemplateItem,
  | "type"
  | "slug"
  | "title"
  | "tagline"
  | "topology"
  | "useCases"
  | "language"
  | "clients"
  | "products"
  | "stack"
  | "agentReady"
>;

/** Summary shapes shown on the /learn hub grid. Non-template payloads are already minimal, so their summary is the full item. */
export type LearnItemSummary = TemplateSummary | VideoItem | TutorialItem | ExampleItem | WorkshopItem;
