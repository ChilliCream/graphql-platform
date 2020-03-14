import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import { GetBlogArticleQuery } from "../../graphql-types";
import SEO from "../components/misc/seo";
import { Layout } from "../components/structure/layout";
import { BlogArticle } from "../components/widgets/blog-article";

interface BlogArticleTemplateProperties {
  data: GetBlogArticleQuery;
}

const BlogArticleTemplate: FunctionComponent<BlogArticleTemplateProperties> = ({
  data: { markdownRemark },
}) => {
  const { frontmatter, html } = markdownRemark!;

  return (
    <Layout>
      <SEO title="Home" />
      <BlogArticle
        title={frontmatter!.title!}
        date={frontmatter!.date!}
        author={frontmatter!.author!}
        htmlContent={html!}
      />
    </Layout>
  );
};

export default BlogArticleTemplate;

export const pageQuery = graphql`
  query getBlogArticle($path: String!) {
    markdownRemark(frontmatter: { path: { eq: $path } }) {
      html
      frontmatter {
        author
        date(formatString: "MMMM DD, YYYY")
        path
        title
      }
    }
  }
`;
