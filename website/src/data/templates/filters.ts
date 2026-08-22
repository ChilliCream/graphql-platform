// Compatibility shim: the filter taxonomy now lives in src/data/learn/facets
// (see website-5yo.2), where content type is the primary /learn facet and
// topology/use case/language/client/agent-ready are scoped as template-only
// axes. This file re-exports the template-only axes under the old names so
// /templates keeps compiling unchanged until it migrates to /learn
// (website-5yo.3, website-5yo.4).
//
// Do not add new axes here — add them to src/data/learn/facets.ts.

export type {
  FilterKind,
  TopologyKey,
  UseCaseKey,
  LanguageKey,
  ClientKey,
  ProductKey,
  FilterOption,
  FilterAxisDef,
  TemplateFilterAxisKey as FilterAxisKey,
} from "@/src/data/learn/facets";

export {
  TOPOLOGY_OPTIONS,
  USE_CASE_OPTIONS,
  LANGUAGE_OPTIONS,
  CLIENT_OPTIONS,
  PRODUCT_OPTIONS,
  TEMPLATE_FILTER_AXES as FILTER_AXES,
  productLabel,
  topologyLabel,
  useCaseLabel,
  languageLabel,
  clientLabel,
} from "@/src/data/learn/facets";
