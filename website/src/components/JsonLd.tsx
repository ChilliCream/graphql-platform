import type { JsonLdDocument } from "@/src/helpers/structuredData";

interface JsonLdProps {
  readonly data: JsonLdDocument;
  readonly id?: string;
}

/**
 * Renders JSON-LD as inert structured data. Escaping `<` prevents content from
 * closing the script element if a future payload contains untrusted text.
 */
export function JsonLd({ data, id }: JsonLdProps) {
  return (
    <script
      id={id}
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: serializeJsonLd(data) }}
    />
  );
}

export function serializeJsonLd(data: JsonLdDocument): string {
  return JSON.stringify(data).replace(/</g, "\\u003c");
}
