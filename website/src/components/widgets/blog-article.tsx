import Img, { FluidObject } from "gatsby-image";
import { Disqus } from "gatsby-plugin-disqus";
import React, { FunctionComponent } from "react";
import { LinkedinShareButton, TwitterShareButton } from "react-share";
import styled from "styled-components";
import { Maybe } from "../../../graphql-types";
import { Link } from "../misc/link";

import LinkedinIconSvg from "../../images/linkedin-square.svg";
import TwitterIconSvg from "../../images/twitter-square.svg";

interface BlogArticleProperties {
  author: string;
  authorImageUrl: string;
  authorUrl: string;
  baseUrl: string;
  date: string;
  featuredImage?: FluidObject | FluidObject[];
  htmlContent: string;
  path: string;
  readingTime: string;
  tags?: Maybe<string>[] | null;
  title: string;
  twitterAuthor: string;
}

export const BlogArticle: FunctionComponent<BlogArticleProperties> = ({
  author,
  authorImageUrl,
  authorUrl,
  baseUrl,
  date,
  featuredImage,
  htmlContent,
  path,
  readingTime,
  tags,
  title,
  twitterAuthor,
}) => {
  const existingTags: string[] = tags
    ? (tags.filter(tag => tag && tag.length > 0) as string[])
    : [];
  const articelUrl = baseUrl + path;
  const disqusConfig = {
    url: articelUrl,
    identifier: path,
    title: title,
  };

  return (
    <Container>
      <ShareButtons>
        <TwitterShareButton
          url={articelUrl}
          title={title}
          via={twitterAuthor}
          hashtags={existingTags}
        >
          <TwitterIcon />
        </TwitterShareButton>
        <LinkedinShareButton url={articelUrl} title={title}>
          <LinkedinIcon />
        </LinkedinShareButton>
      </ShareButtons>
      <BlogContent>
        <Article>
          {featuredImage && <Img fluid={featuredImage} />}
          <Title>{title}</Title>
          <Metadata>
            <AuthorLink to={authorUrl}>
              <AuthorImage src={authorImageUrl} />
              {author}
            </AuthorLink>{" "}
            ・ {date} ・ {readingTime}
          </Metadata>
          {existingTags.length > 0 && (
            <Tags>
              {existingTags.map(tag => (
                <Tag>
                  <TagLink to={`/blog/tag/${tag}`}>{tag}</TagLink>
                </Tag>
              ))}
            </Tags>
          )}
          <Content dangerouslySetInnerHTML={{ __html: htmlContent }} />
        </Article>
        <DisqusWrapper config={disqusConfig} />
      </BlogContent>
    </Container>
  );
};

const Container = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  width: 100%;
  max-width: 800px;
`;

const ShareButtons = styled.aside`
  position: fixed;
  left: calc(50% - 480px);
  display: none;
  flex-direction: column;
  padding: 150px 0 250px;
  width: 60px;

  > button {
    flex: 0 0 50px;

    > svg {
      width: 30px;
    }
  }

  @media only screen and (min-width: 992px) {
    display: flex;
  }
`;

const TwitterIcon = styled(TwitterIconSvg)`
  fill: #1da0f2;
`;

const LinkedinIcon = styled(LinkedinIconSvg)`
  fill: #0073b0;
`;

const BlogContent = styled.div`
  display: flex;
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

const Title = styled.h1`
  margin-top: 20px;
  margin-right: 20px;
  margin-left: 20px;
  font-size: 2em;

  @media only screen and (min-width: 800px) {
    margin-right: 50px;
    margin-left: 50px;
  }
`;

const Metadata = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 0 20px 20px;
  font-size: 0.778em;

  @media only screen and (min-width: 800px) {
    margin: 0 50px 20px;
  }
`;

const AuthorLink = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  color: #666;
`;

const AuthorImage = styled.img`
  flex: 0 0 auto;
  margin-right: 0.5em;
  border-radius: 15px;
  width: 30px;
`;

const Tags = styled.ul`
  margin: 0 20px 20px;
  list-style-type: none;

  @media only screen and (min-width: 800px) {
    margin: 0 50px 20px;
  }
`;

const Tag = styled.li`
  display: inline-block;
  margin: 0 5px 0 0;
  border-radius: 4px;
  padding: 0;
  background-color: #f40010;
  font-size: 0.722em;
  letter-spacing: 0.05em;
  color: #fff;
`;

const TagLink = styled(Link)`
  display: block;
  padding: 5px 15px;
  color: #fff;
`;

const Content = styled.div`
  > * {
    padding-right: 20px;
    padding-left: 20px;
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
      padding-right: 20px;
      padding-left: 20px;
    }
  }

  @media only screen and (min-width: 800px) {
    > * {
      padding-right: 50px;
      padding-left: 50px;
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
