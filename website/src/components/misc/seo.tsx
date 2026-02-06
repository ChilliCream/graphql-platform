import React, { FC } from "react";

export interface SEOProps {
  readonly description?: string;
  readonly imageUrl?: string;
  readonly isArticle?: boolean;
  readonly lang?: string;
  readonly title: string;
}

/**
 * No-op SEO component for compatibility.
 * In Next.js, metadata is handled via page-level `metadata` exports.
 */
export const SEO: FC<SEOProps> = () => {
  return null;
};
