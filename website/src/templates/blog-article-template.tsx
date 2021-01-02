import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import { BlogArticleFragment } from "../../graphql-types";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";
import { BlogArticle } from "../components/blog-article/blog-article";

interface BlogArticleTemplateProperties {
  data: BlogArticleFragment;
}

const BlogArticleTemplate: FunctionComponent<BlogArticleTemplateProperties> = ({
  data,
}) => {
  return (
    <Layout>
      <SEO
        description={data.mdx!.excerpt || undefined}
        imageUrl={
          data.mdx!.frontmatter!.featuredImage?.childImageSharp!
            .fluid!.src
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
