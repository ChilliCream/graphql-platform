import { graphql, useStaticQuery } from "gatsby";
import React, { FC } from "react";
import styled from "styled-components";

import { IconContainer } from "@/components/misc/icon-container";
import { Link } from "@/components/misc/link";
import { Brand, Logo } from "@/components/sprites";
import { GetFooterDataQuery } from "@/graphql-types";
import { FONT_FAMILY_HEADING, THEME_COLORS } from "@/shared-style";

// Brands
import GithubIconSvg from "@/images/brands/github.svg";
import LinkedInIconSvg from "@/images/brands/linkedin.svg";
import SlackIconSvg from "@/images/brands/slack.svg";
import TwitterIconSvg from "@/images/brands/twitter.svg";
import YouTubeIconSvg from "@/images/brands/youtube.svg";

// Logos
import LogoTextSvg from "@/images/logo/chillicream-text.svg";
import LogoIconSvg from "@/images/logo/chillicream.svg";

export const Footer: FC = () => {
  const data = useStaticQuery<GetFooterDataQuery>(graphql`
    query getFooterData {
      site {
        siteMetadata {
          tools {
            github
            linkedIn
            shop
            slack
            twitter
            youtube
          }
        }
      }
      docNav: file(
        sourceInstanceName: { eq: "docs" }
        relativePath: { eq: "docs.json" }
      ) {
        products: childrenDocsJson {
          path
          title
          versions {
            path
          }
        }
      }
    }
  `);
  const { tools } = data.site!.siteMetadata!;
  const { products } = data.docNav!;

  return (
    <Container>
      <Section>
        <About>
          <LogoContainer>
            <LogoIcon />
            <LogoText />
          </LogoContainer>
          <Description>
            We at ChilliCream build the ultimate GraphQL platform.
            <br />
            Most of our code is open-source and will forever remain open-source.
            <br />
            You can be a valued member of our team. Start helping us today.
          </Description>
          <Connect>
            <ConnectLink to={tools!.github!}>
              <IconContainer>
                <GithubIcon />
              </IconContainer>
              {" to work with us on the platform"}
            </ConnectLink>
            <ConnectLink to={tools!.slack!}>
              <IconContainer>
                <SlackIcon />
              </IconContainer>
              {" to get in touch with us"}
            </ConnectLink>
            <ConnectLink to={tools!.youtube!}>
              <IconContainer>
                <YouTubeIcon />
              </IconContainer>
              {" to learn new stuff"}
            </ConnectLink>
            <ConnectLink to={tools!.twitter!}>
              <IconContainer>
                <TwitterIcon />
              </IconContainer>
              {" to stay up-to-date"}
            </ConnectLink>
            <ConnectLink to={tools!.linkedIn!}>
              <IconContainer>
                <LinkedInIcon />
              </IconContainer>
              {" to connect"}
            </ConnectLink>
          </Connect>
        </About>
        <Links>
          <Title>Products</Title>
          <Navigation>
            <NavLink to="/products/bananacakepop">Banana Cake Pop</NavLink>
            <NavLink to="/docs/hotchocolate">Hot Chocolate</NavLink>
            <NavLink to="/docs/strawberryshake">Strawberry Shake</NavLink>
          </Navigation>
        </Links>
        <Links>
          <Title>Developers</Title>
          <Navigation>
            {products!.map((product, index) => (
              <NavLink
                key={`products-item-${index}`}
                to={`/docs/${product!.path!}/`}
              >
                {product!.title}
              </NavLink>
            ))}
            <NavLink to="/blog">Blog</NavLink>
          </Navigation>
        </Links>
        <Links>
          <Title>Services</Title>
          <Navigation>
            <NavLink to="/services/advisory">Advisory</NavLink>
            <NavLink to="/services/training">Training</NavLink>
            <NavLink to="/services/support">Support</NavLink>
          </Navigation>
        </Links>
      </Section>
      <Section>
        <Copyright>© {new Date().getFullYear()} ChilliCream</Copyright>
      </Section>
    </Container>
  );
};

const Container = styled.footer`
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 40px 0;
  width: 100%;
  background-color: #252d3c;
  color: ${THEME_COLORS.footerText};
`;

const Section = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  width: 100%;
  max-width: 1400px;
`;

const About = styled.div`
  display: flex;
  flex: 6 1 auto;
  flex-direction: column;
  padding: 0 20px;
`;

const LogoContainer = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  margin-bottom: 10px;
`;

const LogoIcon = styled(Logo).attrs(LogoIconSvg)`
  height: 40px;
  fill: ${THEME_COLORS.footerText};
`;

const LogoText = styled(Logo).attrs(LogoTextSvg)`
  padding-left: 15px;
  height: 24px;
  fill: ${THEME_COLORS.footerText};
`;

const Description = styled.p`
  font-size: 0.833em;
  line-height: 1.5em;
  margin-bottom: 10px;
`;

const Connect = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
`;

const ConnectLink = styled(Link)`
  flex: 0 0 auto;
  margin: 5px 0;
  font-size: 0.833em;
  text-decoration: none;
  color: ${THEME_COLORS.footerText};
  transition: color 0.2s ease-in-out;

  > ${IconContainer} {
    margin-right: 10px;
    vertical-align: middle;

    > svg {
      transition: fill 0.2s ease-in-out;
    }
  }

  :hover {
    color: ${THEME_COLORS.textContrast};

    > ${IconContainer} > svg {
      fill: ${THEME_COLORS.textContrast};
    }
  }
`;

const GithubIcon = styled(Brand).attrs(GithubIconSvg)`
  height: 26px;
  fill: ${THEME_COLORS.footerText};
`;

const SlackIcon = styled(Brand).attrs(SlackIconSvg)`
  height: 22px;
  fill: ${THEME_COLORS.footerText};
`;

const YouTubeIcon = styled(Brand).attrs(YouTubeIconSvg)`
  height: 22px;
  fill: ${THEME_COLORS.footerText};
`;

const TwitterIcon = styled(Brand).attrs(TwitterIconSvg)`
  height: 22px;
  fill: ${THEME_COLORS.footerText};
`;

const LinkedInIcon = styled(Brand).attrs(LinkedInIconSvg)`
  height: 22px;
  fill: ${THEME_COLORS.footerText};
`;

const Links = styled.div`
  display: none;
  flex: 2 1 auto;
  flex-direction: column;
  padding: 0 10px;
  min-width: 150px;

  @media only screen and (min-width: 768px) {
    display: flex;
  }
`;

const Navigation = styled.nav`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
`;

const NavLink = styled(Link)`
  flex: 0 0 auto;
  margin: 4px 0;
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 0.833em;
  line-height: 1.5em;
  color: ${THEME_COLORS.footerText};
  text-decoration: none;
  transition: color 0.2s ease-in-out;

  :hover {
    color: ${THEME_COLORS.textContrast};
  }
`;

const Title = styled.h3`
  margin: 15px 0 9px;
  font-size: 1em;
  font-weight: 600;
  color: ${THEME_COLORS.textContrast};
`;

const Copyright = styled.div`
  margin-top: 20px;
  padding: 0 20px;
`;
