import { graphql } from "gatsby";
import { Disqus } from "gatsby-plugin-disqus";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { DocPageFragment } from "../../../graphql-types";
import { ArticleTitle } from "../misc/blog-article-elements";
import { IconContainer } from "../misc/icon-container";
import { Link } from "../misc/link";

import GitHubIconSvg from "../../images/github.svg";
import SlackIconSvg from "../../images/slack.svg";

interface DocPageProperties {
  data: DocPageFragment;
}

export const DocPage: FunctionComponent<DocPageProperties> = ({
  data: { markdownRemark, site },
}) => {
  const { frontmatter, html } = markdownRemark!;
  const path = frontmatter!.path!;
  const articelUrl = site!.siteMetadata!.baseUrl! + path;
  const title = frontmatter!.title!;
  const disqusConfig = {
    url: articelUrl,
    identifier: path,
    title,
  };

  return (
    <Container>
      <Navigation>
        <FixedContainer>
          <NavigationList>
            <NavigationItem>Test</NavigationItem>
          </NavigationList>
        </FixedContainer>
      </Navigation>
      <Content>
        <Article>
          <ArticleTitle>{title}</ArticleTitle>
          <ArticleContent dangerouslySetInnerHTML={{ __html: html! }} />
        </Article>
        <DisqusWrapper config={disqusConfig} />
      </Content>
      <Aside>
        <FixedContainer>
          <AsideTitle>Help us improving our content</AsideTitle>
          <CommunityItems>
            <CommunityItem>
              <CommunityLink to="/test">
                <IconContainer>
                  <GitHubIconSvg />
                </IconContainer>
                Edit on GitHub
              </CommunityLink>
            </CommunityItem>
            <CommunityItem>
              <CommunityLink to="/test">
                <IconContainer>
                  <SlackIconSvg />
                </IconContainer>
                Discuss on Slack
              </CommunityLink>
            </CommunityItem>
          </CommunityItems>
        </FixedContainer>
      </Aside>
    </Container>
  );
};

export const DocPageGraphQLFragment = graphql`
  fragment DocPage on Query {
    markdownRemark(frontmatter: { path: { eq: $path } }) {
      frontmatter {
        path
        title
      }
      html
    }
    site {
      siteMetadata {
        author
        baseUrl
      }
    }
  }
`;

const Container = styled.div`
  display: flex;
  flex-direction: row;
  width: 100%;
  max-width: 1400px;
`;

const Navigation = styled.nav`
  display: flex;
  flex: 0 0 250px;
  flex-direction: column;

  @media only screen and (min-width: 992px) {
    display: flex;
  }
`;

const FixedContainer = styled.div`
  position: fixed;
  padding: 25px 0 250px;
`;

const NavigationList = styled.ul`
  margin: 0;
  padding: 0;
`;

const NavigationItem = styled.li`
  margin: 0;
  padding: 0;
  list-style-type: none;
`;

const Content = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
`;

const Article = styled.article`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  margin-bottom: 40px;
  padding-bottom: 20px;

  @media only screen and (min-width: 800px) {
    border: 1px solid #ccc;
    border-top: 0 none;
  }
`;

const ArticleContent = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;

  > * {
    padding-right: 20px;
    padding-left: 20px;
  }

  > h1 > a.anchor.before,
  > h2 > a.anchor.before,
  > h3 > a.anchor.before,
  > h4 > a.anchor.before,
  > h5 > a.anchor.before,
  > h6 > a.anchor.before {
    padding-right: 4px;
    transform: translateX(0px);
  }

  > table {
    margin-right: 20px;
    margin-left: 20px;
    padding-right: 0;
    padding-left: 0;
    width: calc(100% - 40px);
  }

  > .gatsby-highlight {
    padding-right: 0;
    padding-left: 0;

    > pre {
      padding: 30px 20px;
    }
  }

  @media only screen and (min-width: 800px) {
    > * {
      padding-right: 50px;
      padding-left: 50px;
    }

    > h1 > a.anchor.before,
    > h2 > a.anchor.before,
    > h3 > a.anchor.before,
    > h4 > a.anchor.before,
    > h5 > a.anchor.before,
    > h6 > a.anchor.before {
      transform: translateX(30px);
    }

    > table {
      margin-right: 50px;
      margin-left: 50px;
      padding-right: 0;
      padding-left: 0;
      width: calc(100% - 100px);
    }

    > .gatsby-highlight {
      > pre {
        padding-right: 50px;
        padding-left: 50px;
      }
    }
  }
`;

const DisqusWrapper = styled(Disqus)`
  margin: 0 20px;

  @media only screen and (min-width: 800px) {
    margin: 0 50px;
  }
`;

const Aside = styled.aside`
  display: flex;
  flex: 0 0 250px;
  flex-direction: column;

  @media only screen and (min-width: 992px) {
    display: flex;
  }
`;

const AsideTitle = styled.h6`
  padding: 0 20px 10px;
  font-size: 0.833em;
`;

const CommunityItems = styled.ul`
  margin: 0;
  padding: 0 20px 20px;
  list-style-type: none;
`;

const CommunityItem = styled.li`
  display: inline-block;
  margin: 5px 0;
  padding: 0;
`;

const CommunityLink = styled(Link)`
  font-size: 0.833em;
  color: #666;

  > ${IconContainer} {
    margin-right: 10px;

    > svg {
      fill: #666;
    }
  }

  :hover {
    color: #000;

    > ${IconContainer} > svg {
      fill: #000;
    }
  }
`;
