import { useDocSearchKeyboardEvents } from "@docsearch/react";
import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, {
  FC,
  MouseEventHandler,
  ReactNode,
  useCallback,
  useMemo,
  useState,
} from "react";
import { useSelector } from "react-redux";
import styled from "styled-components";

import {
  WorkshopNdcCopenhagenImage,
  WorkshopNdcOsloImage,
  WorkshopOnlineImage,
} from "@/components/images";
import { IconContainer, Link, SearchModal } from "@/components/misc";
import { Icon, Logo } from "@/components/sprites";
import { GitHubStarButton } from "@/components/widgets";
import {
  DocsJson,
  GetHeaderDataQuery,
  Maybe,
  SiteSiteMetadataTools,
} from "@/graphql-types";
import { State, WorkshopsState } from "@/state";
import {
  ApplyBackdropBlur,
  FONT_FAMILY_HEADING,
  MAX_CONTENT_WIDTH,
  THEME_COLORS,
} from "@/style";

// Icons
import AngleLeftIconSvg from "@/images/icons/angle-left.svg";
import AngleRightIconSvg from "@/images/icons/angle-right.svg";
import BarsIconSvg from "@/images/icons/bars.svg";
import BlogIconSvg from "@/images/icons/blog.svg";
import ChevronDownIconSvg from "@/images/icons/chevron-down.svg";
import CircleInfoIconSvg from "@/images/icons/circle-info.svg";
import GaugeCirclePlusIconSvg from "@/images/icons/gauge-circle-plus.svg";
import GithubIconSvg from "@/images/icons/github.svg";
import HandshakeAngleIconSvg from "@/images/icons/handshake-angle.svg";
import LinkedInIconSvg from "@/images/icons/linkedin.svg";
import LollipopIconSvg from "@/images/icons/lollipop.svg";
import PlanetRingedIconSvg from "@/images/icons/planet-ringed.svg";
import SearchIconSvg from "@/images/icons/search.svg";
import SlackIconSvg from "@/images/icons/slack.svg";
import SparklesIconSvg from "@/images/icons/sparkles.svg";
import WavePulseIconSvg from "@/images/icons/wave-pulse.svg";
import XIconSvg from "@/images/icons/x.svg";
import XmarkIconSvg from "@/images/icons/xmark.svg";
import YouTubeIconSvg from "@/images/icons/youtube.svg";
import LogoIconSvg from "@/images/logo/chillicream-winking.svg";

export const Header: FC = () => {
  const [topNavOpen, setTopNavOpen] = useState<boolean>(false);
  const [searchOpen, setSearchOpen] = useState<boolean>(false);
  const data = useStaticQuery<GetHeaderDataQuery>(graphql`
    query getHeaderData {
      site {
        siteMetadata {
          siteUrl
          tools {
            blog
            github
            linkedIn
            nitro
            shop
            slack
            youtube
            x
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
          latestStableVersion
        }
      }
      allMdx(
        limit: 1
        filter: { frontmatter: { path: { glob: "/blog/**/*" } } }
        sort: { fields: [frontmatter___date], order: DESC }
      ) {
        edges {
          node {
            id
            fields {
              readingTime {
                text
              }
            }
            frontmatter {
              featuredImage {
                childImageSharp {
                  gatsbyImageData(layout: CONSTRAINED, width: 400, quality: 100)
                }
              }
              path
              title
              date(formatString: "MMMM DD, YYYY")
            }
          }
        }
      }
    }
  `);
  const { siteUrl, tools } = data.site!.siteMetadata!;
  const products = data.docNav!.products!;
  const firstBlogPost = data.allMdx.edges[0].node;

  const handleTopNavClose = useCallback(() => {
    setTopNavOpen(false);
  }, [setTopNavOpen]);

  const handleTopNavOpen = useCallback(() => {
    setTopNavOpen(true);
  }, [setTopNavOpen]);

  const handleSearchClose = useCallback(() => {
    setSearchOpen(false);
  }, [setSearchOpen]);

  const handleSearchOpen = useCallback(() => {
    setTopNavOpen(false);
    setSearchOpen(true);
  }, [setSearchOpen]);

  useDocSearchKeyboardEvents({
    isOpen: searchOpen,
    onOpen: handleSearchOpen,
    onClose: handleSearchClose,
  });

  return (
    <Container>
      <ContainerWrapper>
        <LogoLink to="/">
          <LogoIcon {...LogoIconSvg} />
        </LogoLink>
        <Navigation open={topNavOpen}>
          <NavigationHeader>
            <LogoLink to="/" onClick={handleTopNavClose}>
              <LogoIcon {...LogoIconSvg} />
            </LogoLink>
            <MobileMenu>
              <SearchButton onClick={handleSearchOpen}>
                <IconContainer $size={20}>
                  <Icon {...SearchIconSvg} />
                </IconContainer>
              </SearchButton>
              <HamburgerCloseButton onClick={handleTopNavClose}>
                <HamburgerCloseIcon />
              </HamburgerCloseButton>
            </MobileMenu>
          </NavigationHeader>
          <Nav>
            <PlatformNavItem
              firstBlogPost={firstBlogPost}
              tools={tools!}
              onTopNavClose={handleTopNavClose}
              onSearchOpen={handleSearchOpen}
            />
            <ServicesNavItem
              tools={tools!}
              onTopNavClose={handleTopNavClose}
              onSearchOpen={handleSearchOpen}
            />
            <DeveloperNavItem
              products={products}
              tools={tools!}
              onTopNavClose={handleTopNavClose}
              onSearchOpen={handleSearchOpen}
            />
            <CompanyNavItem
              tools={tools!}
              onTopNavClose={handleTopNavClose}
              onSearchOpen={handleSearchOpen}
            />
            {/* <PricingNavItem /> */}
            <HelpNavItem />
            <NavItemContainer className="mobile-only double-height">
              <DemoAndLaunch tools={tools!} />
            </NavItemContainer>
          </Nav>
        </Navigation>
        <Tools>
          <GitHubStarButton />
          <DemoAndLaunch tools={tools!} />
          <SearchButton onClick={handleSearchOpen}>
            <IconContainer $size={20}>
              <Icon {...SearchIconSvg} />
            </IconContainer>
          </SearchButton>
        </Tools>
        <MobileMenu>
          <SearchButton onClick={handleSearchOpen}>
            <IconContainer $size={20}>
              <Icon {...SearchIconSvg} />
            </IconContainer>
          </SearchButton>
          <HamburgerOpenButton onClick={handleTopNavOpen}>
            <HamburgerOpenIcon />
          </HamburgerOpenButton>
        </MobileMenu>
      </ContainerWrapper>
      <SearchModal
        open={searchOpen}
        siteUrl={siteUrl!}
        onClose={handleSearchClose}
      />
    </Container>
  );
};

