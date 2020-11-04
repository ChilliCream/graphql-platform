import { graphql } from "gatsby";
import React, { FunctionComponent, useCallback } from "react";
import { useDispatch } from "react-redux";
import styled from "styled-components";
import { DocPageFragment } from "../../../graphql-types";
import { toggleAside, toggleTOC } from "../../state/common";
import { ArticleComments } from "../misc/article-comments";
import {
  Article,
  ArticleContent,
  ArticleHeader,
  ArticleTitle,
  ArticleWrapper,
} from "../misc/article-elements";
import { ArticleSections } from "../misc/article-sections";
import { DocPageAside } from "../misc/doc-page-aside";
import { DocPageCommunity } from "../misc/doc-page-community";
import { DocPageLegacy } from "../misc/doc-page-legacy";
import { DocPageNavigation } from "../misc/doc-page-navigation";

import ListAltIconSvg from "../../images/list-alt.svg";
import NewspaperIconSvg from "../../images/newspaper.svg";

interface DocPageProperties {
  data: DocPageFragment;
  originPath: string;
}

export const DocPage: FunctionComponent<DocPageProperties> = ({
  data,
  originPath,
}) => {
  const dispatch = useDispatch();
  const { fields, frontmatter, html } = data.file!.childMarkdownRemark!;
  const slug = fields!.slug!.substring(1);
  const path = `/docs/${slug}`;
  const productAndVersionPattern = /^([\w-]*?)\/(v\d+)?/g;
  const result = productAndVersionPattern.exec(slug);
  const selectedProduct = result![1]! || "";
  const selectedVersion = (result && result[2]) || "";
  const title = frontmatter!.title!;

  const handleToggleTOC = useCallback(() => {
    dispatch(toggleTOC());
  }, []);

  const handleToggleAside = useCallback(() => {
    dispatch(toggleAside());
  }, []);

  return (
    <Container>
      <DocPageNavigation
        data={data}
        selectedPath={path}
        selectedProduct={selectedProduct}
        selectedVersion={selectedVersion}
      />
      <ArticleWrapper>
        <Article>
          {false && <DocPageLegacy />}
          <ArticleHeader>
            <ResponsiveMenu>
              <Button onClick={handleToggleTOC} className="toc-toggle">
                <ListAltIconSvg /> Table of contents
              </Button>
              <Button onClick={handleToggleAside} className="aside-toggle">
                <NewspaperIconSvg /> About this article
              </Button>
            </ResponsiveMenu>
            <ArticleTitle>{title}</ArticleTitle>
          </ArticleHeader>
          <ArticleContent dangerouslySetInnerHTML={{ __html: html! }} />
        </Article>
        {false && <ArticleComments data={data} path={path} title={title} />}
      </ArticleWrapper>
      <DocPageAside>
        <DocPageCommunity data={data} originPath={originPath} />
        <ArticleSections data={data.file!.childMarkdownRemark!} />
      </DocPageAside>
    </Container>
  );
};

export const DocPageGraphQLFragment = graphql`
  fragment DocPage on Query {
    file(
      sourceInstanceName: { eq: "docs" }
      relativePath: { eq: $originPath }
    ) {
      childMarkdownRemark {
        fields {
          slug
        }
        frontmatter {
          title
        }
        html
        ...ArticleSections
      }
    }
    ...ArticleComments
    ...DocPageCommunity
    ...DocPageNavigation
  }
`;

const Container = styled.div`
  display: flex;
  flex-direction: row;
  width: 100%;
  max-width: 820px;

  @media only screen and (min-width: 1070px) {
    max-width: 1070px;
  }

  @media only screen and (min-width: 1320px) {
    max-width: 1320px;
  }
`;

const ResponsiveMenu = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  padding: 30px 20px 20px;

  @media only screen and (min-width: 820px) {
    padding-right: 50px;
    padding-left: 50px;
  }

  @media only screen and (min-width: 1070px) {
    > .toc-toggle {
      display: none;
    }
  }

  @media only screen and (min-width: 1320px) {
    display: none;
  }
`;

const Button = styled.button`
  display: flex;
  flex-direction: row;
  align-items: center;
  color: #666;
  transition: color 0.2s ease-in-out;

  &.aside-toggle {
    margin-left: auto;
  }

  &:hover {
    color: #000;

    > svg {
      fill: #000;
    }
  }

  > svg {
    margin-right: 5px;
    width: 16px;
    height: 16px;
    fill: #666;
    transition: fill 0.2s ease-in-out;
  }
`;
