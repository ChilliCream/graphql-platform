import { graphql } from "gatsby";
import React, { FC } from "react";

import { BlogArticle } from "@/components/articles";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc/seo";
import { BlogArticleFragment } from "@/graphql-types";

interface BlogArticleTemplateProps {
  readonly data: BlogArticleFragment;
}

const BlogArticleTemplate: FC<BlogArticleTemplateProps> = ({ data }) => {
  return (
    <SiteLayout disableStars>
      <SEO
        description={
          data.mdx!.frontmatter!.description || data.mdx!.excerpt || undefined
        }
        imageUrl={
          data.mdx!.frontmatter!.featuredImage?.childImageSharp!
            .gatsbyImageData!.images.fallback.src
        }
        isArticle
        title={data.mdx!.frontmatter!.title!}
      />
      <BlogArticle data={data} />
    </SiteLayout>
  );
};

export default BlogArticleTemplate;

export const pageQuery = graphql`
  query getBlogArticle($path: String!) {
    ...BlogArticle
  }
`;