const Container = styled.header`
  position: sticky;
  top: 0;
  z-index: 30;
  display: flex;
  justify-content: center;
  box-sizing: border-box;
  border-bottom: 1px solid ${THEME_COLORS.boxBorder};
  width: 100vw;
  height: 72px;

  ${ApplyBackdropBlur(48, `background-color: ${THEME_COLORS.backgroundMenu};`)}
`;

const ContainerWrapper = styled.div`
  position: relative;
  display: flex;
  flex: 1 1 100svw;
  justify-content: space-between;
  height: 71px;

  @media only screen and (min-width: 992px) {
    justify-content: center;
    max-width: ${MAX_CONTENT_WIDTH}px;
  }
`;

const LogoLink = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  height: 100%;
  padding-right: 16px;
  padding-left: 16px;
`;

const LogoIcon = styled(Logo)`
  height: 32px;
  fill: ${THEME_COLORS.textContrast};
  transition: fill 0.2s ease-in-out;
`;

const HamburgerOpenButton = styled.div`
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  margin-left: auto;
  height: 100%;
  padding-right: 16px;
  padding-left: 16px;
  cursor: pointer;

  @media only screen and (min-width: 992px) {
    display: none;
  }
`;

const HamburgerOpenIcon = styled(Icon).attrs(BarsIconSvg)`
  width: 20px;
  fill: ${THEME_COLORS.textContrast};
`;

const Navigation = styled.nav<{
  readonly open: boolean;
}>`
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  z-index: 30;
  display: ${({ open }) => (open ? "flex" : "none")};
  flex: 1 1 auto;
  flex-direction: column;
  max-height: 100vh;
  opacity: ${({ open }) => (open ? "1" : "0")};
  transition: opacity 0.2s ease-in-out;

  @media only screen and (min-width: 992px) {
    position: initial;
    top: initial;
    right: initial;
    bottom: initial;
    left: initial;
    z-index: initial;
    display: flex;
    flex-direction: row;
    height: 100%;
    max-height: initial;
    //background-color: initial;
    opacity: initial;
    //box-shadow: initial;
  }
`;

const NavigationHeader = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  box-sizing: border-box;
  border-bottom: 1px solid ${THEME_COLORS.boxBorder};
  width: 100%;
  height: 72px;
  background-color: ${THEME_COLORS.background};

  @media only screen and (min-width: 992px) {
    display: none;
  }
`;

const HamburgerCloseButton = styled.div`
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  height: 100%;
  padding-right: 16px;
  padding-left: 16px;
  cursor: pointer;
`;

const BackButton = styled.div`
  display: flex;
  flex: 0 0 84px;
  flex-direction: row;
  align-items: center;
  gap: 12px;
  border-bottom: 1px solid #ccc9e41f;
  padding-right: 24px;
  padding-left: 24px;
  font-size: 0.875rem;
  font-weight: 600;
  color: ${THEME_COLORS.menuLink};
  cursor: pointer;
  transition: color 0.2s ease-in-out;

  ${IconContainer} {
    > svg {
      fill: ${THEME_COLORS.menuLink};
      transition: fill 0.2s ease-in-out;
    }
  }

  &:hover {
    color: ${THEME_COLORS.menuLinkHover};

    ${IconContainer} > svg {
      fill: ${THEME_COLORS.menuLinkHover};
    }
  }

  @media only screen and (min-width: 992px) {
    display: none;
  }
`;

const HamburgerCloseIcon = styled(Icon).attrs(XmarkIconSvg)`
  width: 20px;
  fill: ${THEME_COLORS.textContrast};
`;

const Nav = styled.ol`
  display: flex;
  flex: 1 1 calc(100% - 80px);
  flex-direction: column;
  align-items: flex-start;
  justify-content: flex-start;
  margin: 0;
  padding: 0;
  list-style-type: none;
  overflow-y: auto;
  background-color: ${THEME_COLORS.background};

  @media only screen and (min-width: 992px) {
    flex: 1 1 auto;
    flex-direction: row;
    justify-content: initial;
    margin: 0;
    height: 100%;
    overflow-y: initial;
    background-color: unset;
  }
`;

