import { graphql, useStaticQuery } from "gatsby";
import React, { FC } from "react";
import styled from "styled-components";

import {
  ContentSectionElement,
  IconContainer,
  Link,
  LinkButton,
  SrOnly,
} from "@/components/misc";
import { Icon } from "@/components/sprites";
import { GetMediaLinksQuery } from "@/graphql-types";
import { MAX_CONTENT_WIDTH, THEME_COLORS } from "@/style";

// Icons
import BlogIconSvg from "@/images/icons/blog.svg";
import GithubIconSvg from "@/images/icons/github.svg";
import LinkedInIconSvg from "@/images/icons/linkedin.svg";
import SlackIconSvg from "@/images/icons/slack.svg";
import XIconSvg from "@/images/icons/x.svg";
import YouTubeIconSvg from "@/images/icons/youtube.svg";

export const NewsletterSection: FC = () => {
  const data = useStaticQuery<GetMediaLinksQuery>(graphql`
    query getMediaLinks {
      site {
        siteMetadata {
          tools {
            blog
            github
            linkedIn
            shop
            slack
            youtube
            x
          }
        }
      }
    }
  `);
  const tools = data.site!.siteMetadata!.tools;

  return (
    <ContentSection>
      <VisibleArea>
        <Newsletter>
          <NewsletterTitle>Join our Newsletter</NewsletterTitle>
          <NewsletterText>
            Get all the latest ChilliCream updates, news and events.
          </NewsletterText>
          <LinkButton to="https://cdn.forms-content.sg-form.com/36f84b0b-cf73-11ee-bbc0-aae526684092">
            Subscribe
          </LinkButton>
        </Newsletter>
        <Media>
          <MediaTitle>Social Media</MediaTitle>
          <MediaContainer>
            <MediaBox>
              <MediaConnectLink to={tools!.blog!}>
                <IconContainer>
                  <BlogIcon />
                </IconContainer>
                <SrOnly>to read the latest stuff</SrOnly>
              </MediaConnectLink>
            </MediaBox>
            <MediaBox>
              <MediaConnectLink to={tools!.github!}>
                <IconContainer>
                  <GithubIcon />
                </IconContainer>
                <SrOnly>to work with us on the platform</SrOnly>
              </MediaConnectLink>
            </MediaBox>
            <MediaBox>
              <MediaConnectLink to={tools!.slack!}>
                <IconContainer>
                  <SlackIcon />
                </IconContainer>
                <SrOnly>to get in touch with us</SrOnly>
              </MediaConnectLink>
            </MediaBox>
            <MediaBox>
              <MediaConnectLink to={tools!.youtube!}>
                <IconContainer>
                  <YouTubeIcon />
                </IconContainer>
                <SrOnly>to learn new stuff</SrOnly>
              </MediaConnectLink>
            </MediaBox>
            <MediaBox>
              <MediaConnectLink to={tools!.x!}>
                <IconContainer>
                  <XIcon />
                </IconContainer>
                <SrOnly>to stay up-to-date</SrOnly>
              </MediaConnectLink>
            </MediaBox>
            <MediaBox>
              <MediaConnectLink to={tools!.linkedIn!}>
                <IconContainer>
                  <LinkedInIcon />
                </IconContainer>
                <SrOnly>to connect</SrOnly>
              </MediaConnectLink>
            </MediaBox>
          </MediaContainer>
        </Media>
      </VisibleArea>
    </ContentSection>
  );
};

const ContentSection = styled(ContentSectionElement).attrs({
  className: "animate",
})`
  &.play .play-me {
    animation-play-state: running;
  }
`;

const VisibleArea = styled.div`
  position: relative;
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  justify-content: center;
  width: 100%;
  height: 100%;
  max-width: ${MAX_CONTENT_WIDTH}px;
  gap: 16px;
  overflow: visible;

  @media only screen and (min-width: 768px) {
    flex-direction: row;
  }

  @media only screen and (min-width: 992px) {
    gap: 24px;
  }
`;

const Newsletter = styled.div`
  position: relative;
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  align-items: flex-start;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--box-border-radius);
  padding: 40px;
  backdrop-filter: blur(2px);
  background-image: linear-gradient(
    to right bottom,
    #379dc83d,
    #2b80ad3d,
    #2263903d,
    #1a48743d,
    #112f573d
  );

  @media only screen and (min-width: 768px) {
    flex: 2 1 auto;
  }
`;

const NewsletterTitle = styled.h3`
  flex: 0 0 auto;
  margin-bottom: 24px;

  @media only screen and (min-width: 992px) {
    margin-bottom: 16px;
  }
`;

const NewsletterText = styled.p.attrs({
  className: "text-2",
})`
  flex: 0 0 auto;

  @media only screen and (min-width: 992px) {
    margin-bottom: 40px;
  }
`;

const Media = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  align-items: center;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--box-border-radius);
  padding: 40px;
  backdrop-filter: blur(2px);
  background-image: linear-gradient(
    to right bottom,
    #ab7bb03d,
    #9162953d,
    #784a7c3d,
    #6033633d,
    #481d4b3d
  );
`;

const MediaTitle = styled.h3`
  flex: 0 0 auto;
  margin-bottom: 32px;
`;

const MediaContainer = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: row;
  flex-wrap: wrap;
  gap: 32px 0;
  width: 200px;
`;

const MediaBox = styled.div`
  display: flex;
  flex: 1 1 33%;
  align-items: center;
  justify-content: center;
`;

const MediaConnectLink = styled(Link)`
  text-decoration: none;
  color: ${THEME_COLORS.footerLink};
  transition: color 0.2s ease-in-out;

  > ${IconContainer} {
    margin-right: 10px;
    vertical-align: middle;

    > svg {
      fill: ${THEME_COLORS.footerLink};
      transition: fill 0.2s ease-in-out;
    }
  }

  :hover {
    color: ${THEME_COLORS.footerLinkHover};

    > ${IconContainer} > svg {
      fill: ${THEME_COLORS.footerLinkHover};
    }
  }
`;

const BlogIcon = styled(Icon).attrs(BlogIconSvg)`
  height: 22px;
`;

const GithubIcon = styled(Icon).attrs(GithubIconSvg)`
  height: 26px;
`;

const SlackIcon = styled(Icon).attrs(SlackIconSvg)`
  height: 22px;
`;

const YouTubeIcon = styled(Icon).attrs(YouTubeIconSvg)`
  height: 22px;
`;

const XIcon = styled(Icon).attrs(XIconSvg)`
  height: 22px;
`;

const LinkedInIcon = styled(Icon).attrs(LinkedInIconSvg)`
  height: 22px;
`;
