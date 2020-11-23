import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import { BlogArticleFragment } from "../../graphql-types";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";
import { BlogArticle } from "../components/widgets/blog-article";

interface BlogArticleTemplateProperties {
  data: BlogArticleFragment;
}

const BlogArticleTemplate: FunctionComponent<BlogArticleTemplateProperties> = ({
  data,
}) => {
  return (
    <Layout>
      <SEO
        title={data.markdownRemark!.frontmatter!.title!}
        meta={[
          {
            property: "og:type",
            content: "article",
          },
          ...(data.markdownRemark!.frontmatter!.featuredImage
            ? [
                {
                  property: "og:image",
                  content: data.markdownRemark!.frontmatter!.featuredImage
                    ?.childImageSharp!.fluid!.src,
                },
              ]
            : []),
        ]}
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