interface PlatformNavItemProps {
  readonly firstBlogPost: any;
  readonly tools: Pick<SiteSiteMetadataTools, "nitro">;
  readonly onTopNavClose: () => void;
  readonly onSearchOpen: () => void;
}

const PlatformNavItem: FC<PlatformNavItemProps> = ({
  firstBlogPost,
  tools,
  onTopNavClose,
  onSearchOpen,
}) => {
  const featuredImage =
    firstBlogPost.frontmatter!.featuredImage?.childImageSharp?.gatsbyImageData;

  const [subNav, navHandlers, linkHandlers] = useSubNav(
    (hideTopAndSubNav, hideSubNav) => (
      <>
        <SubNavMain>
          <BackButton onClick={hideSubNav}>
            <IconContainer $size={16}>
              <Icon {...AngleLeftIconSvg} />
            </IconContainer>
            Back
          </BackButton>
          <SubNavGroup>
            <SubNavTitle>Platform</SubNavTitle>
            <SubNavLinkWithDescription
              to="/platform/analytics"
              onClick={hideTopAndSubNav}
            >
              <IconContainer $size={24}>
                <Icon {...WavePulseIconSvg} />
              </IconContainer>
              <SubNavLinkTextGroup>
                <div className="title">Analytics</div>
                <div className="desc">
                  Instant Insights. Enhanced Performance.
                </div>
              </SubNavLinkTextGroup>
            </SubNavLinkWithDescription>
            <SubNavLinkWithDescription
              to="/platform/continuous-integration"
              onClick={hideTopAndSubNav}
            >
              <IconContainer $size={24}>
                <Icon {...SparklesIconSvg} />
              </IconContainer>
              <SubNavLinkTextGroup>
                <div className="title">Continuous Integration</div>
                <div className="desc">
                  Innovate with Confidence. Deliver with Quality.
                </div>
              </SubNavLinkTextGroup>
            </SubNavLinkWithDescription>
            {false && (
              <SubNavLinkWithDescription
                to="/platform/collaboration"
                onClick={hideTopAndSubNav}
              >
                <IconContainer $size={24}>
                  <Icon {...HandshakeAngleIconSvg} />
                </IconContainer>
                <SubNavLinkTextGroup>
                  <div className="title">Collaboration</div>
                  <div className="desc">Together We Are Better.</div>
                </SubNavLinkTextGroup>
              </SubNavLinkWithDescription>
            )}
            <SubNavLinkWithDescription
              to="/platform/ecosystem"
              onClick={hideTopAndSubNav}
            >
              <IconContainer $size={24}>
                <Icon {...PlanetRingedIconSvg} />
              </IconContainer>
              <SubNavLinkTextGroup>
                <div className="title">Ecosystem</div>
                <div className="desc">An Ecosystem You Trust and Love.</div>
              </SubNavLinkTextGroup>
            </SubNavLinkWithDescription>
          </SubNavGroup>
          <SubNavSeparator />
          <SubNavGroup>
            <SubNavTitle>Products</SubNavTitle>
            <SubNavLinkWithDescription
              to="/products/nitro"
              onClick={hideTopAndSubNav}
            >
              <IconContainer $size={24}>
                <Icon {...LollipopIconSvg} />
              </IconContainer>
              <SubNavLinkTextGroup>
                <div className="title">Nitro (<abbr title="Formerly Known As">fka</abbr> Banana Cake Pop)</div>
                <div className="desc">GraphQL IDE / API Cockpit</div>
              </SubNavLinkTextGroup>
            </SubNavLinkWithDescription>
          </SubNavGroup>
          <SubNavTools>
            <DemoAndLaunch tools={tools} />
          </SubNavTools>
        </SubNavMain>
        <SubNavAdditionalInfo>
          <SubNavTitle>Latest Blog Post</SubNavTitle>
          <TeaserLink to={firstBlogPost.frontmatter!.path!}>
            {featuredImage && (
              <TeaserImage>
                <GatsbyImage
                  image={featuredImage}
                  alt={firstBlogPost.frontmatter!.title}
                />
              </TeaserImage>
            )}
            <TeaserMetadata>
              {firstBlogPost.frontmatter?.date}
              {firstBlogPost?.readingTime?.text &&
                " ・ " + firstBlogPost.readingTime.text}
            </TeaserMetadata>
            <TeaserTitle>{firstBlogPost.frontmatter!.title}</TeaserTitle>
          </TeaserLink>
        </SubNavAdditionalInfo>
      </>
    ),
    onTopNavClose,
    onSearchOpen
  );

  return (
    <NavItemContainer {...navHandlers}>
      <NavLink to="/platform" prefetch={false} {...linkHandlers}>
        Platform
        <SubNavIndicatorIcon />
      </NavLink>
      {subNav}
    </NavItemContainer>
  );
};

interface ServicesNavItemProps {
  readonly tools: Pick<SiteSiteMetadataTools, "nitro">;
  readonly onTopNavClose: () => void;
  readonly onSearchOpen: () => void;
}

