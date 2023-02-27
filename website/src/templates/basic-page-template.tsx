import { graphql } from "gatsby";
import React, { FC } from "react";

import { BasicPage } from "@/components/basic-page/basic-page";
import { Layout } from "@/components/layout";
import { SEO } from "@/components/misc/seo";
import { BasicPageFragment } from "@/graphql-types";

export interface BasicPageTemplateProps {
  readonly data: BasicPageFragment;
}

const BasicPageTemplate: FC<BasicPageTemplateProps> = ({ data }) => {
  const title = data.file!.childMdx!.frontmatter!.title!;
  const description = data.file!.childMdx!.frontmatter!.description;

  return (
    <Layout>
      <SEO title={title} description={description || undefined} />
      <BasicPage data={data} />
    </Layout>
  );
};

export default BasicPageTemplate;

export const pageQuery = graphql`
  query getBasicPage($originPath: String!) {
    ...BasicPage
  }
`;
