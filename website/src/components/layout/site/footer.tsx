import React, { FC } from "react";
import styled from "styled-components";

import { IconContainer } from "@/components/misc/icon-container";
import { Link } from "@/components/misc/link";
import { SrOnly } from "@/components/misc/sr-only";
import { Icon, Logo } from "@/components/sprites";
import { GitHubStarButton } from "@/components/widgets";
import { siteMetadata } from "@/lib/site-config";
import docsConfig from "@/docs/docs.json";
import { MAX_CONTENT_WIDTH, THEME_COLORS } from "@/style";

// Icons
import BlogIconSvg from "@/images/icons/blog.svg";
import GithubIconSvg from "@/images/icons/github.svg";
import LinkedInIconSvg from "@/images/icons/linkedin.svg";
import SlackIconSvg from "@/images/icons/slack.svg";
import XIconSvg from "@/images/icons/x.svg";
import YouTubeIconSvg from "@/images/icons/youtube.svg";

// Logos
import LogoTextSvg from "@/images/logo/chillicream-text.svg";

const tools = siteMetadata.tools;
const products = docsConfig.map((p) => ({
  path: p.path,
  title: p.title,
  latestStableVersion: p.latestStableVersion,
}));

export const Footer: FC = () => {
  return (
    <Container>
      <Section>
        <Company>
          <LogoContainer>
            <LogoTextLink href="/">
              <LogoText {...LogoTextSvg} />
            </LogoTextLink>
          </LogoContainer>
          <About>
            <Address>
              16192 Coastal Highway
              <br />
              Lewes, DE 19958
              <br />
              United States
            </Address>
          </About>
          <GitHubStarButton />
        </Company>
        <LinkGrid>
          <Links>
            <Title>Platform</Title>
            <Navigation>
              <NavLink href="/platform/analytics">Analytics</NavLink>
              <NavLink href="/platform/continuous-integration">
                Continuous Integration
              </NavLink>
              {false && (
                <NavLink href="/platform/collaboration">Collaboration</NavLink>
              )}
              <NavLink href="/platform/ecosystem">Ecosystem</NavLink>
              <NavLink href="/products/nitro">Nitro</NavLink>
            </Navigation>
          </Links>
          <Links>
            <Title>Services</Title>
            <Navigation>
              <NavLink href="/services/advisory">Advisory</NavLink>
              <NavLink href="/services/support">Support</NavLink>
              <NavLink href="/services/training">Training</NavLink>
            </Navigation>
          </Links>
          <Links>
            <Title>Documentation</Title>
            <Navigation>
              {products.map((product, index: number) => (
                <NavLink
                  key={`doc-item-${index}`}
                  href={`/docs/${product.path}${
                    product.latestStableVersion
                      ? "/" + product.latestStableVersion
                      : ""
                  }`}
                >
                  {product.title}
                </NavLink>
              ))}
            </Navigation>
          </Links>
          <Links>
            <Title>Company</Title>
            <Navigation>
              <NavLink prefetch={false} href="mailto:contact@chillicream.com">
                Contact
              </NavLink>
              <NavLink href={tools.shop}>Shop</NavLink>
              <NavLink href="/legal/acceptable-use-policy">
                Acceptable Use Policy
              </NavLink>
              <NavLink href="/legal/cookie-policy">Cookie Policy</NavLink>
              <NavLink href="/legal/privacy-policy">Privacy Policy</NavLink>
              <NavLink href="/legal/terms-of-service">Terms of Service</NavLink>
              <NavLink href="/licensing/chillicream-license">
                ChilliCream License
              </NavLink>
            </Navigation>
          </Links>
        </LinkGrid>
      </Section>
      <Section>
        <Connect>
          <ConnectLink href={tools.blog}>
            <IconContainer>
              <BlogIcon {...BlogIconSvg} />
            </IconContainer>
            <SrOnly>to read the latest stuff</SrOnly>
          </ConnectLink>
          <ConnectLink href={tools.github}>
            <IconContainer>
              <GithubIcon {...GithubIconSvg} />
            </IconContainer>
            <SrOnly>to work with us on the platform</SrOnly>
          </ConnectLink>
          <ConnectLink href={tools.slack}>
            <IconContainer>
              <SlackIcon {...SlackIconSvg} />
            </IconContainer>
            <SrOnly>to get in touch with us</SrOnly>
          </ConnectLink>
          <ConnectLink href={tools.youtube}>
            <IconContainer>
              <YouTubeIcon {...YouTubeIconSvg} />
            </IconContainer>
            <SrOnly>to learn new stuff</SrOnly>
          </ConnectLink>
          <ConnectLink href={tools.x}>
            <IconContainer>
              <XIcon {...XIconSvg} />
            </IconContainer>
            <SrOnly>to stay up-to-date</SrOnly>
          </ConnectLink>
          <ConnectLink href={tools.linkedIn}>
            <IconContainer>
              <LinkedInIcon {...LinkedInIconSvg} />
            </IconContainer>
            <SrOnly>to connect</SrOnly>
          </ConnectLink>
        </Connect>
      </Section>
      <Section>
        © {new Date().getFullYear()} ChilliCream, Inc. ・ All Rights Reserved
      </Section>
    </Container>
  );
};