const ServicesNavItem: FC<ServicesNavItemProps> = ({
  tools,
  onTopNavClose,
  onSearchOpen,
}) => {
  const [subNav, navHandlers, linkHandlers] = useSubNav(
    (hideTopAndSubNav, hideSubNav) => (
      <>
        <SubNavMain>
          <BackButton onClick={hideSubNav}>
            <IconContainer $size={16}>
              <Icon {...AngleLeftIconSvg} />
            </IconContainer>
            Back
          </BackButton>
          <SubNavGroup>
            <SubNavTitle>Services</SubNavTitle>
            <SubNavLinkWithDescription
              to="/services/advisory"
              onClick={hideTopAndSubNav}
            >
              <IconContainer $size={24}>
                <Icon {...CircleInfoIconSvg} />
              </IconContainer>
              <SubNavLinkTextGroup>
                <div className="title">Advisory</div>
                <div className="desc">Consulting / Contracting</div>
              </SubNavLinkTextGroup>
            </SubNavLinkWithDescription>
            <SubNavLinkWithDescription
              to="/services/support"
              onClick={hideTopAndSubNav}
            >
              <IconContainer $size={24}>
                <Icon {...HandshakeAngleIconSvg} />
              </IconContainer>
              <SubNavLinkTextGroup>
                <div className="title">Support</div>
                <div className="desc">Get Help from Experts</div>
              </SubNavLinkTextGroup>
            </SubNavLinkWithDescription>
            <SubNavLinkWithDescription
              to="/services/training"
              onClick={hideTopAndSubNav}
            >
              <IconContainer $size={24}>
                <Icon {...GaugeCirclePlusIconSvg} />
              </IconContainer>
              <SubNavLinkTextGroup>
                <div className="title">Training</div>
                <div className="desc">Increase Your Team's Productivity</div>
              </SubNavLinkTextGroup>
            </SubNavLinkWithDescription>
          </SubNavGroup>
          <SubNavTools>
            <DemoAndLaunch tools={tools} />
          </SubNavTools>
        </SubNavMain>
        <SubNavAdditionalInfo>
          <SubNavTitle>Get in Touch</SubNavTitle>
          <TeaserLink to="mailto:contact@chillicream.com?subject=Services">
            <TeaserHero>
              Your technology journey.
              <br />
              Our expertise.
            </TeaserHero>
            <TeaserDescription>
              <strong>ChilliCream</strong> helps you unlock your full potential,
              delivering on its promise to transform your business.
            </TeaserDescription>
          </TeaserLink>
        </SubNavAdditionalInfo>
      </>
    ),
    onTopNavClose,
    onSearchOpen
  );

  return (
    <NavItemContainer {...navHandlers}>
      <NavLink to="/services" prefetch={false} {...linkHandlers}>
        Services
        <SubNavIndicatorIcon />
      </NavLink>
      {subNav}
    </NavItemContainer>
  );
};

interface DeveloperNavItemProps {
  readonly products: Maybe<
    Pick<DocsJson, "path" | "title" | "latestStableVersion">
  >[];
  readonly tools: Pick<
    SiteSiteMetadataTools,
    | "blog"
    | "github"
    | "linkedIn"
    | "nitro"
    | "shop"
    | "slack"
    | "x"
    | "youtube"
  >;
  readonly onTopNavClose: () => void;
  readonly onSearchOpen: () => void;
}

const DeveloperNavItem: FC<DeveloperNavItemProps> = ({
  products,
  tools,
  onTopNavClose,
  onSearchOpen,
}) => {
  const workshop = useSelector<State, WorkshopsState[number] | undefined>(
    (state) =>
      state.workshops.find(
        ({ hero, active, self }) => hero && active && self === false
      )
  );

  const [subNav, navHandlers, linkHandlers] = useSubNav(
    (hideTopAndSubNav, hideSubNav) => (
      <>
        <SubNavMain>
          <BackButton onClick={hideSubNav}>
            <IconContainer $size={16}>
              <Icon {...AngleLeftIconSvg} />
            </IconContainer>
            Back
          </BackButton>
          <SubNavGroup>
            <SubNavTitle>Documentation</SubNavTitle>
            {products.map((product, index) => (
              <SubNavLink
                key={index}
                to={`/docs/${product!.path!}${
                  product?.latestStableVersion
                    ? "/" + product?.latestStableVersion
                    : ""
                }`}
                onClick={hideTopAndSubNav}
              >
                <IconContainer $size={16}>
                  <Icon {...AngleRightIconSvg} />
                </IconContainer>
                {product!.title}
              </SubNavLink>
            ))}
          </SubNavGroup>
          <SubNavSeparator />
          <SubNavGroup>
            <SubNavTitle>Additional Resources</SubNavTitle>
            <SubNavLink to={tools.blog!} onClick={hideTopAndSubNav}>
              <IconContainer $size={20}>
                <Icon {...BlogIconSvg} />
              </IconContainer>
              Blog
            </SubNavLink>
            <SubNavLink to={tools.github!} onClick={hideTopAndSubNav}>
              <IconContainer $size={20}>
                <Icon {...GithubIconSvg} />
              </IconContainer>
              GitHub
            </SubNavLink>
            <SubNavLink to={tools.slack!} onClick={hideTopAndSubNav}>
              <IconContainer $size={20}>
                <Icon {...SlackIconSvg} />
              </IconContainer>
              Slack / Community
            </SubNavLink>
            <SubNavLink to={tools.youtube!} onClick={hideTopAndSubNav}>
              <IconContainer $size={20}>
                <Icon {...YouTubeIconSvg} />
              </IconContainer>
              YouTube Channel
            </SubNavLink>
            <SubNavLink to={tools.x!} onClick={hideTopAndSubNav}>
              <IconContainer $size={20}>
                <Icon {...XIconSvg} />
              </IconContainer>
              Formerly Twitter
            </SubNavLink>
            <SubNavLink to={tools.linkedIn!} onClick={hideTopAndSubNav}>
              <IconContainer $size={20}>
                <Icon {...LinkedInIconSvg} />
              </IconContainer>
              LinkedIn
            </SubNavLink>
          </SubNavGroup>
          <SubNavTools>
            <DemoAndLaunch tools={tools} />
          </SubNavTools>
        </SubNavMain>
        <SubNavAdditionalInfo>
          {workshop && (
            <>
              <SubNavTitle>Upcoming Workshop</SubNavTitle>
              <TeaserLink to={workshop.url}>
                <TeaserImage>
                  <WorkshopHero image={workshop.image} />
                </TeaserImage>
                <TeaserMetadata>
                  {`${workshop.date} ・ ${workshop.host} ・ `}
                  <NoWrap>{workshop.place}</NoWrap>
                </TeaserMetadata>
                <TeaserTitle>{workshop.title}</TeaserTitle>
              </TeaserLink>
            </>
          )}
        </SubNavAdditionalInfo>
      </>
    ),
    onTopNavClose,
    onSearchOpen
  );

  return (
    <NavItemContainer {...navHandlers}>
      <NavLink to="/docs" prefetch={false} {...linkHandlers}>
        Developers
        <SubNavIndicatorIcon />
      </NavLink>
      {subNav}
    </NavItemContainer>
  );
};

