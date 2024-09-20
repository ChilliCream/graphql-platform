import { graphql } from "gatsby";
import React, { FC } from "react";

import { DefaultArticle } from "@/components/articles";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc/seo";
import { DefaultArticleFragment } from "@/graphql-types";

export interface DefaultArticleTemplateProps {
  readonly data: DefaultArticleFragment;
}

const DefaultArticleTemplate: FC<DefaultArticleTemplateProps> = ({ data }) => {
  const title = data.file!.childMdx!.frontmatter!.title!;
  const description = data.file!.childMdx!.frontmatter!.description;

  return (
    <SiteLayout disableStars>
      <SEO title={title} description={description || undefined} />
      <DefaultArticle data={data} />
    </SiteLayout>
  );
};

export default DefaultArticleTemplate;

export const pageQuery = graphql`
  query getDefaultArticle($originPath: String!) {
    ...DefaultArticle
  }
`;