const Container = styled.footer.attrs({
  className: "text-3",
})`
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 48px;
  box-sizing: border-box;
  margin-top: 40px;
  border-top: 1px solid ${THEME_COLORS.boxBorder};
  padding-top: 54px;
  padding-right: 16px;
  padding-bottom: 54px;
  padding-left: 16px;
  width: 100%;
  font-size: 0.875rem !important;
  backdrop-filter: blur(4px);
  background-color: ${THEME_COLORS.backgroundMenu};

  @media only screen and (min-width: 992px) {
    gap: 32px;
    padding-top: 144px;
  }
`;

const Section = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  gap: 32px;
  width: 100%;
  max-width: ${MAX_CONTENT_WIDTH}px;

  @media only screen and (min-width: 992px) {
    flex-direction: row;
  }
`;

const Company = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  gap: 24px;

  @media only screen and (min-width: 768px) {
    flex-wrap: nowrap;
  }
`;

const LogoContainer = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  line-height: 0;
`;

const About = styled.div`
  display: flex;
  flex-direction: column;
  flex-wrap: wrap;
`;

const LogoTextLink = styled(Link)`
  text-decoration: none;

  > svg {
    fill: ${THEME_COLORS.heading};
    transition: fill 0.2s ease-in-out;
  }

  :hover > svg {
    fill: ${THEME_COLORS.footerLinkHover};
  }
`;

const LogoText = styled(Logo)`
  height: 30px;
  fill: ${THEME_COLORS.heading};
`;

const Address = styled.p`
  margin: 0;
`;

const Connect = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  gap: 0 10px;
`;

const ConnectLink = styled(Link)`
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

const BlogIcon = styled(Icon)`
  height: 22px;
`;

const GithubIcon = styled(Icon)`
  height: 26px;
`;

const SlackIcon = styled(Icon)`
  height: 22px;
`;

const YouTubeIcon = styled(Icon)`
  height: 22px;
`;

const XIcon = styled(Icon)`
  height: 22px;
`;

const LinkedInIcon = styled(Icon)`
  height: 22px;
`;

const LinkGrid = styled.div`
  display: grid;
  flex: 4 1 auto;
  grid-template-columns: repeat(2, 1fr);
  gap: 32px;

  @media only screen and (min-width: 768px) {
    grid-template-columns: repeat(4, 1fr);
  }
`;

const Links = styled.div`
  display: flex;
  flex: 2 1 auto;
  flex-direction: column;
  gap: 24px;
  min-width: 150px;
`;

const Navigation = styled.nav`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  gap: 10px;
`;

const NavLink = styled(Link)`
  flex: 0 0 auto;
  color: ${THEME_COLORS.footerLink};
  text-decoration: none;
  transition: color 0.2s ease-in-out;

  :hover {
    color: ${THEME_COLORS.footerLinkHover};
  }
`;

const Title = styled.h3`
  display: flex;
  align-items: flex-end;
  box-sizing: border-box;
  height: 30px;
  font-size: 1rem;
  font-weight: 600;
  color: ${THEME_COLORS.heading};
`;