interface CompanyNavItemProps {
  readonly tools: Pick<
    SiteSiteMetadataTools,
    "github" | "linkedIn" | "nitro" | "shop" | "slack" | "x" | "youtube"
  >;
  readonly onTopNavClose: () => void;
  readonly onSearchOpen: () => void;
}

const CompanyNavItem: FC<CompanyNavItemProps> = ({
  tools,
  onTopNavClose,
  onSearchOpen,
}) => {
  const [subNav, navHandlers, linkHandlers] = useSubNav(
    (hideTopAndSubNav, hideSubNav) => (
      <>
        <SubNavMain>
          <BackButton onClick={hideSubNav}>
            <IconContainer $size={16}>
              <Icon {...AngleLeftIconSvg} />
            </IconContainer>
            Back
          </BackButton>
          <SubNavGroup>
            <SubNavTitle>Company</SubNavTitle>
            <SubNavLink
              prefetch={false}
              to="mailto:contact@chillicream.com"
              onClick={hideTopAndSubNav}
            >
              <IconContainer $size={16}>
                <Icon {...AngleRightIconSvg} />
              </IconContainer>
              Contact
            </SubNavLink>
            <SubNavLink
              prefetch={false}
              to={tools.shop!}
              onClick={hideTopAndSubNav}
            >
              <IconContainer $size={16}>
                <Icon {...AngleRightIconSvg} />
              </IconContainer>
              Shop
            </SubNavLink>
            <SubNavLink
              to="/legal/acceptable-use-policy"
              onClick={hideTopAndSubNav}
            >
              <IconContainer $size={16}>
                <Icon {...AngleRightIconSvg} />
              </IconContainer>
              Acceptable Use Policy
            </SubNavLink>
            <SubNavLink to="/legal/cookie-policy" onClick={hideTopAndSubNav}>
              <IconContainer $size={16}>
                <Icon {...AngleRightIconSvg} />
              </IconContainer>
              Cookie Policy
            </SubNavLink>
            <SubNavLink to="/legal/privacy-policy" onClick={hideTopAndSubNav}>
              <IconContainer $size={16}>
                <Icon {...AngleRightIconSvg} />
              </IconContainer>
              Privacy Policy
            </SubNavLink>
            <SubNavLink to="/legal/terms-of-service" onClick={hideTopAndSubNav}>
              <IconContainer $size={16}>
                <Icon {...AngleRightIconSvg} />
              </IconContainer>
              Terms of Service
            </SubNavLink>
            <SubNavLink
              to="/licensing/chillicream-license"
              onClick={hideTopAndSubNav}
            >
              <IconContainer $size={16}>
                <Icon {...AngleRightIconSvg} />
              </IconContainer>
              ChilliCream License
            </SubNavLink>
          </SubNavGroup>
          <SubNavTools>
            <DemoAndLaunch tools={tools} />
          </SubNavTools>
        </SubNavMain>
        <SubNavAdditionalInfo>
          <SubNavTitle>Get in Touch</SubNavTitle>
          <TeaserLink to="mailto:contact@chillicream.com?subject=Services">
            <TeaserHero>
              Your technology journey.
              <br />
              Our expertise.
            </TeaserHero>
            <TeaserDescription>
              <strong>ChilliCream</strong> helps you unlock your full potential,
              delivering on its promise to transform your business.
            </TeaserDescription>
          </TeaserLink>
        </SubNavAdditionalInfo>
      </>
    ),
    onTopNavClose,
    onSearchOpen
  );

  return (
    <NavItemContainer {...navHandlers}>
      <NavLink to="/resources" prefetch={false} {...linkHandlers}>
        Company
        <SubNavIndicatorIcon />
      </NavLink>
      {subNav}
    </NavItemContainer>
  );
};

