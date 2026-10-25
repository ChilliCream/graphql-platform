import { JsonLd } from "@/src/components/JsonLd";
import {
  type BreadcrumbItem,
  createPageGraph,
  type JsonLdNode,
} from "@/src/helpers/structuredData";

interface PageStructuredDataProps {
  readonly title: string;
  readonly description?: string;
  readonly dateModified?: string;
  readonly path: string;
  readonly pageType?: string | readonly string[];
  readonly breadcrumbs?: readonly BreadcrumbItem[];
  readonly mainEntity?: JsonLdNode;
  readonly about?: JsonLdNode | readonly JsonLdNode[];
  readonly additionalNodes?: readonly JsonLdNode[];
}

/** Emits the page-local WebPage graph and its connected entities. */
export function PageStructuredData(props: PageStructuredDataProps) {
  return <JsonLd id="page-structured-data" data={createPageGraph(props)} />;
}
