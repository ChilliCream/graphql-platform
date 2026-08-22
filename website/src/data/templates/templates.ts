// Compatibility shim: the template data model now lives in src/data/learn
// (see website-5yo.2) as the `template` variant of the LearnItem union.
// This file re-exports the template-shaped subset under the old names so
// /templates and /templates/[slug] keep compiling unchanged until the
// routes are migrated to /learn (website-5yo.3, website-5yo.4).
//
// Do not add new content here — add it to src/data/learn/content.ts.

import type { TemplateSummary as LearnTemplateSummary } from "@/src/data/learn/types";

export type {
  StackKey,
  CodeBlock,
  CliCommand,
  TemplateSection,
  TemplateItem as Template,
} from "@/src/data/learn/types";

// The old shape predates the `type` discriminant the LearnItem union added;
// keep it structurally identical so existing call sites that build a
// TemplateSummary-shaped literal without `type` still type-check.
export type TemplateSummary = Omit<LearnTemplateSummary, "type">;

export {
  TEMPLATE_ITEMS as TEMPLATES,
  TEMPLATE_SUMMARIES,
  findTemplate,
  findFeaturedTemplate,
  findRelatedTemplates as findRelated,
} from "@/src/data/learn/content";