// const PricingNavItem: FC = () => {
//   return (
//     <NavItemContainer>
//       <NavLink to={"/pricing"}>Pricing</NavLink>
//     </NavItemContainer>
//   );
// };

const HelpNavItem: FC = () => {
  return (
    <NavItemContainer>
      <NavLink to={"/help"}>Help</NavLink>
    </NavItemContainer>
  );
};

interface DemoAndLaunchProps {
  readonly tools: Pick<SiteSiteMetadataTools, "nitro">;
}

const DemoAndLaunch: FC<DemoAndLaunchProps> = ({ tools }) => {
  return (
    <>
      <RequestDemoLink
        to="mailto:contact@chillicream.com?subject=Demo"
        prefetch={false}
      >
        Request a Demo
      </RequestDemoLink>
      <LaunchLink to={tools!.nitro!}>Launch</LaunchLink>
    </>
  );
};

const SubNavIndicatorIcon: FC = () => {
  return (
    <>
      <IconContainer $size={14} className="desktop-only">
        <Icon {...ChevronDownIconSvg} />
      </IconContainer>
      <IconContainer $size={20} className="mobile-only">
        <Icon {...AngleRightIconSvg} />
      </IconContainer>
    </>
  );
};

function isTouchDevice() {
  return (
    window.PointerEvent &&
    "maxTouchPoints" in navigator &&
    navigator.maxTouchPoints > 0
  );
}

type NavHandlers = Record<"onMouseEnter" | "onMouseLeave", MouseEventHandler>;
type LinkHandlers = Record<"onClick", MouseEventHandler>;

function useSubNav(
  children: (hideTopAndSubNav: () => void, hideSubNav: () => void) => ReactNode,
  onTopNavClose: () => void,
  onOpenSearchOpen: () => void
): [subNav: ReactNode, navHandlers: NavHandlers, linkHandlers: LinkHandlers] {
  const [show, setShow] = useState<boolean>(false);

  const toggle = useCallback(() => {
    setShow((state) => !state);
  }, [setShow]);

  const hideSubMenu = useCallback(() => {
    setShow(false);
  }, [setShow]);

  const hideTopAndSubMenu = useCallback(() => {
    onTopNavClose();
    setShow(false);
  }, [onTopNavClose, setShow]);

  const openSearch = useCallback(() => {
    onOpenSearchOpen();
    setShow(false);
  }, [onOpenSearchOpen, setShow]);

  const subNav = show && (
    <SubNavContainer>
      <NavigationHeader>
        <LogoLink to="/" onClick={hideTopAndSubMenu}>
          <LogoIcon {...LogoIconSvg} />
        </LogoLink>
        <MobileMenu>
          <SearchButton onClick={openSearch}>
            <IconContainer $size={20}>
              <Icon {...SearchIconSvg} />
            </IconContainer>
          </SearchButton>
          <HamburgerCloseButton onClick={hideTopAndSubMenu}>
            <HamburgerCloseIcon />
          </HamburgerCloseButton>
        </MobileMenu>
      </NavigationHeader>
      <SubNav>{children(hideTopAndSubMenu, hideSubMenu)}</SubNav>
    </SubNavContainer>
  );

  const navHandlers = useMemo<NavHandlers>(
    () => ({
      onMouseEnter: () => {
        const viewport = window.visualViewport;

        if (!isTouchDevice() && (!viewport || viewport.width >= 992)) {
          setShow(true);
        }
      },
      onMouseLeave: () => {
        const viewport = window.visualViewport;

        if (!isTouchDevice() && (!viewport || viewport.width >= 992)) {
          hideTopAndSubMenu();
        }
      },
    }),
    []
  );

  const linkHandlers = useMemo<LinkHandlers>(
    () => ({
      onClick: (event) => {
        event.preventDefault();

        toggle();
      },
    }),
    []
  );

  return [subNav, navHandlers, linkHandlers];
}

const NavLink = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: space-between;
  box-sizing: border-box;
  border-bottom: 1px solid #ccc9e41f;
  padding-right: 16px;
  padding-left: 16px;
  width: 100%;
  height: 84px;
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 1.25rem;
  font-weight: 500;
  line-height: 1.6em;
  color: ${THEME_COLORS.menuLink};
  text-decoration: none;
  transition: color 0.2s ease-in-out;

  &.active {
    color: ${THEME_COLORS.menuLinkHover};

    ${IconContainer} > svg {
      fill: ${THEME_COLORS.menuLinkHover};
    }
  }

  &.active:hover,
  &:hover {
    color: ${THEME_COLORS.menuLinkHover};

    ${IconContainer} > svg {
      fill: ${THEME_COLORS.menuLinkHover};
    }
  }

  ${IconContainer} {
    margin-bottom: 2px;
    margin-left: 6px;

    > svg {
      fill: ${THEME_COLORS.menuLink};
      transition: fill 0.2s ease-in-out;
    }
  }

  @media only screen and (min-width: 992px) {
    justify-content: initial;
    border-bottom: unset;
    width: unset;
    height: 100%;
    font-size: 0.875rem;
  }
