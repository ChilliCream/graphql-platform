import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import { GetBlogArticleQuery } from "../../graphql-types";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";
import { BlogArticle } from "../components/widgets/blog-article";

interface BlogArticleTemplateProperties {
  data: GetBlogArticleQuery;
}

const BlogArticleTemplate: FunctionComponent<BlogArticleTemplateProperties> = ({
  data: { markdownRemark, site },
}) => {
  const { fields, frontmatter, html } = markdownRemark!;

  return (
    <Layout>
      <SEO title="Home" />
      <BlogArticle
        author={frontmatter!.author!}
        authorImageUrl={frontmatter!.authorImageUrl!}
        authorUrl={frontmatter!.authorUrl!}
        baseUrl={site!.siteMetadata!.baseUrl!}
        date={frontmatter!.date!}
        htmlContent={html!}
        path={frontmatter!.path!}
        readingTime={fields!.readingTime!.text!}
        title={frontmatter!.title!}
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
        authorImageUrl
        authorUrl
        date(formatString: "MMMM DD, YYYY")
        path
        title
      }
      fields {
        readingTime {
          text
        }
      }
    }
    site {
      siteMetadata {
        baseUrl
      }
    }
  }
`;
