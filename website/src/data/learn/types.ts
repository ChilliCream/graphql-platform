// The LearnItem model backing /learn: a discriminated union over the content
// types the hub carries (template, video, tutorial, example, workshop),
// keyed on `type`. Every item shares a base shape (slug, title, tagline,
// products, updated info, optional external URL); each type then adds its
// own payload. Templates carry the full shape the old /templates pages
// render (topology, language, clients, stack, agentReady, cli, body).
// Tutorial/example/workshop payloads share the same detail-page fields
// (body/cli/githubUrl) as templates, kept optional since the concept can
// genuinely be absent (a docs-hosted tutorial has no githubUrl; a workshop
// walkthrough has no cli) and since the 8 seed items don't carry real body
// content yet (website-kbx.26 backfills it). Video payloads stay on their
// own shape (see LearnVideoDetail).

import type { LearnContentType, ProductKey } from "./facets";
import type { LanguageKey, TopologyKey } from "./facets";
import type { ClientKey } from "./facets";
import type { UseCaseKey } from "./facets";
import type { HubKey } from "./hubs";

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

/** One heading + paragraphs (+ optional code sample) section of a detail page's body. */
export interface DetailSection {
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
  /**
   * Explicit topic-hub override/addition (`src/data/learn/hubs.ts`), for
   * content whose real subject a `products` tag alone cannot express, e.g. an
   * OpenTelemetry video tagged only `hot-chocolate`. Most items omit this and
   * are inferred from `products` alone.
   */
  readonly hubs?: readonly HubKey[];
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
  readonly body: readonly DetailSection[];
}

export interface VideoItem extends LearnItemBase {
  readonly type: "video";
  readonly level?: LearnLevel;
  readonly url: string;
  readonly duration?: string;
  /**
   * Self-hosted optimized poster src, resolved server-side from the image
   * manifest. `undefined` when the manifest has no entry for this video's id.
   */
  readonly poster?: string;
  /** YouTube video id (the `v` query param), for embedding without re-parsing `url`. */
  readonly youtubeId?: string;
  /** Long-form description for a future video detail page: cleaned of repeated social-links boilerplate. */
  readonly description?: string;
  /** ISO 8601 publish timestamp, when known. */
  readonly publishedAt?: string;
}

export interface TutorialItem extends LearnItemBase {
  readonly type: "tutorial";
  readonly level?: LearnLevel;
  /** Tutorials are mostly docs-hosted walkthroughs, not GitHub repos; omitted unless one exists. */
  readonly githubUrl?: string;
  readonly cli?: readonly CliCommand[];
  readonly body?: readonly DetailSection[];
}

export interface ExampleItem extends LearnItemBase {
  readonly type: "example";
  readonly level?: LearnLevel;
  readonly githubUrl?: string;
  readonly cli?: readonly CliCommand[];
  readonly body?: readonly DetailSection[];
}

export interface WorkshopItem extends LearnItemBase {
  readonly type: "workshop";
  readonly level?: LearnLevel;
  readonly githubUrl?: string;
  /** Most workshops are facilitated walkthroughs with nothing to scaffold from a CLI. */
  readonly cli?: readonly CliCommand[];
  readonly body?: readonly DetailSection[];
}

export type LearnItem = TemplateItem | VideoItem | TutorialItem | ExampleItem | WorkshopItem;

export type LearnItemOfType<T extends LearnContentType> = Extract<LearnItem, { type: T }>;

/** The content types with a shared `LearnDetail` detail page (every catalog type except video, which has its own layout). */
export type DetailItem = TemplateItem | TutorialItem | ExampleItem | WorkshopItem;

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