`;

const SubNavContainer = styled.div`
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  z-index: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  overflow: visible;
  background-color: ${THEME_COLORS.background};

  @media only screen and (min-width: 992px) {
    top: 56px;
    right: calc(50vw - 230px);
    bottom: initial;
    left: 64px;
    z-index: -1;
    background-color: unset;
  }

  @media only screen and (min-width: 1200px) {
    right: calc(50vw - 230px);
    bottom: initial;
    left: calc(50vw - 568px);
  }
`;

const NavItemContainer = styled.li`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  justify-content: space-evenly;
  margin: 0;
  width: 100%;
  height: 84px;

  &.desktop-only,
  .desktop-only {
    display: none;
  }

  &.mobile-only,
  .mobile-only {
    display: flex;
  }

  &.mobile-only.double-height {
    height: 168px;
  }

  @media only screen and (min-width: 992px) {
    width: unset;
    height: 100%;

    &.desktop-only,
    .desktop-only {
      display: flex;
    }

    &.mobile-only,
    .mobile-only {
      display: none;
    }
  }
`;

const SubNav = styled.div.attrs({
  className: "text-3",
})`
  position: relative;
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  min-width: 100%;
  overflow-y: auto;
  background-color: ${THEME_COLORS.background};

  @media only screen and (min-width: 992px) {
    flex-direction: row;
    border: 1px solid ${THEME_COLORS.boxBorder};
    border-radius: var(--box-border-radius);
    width: 700px;
    overflow-y: initial;

    ${ApplyBackdropBlur(
      48,
      `background-color: ${THEME_COLORS.backgroundMenu};`
    )}
  }
`;

const SubNavMain = styled.div`
  display: flex;
  flex: 1 1 100%;
  flex-direction: column;
  overflow-y: auto;

  @media only screen and (min-width: 992px) {
    flex-basis: 55%;
    overflow-y: initial;
  }
`;

const SubNavTools = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  justify-content: space-evenly;
  margin: 0;
  width: 100%;
  height: 168px;

  @media only screen and (min-width: 992px) {
    display: none;
  }
`;

const SubNavGroup = styled.div`
  display: grid;
  flex: 0 0 auto;
  margin-top: 28px;

  @media only screen and (min-width: 992px) {
    gap: 12px;
    margin: 32px;
  }
`;

const SubNavTitle = styled.h1`
  padding-right: 24px;
  padding-left: 24px;
  font-size: 0.75rem;
  font-weight: 400;
  line-height: 1.6em;
  letter-spacing: normal;
  text-transform: uppercase;
  color: #b1a6b1;

  @media only screen and (min-width: 992px) {
    padding-right: unset;
    padding-left: unset;
  }
`;

const SubNavSeparator = styled.div`
  display: none;
  margin-right: 32px;
  margin-left: 32px;
  height: 1px;
  background-color: ${THEME_COLORS.text};
  opacity: 0.12;

  @media only screen and (min-width: 992px) {
    display: block;
  }
`;

const SubNavLink = styled(Link)`
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 12px;
  border-bottom: 1px solid #ccc9e41f;
  padding-right: 24px;
  padding-left: 24px;
  height: 84px;
  font-size: 0.875rem;
  font-weight: 600;
  color: ${THEME_COLORS.menuLink};
  transition: color 0.2s ease-in-out;

  ${IconContainer} {
    > svg {
      fill: ${THEME_COLORS.menuLink};
      transition: fill 0.2s ease-in-out;
    }
  }

  &:hover {
    color: ${THEME_COLORS.menuLinkHover};

    ${IconContainer} > svg {
      fill: ${THEME_COLORS.menuLinkHover};
    }
  }

  @media only screen and (min-width: 992px) {
    border-bottom: unset;
    padding-right: unset;
    padding-left: unset;
    height: unset;
    font-size: unset;
  }
`;

const SubNavLinkWithDescription = styled(SubNavLink)`
  align-items: center;
  height: 92px;

  @media only screen and (min-width: 992px) {
    height: unset;
  }
`;

const SubNavLinkTextGroup = styled.div`
  .title {
    font-size: 0.875rem;
  }

  .desc {
    font-size: 0.75rem;
    font-weight: 300;
  }

  @media only screen and (min-width: 992px) {
    .title {
      font-size: unset;
    }

    .desc {
      font-size: unset;
    }
  }
`;

const TileLinkTitle = styled.h1`
  font-size: 1rem;
  font-weight: 400;
  line-height: 1.6em;
  letter-spacing: normal;
  transition: color 0.2s ease-in-out;
`;

const TileLinkDescription = styled.p`
  color: ${THEME_COLORS.primary};
  transition: color 0.2s ease-in-out;
`;

const TileLink = styled(Link)`
  display: flex;
  flex-direction: column;
  border-radius: var(--box-border-radius);
  width: auto;
  min-height: 72px;
  background-color: ${THEME_COLORS.background};
  transition: background-color 0.2s ease-in-out;

  &:hover {
    background-color: ${THEME_COLORS.primary};

    ${TileLinkTitle},
    ${TileLinkDescription} {
      color: ${THEME_COLORS.background};
    }
  }

  //@media only screen and ((min-width: 600px) and (min-height: 430px)) {
  //  margin: 5px 20px;
  //  padding: 10px;
  //}
`;

const SubNavAdditionalInfo = styled.div`
  display: none;
  flex: 1 1 45%;
  flex-direction: column;
  gap: 12px;
  padding-top: 32px;
  padding-right: 32px;
  padding-bottom: 32px;
  border-radius: 0 var(--border-radius) var(--border-radius) 0;

  @media only screen and (min-width: 992px) {
    display: flex;
  }
`;

