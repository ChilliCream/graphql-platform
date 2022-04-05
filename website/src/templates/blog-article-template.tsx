import { graphql } from "gatsby";
import React, { FC } from "react";
import { BlogArticleFragment } from "../../graphql-types";
import { BlogArticle } from "../components/blog-article/blog-article";
import { Layout } from "../components/layout";
import { SEO } from "../components/misc/seo";

interface BlogArticleTemplateProps {
  data: BlogArticleFragment;
}

const BlogArticleTemplate: FC<BlogArticleTemplateProps> = ({ data }) => {
  return (
    <Layout>
      <SEO
        description={data.mdx!.excerpt || undefined}
        imageUrl={
          data.mdx!.frontmatter!.featuredImage?.childImageSharp!
            .gatsbyImageData!.images.fallback.src
        }
        isArticle
        title={data.mdx!.frontmatter!.title!}
      />
      <BlogArticle data={data} />
    </Layout>
  );
};

export default BlogArticleTemplate;

export const pageQuery = graphql`
  query getBlogArticle($path: String!) {
    ...BlogArticle
  }
`;