const TeaserHero = styled.h2`
  display: flex;
  align-items: center;
  justify-content: center;
  text-align: center;
  font-size: 1rem;
  line-height: 1.6em;
  max-width: 100%;
  margin-bottom: 16px;
  aspect-ratio: 16/9;
  border-radius: var(--box-border-radius);
  color: ${THEME_COLORS.textContrast};
  background-color: ${THEME_COLORS.primary};
  background: linear-gradient(180deg, ${THEME_COLORS.primary} 0%, #3d5f9f 100%);

  //@media only screen and ((min-width: 600px) and (min-height: 430px)) {
  //  max-width: 400px;
  //}
`;

const TeaserLink = styled(Link)`
  .gatsby-image-wrapper {
    pointer-events: none;
  }

  &:hover {
    > * {
      color: ${THEME_COLORS.menuLinkHover};
    }

    .gatsby-image-wrapper {
    }

    ${TeaserHero} {
      color: ${THEME_COLORS.menuLink};
    }
  }

  //@media only screen and ((min-width: 600px) and (min-height: 430px)) {
  //  margin: 5px 30px;
  //}
`;

interface WorkshopHeroProps {
  readonly image: string;
}

const WorkshopHero: FC<WorkshopHeroProps> = ({ image }) => {
  switch (image) {
    case "ndc-oslo":
      return <WorkshopNdcOsloImage />;

    case "ndc-copenhagen":
      return <WorkshopNdcCopenhagenImage />;

    case "online":
      return <WorkshopOnlineImage />;

    default:
      return null;
  }
};

const TeaserImage = styled.div`
  overflow: visible;
  max-width: fit-content;
  margin-bottom: 16px;

  .gatsby-image-wrapper {
    border-radius: var(--box-border-radius);
  }

  //@media only screen and ((min-width: 600px) and (min-height: 430px)) {
  //  max-width: fit-content;
  //}
`;

const TeaserMetadata = styled.div`
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  align-items: center;
  margin-bottom: 8px;
  font-size: 0.75rem;
  color: ${THEME_COLORS.text};
  transition: color 0.2s ease-in-out;
`;

const NoWrap = styled.span`
  white-space: nowrap;
`;

const TeaserTitle = styled.h3`
  font-size: 1rem;
  line-height: 1.6em;
  color: ${THEME_COLORS.menuLink};
  transition: color 0.2s ease-in-out;
`;

const TeaserMessage = styled.div`
  margin-bottom: 16px;
  color: ${THEME_COLORS.menuLink};
  transition: color 0.2s ease-in-out;
`;

const TeaserDescription = styled.div`
  color: ${THEME_COLORS.menuLink};
  transition: color 0.2s ease-in-out;
`;

const MobileMenu = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  gap: 8px;
  height: 100%;

  @media only screen and (min-width: 992px) {
    display: none;
  }
`;

const Tools = styled.div`
  display: none;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  justify-content: flex-end;
  gap: 32px;
  height: 100%;
  padding-right: 16px;
  padding-left: 16px;

  > :nth-child(1),
  > :nth-child(2) {
    display: none;
  }

  @media only screen and (min-width: 992px) {
    display: flex;
  }

  @media only screen and (min-width: 1080px) {
    > :nth-child(2) {
      display: flex;
    }
  }

  @media only screen and (min-width: 1200px) {
    > :nth-child(1) {
      display: flex;
    }
  }
`;

const LaunchLink = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  box-sizing: border-box;
  border-radius: var(--button-border-radius);
  height: 38px;
  padding: 0 30px;
  border: 2px solid ${THEME_COLORS.primaryButtonBorder};
  color: ${THEME_COLORS.primaryButtonText};
  background-color: ${THEME_COLORS.primaryButton};
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 0.875rem;
  text-decoration: none;
  font-weight: 500;
  transition: background-color 0.2s ease-in-out, border-color 0.2s ease-in-out,
    color 0.2s ease-in-out;

  :hover {
    border-color: ${THEME_COLORS.primaryButtonBorder};
    color: ${THEME_COLORS.primaryButtonHoverText};
    background-color: ${THEME_COLORS.primaryButtonHover};
  }
`;

const RequestDemoLink = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  height: 38px;
  color: ${THEME_COLORS.primaryTextButton};
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 0.875rem;
  text-decoration: none;
  font-weight: 500;
  transition: color 0.2s ease-in-out;

  :hover {
    color: ${THEME_COLORS.primaryTextButtonHover};
  }
`;

const SearchButton = styled.button`
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  box-sizing: border-box;
  padding-right: 16px;
  padding-left: 16px;
  height: 100%;
  font-size: 0.875rem;
  line-height: 1.6em;
  color: ${THEME_COLORS.menuLink};
  text-decoration: none;
  transition: color 0.2s ease-in-out;

  > ${IconContainer} > svg {
    fill: ${THEME_COLORS.menuLink};
    transition: fill 0.2s ease-in-out;
  }

  :hover {
    > ${IconContainer} > svg {
      fill: ${THEME_COLORS.menuLinkHover};
    }
  }

  @media only screen and (min-width: 992px) {
    padding-right: 0;
    padding-left: 0;
  }
`;
