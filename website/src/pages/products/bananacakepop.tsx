import React, {
  FC,
  MouseEventHandler,
  useCallback,
  useEffect,
  useRef,
  useState,
} from "react";
import styled, { css } from "styled-components";
import { parse } from "yaml";

import { BananaCakePopImage } from "@/components/images";
import { SiteLayout } from "@/components/layout";
import {
  ContentSection,
  Hero,
  Link,
  SEO,
  Spinner,
  SrOnly,
} from "@/components/misc";
import { Card, CardOffer, CardsContainer } from "@/components/misc/cards";
import { Icon } from "@/components/sprites";
import {
  CompaniesSection,
  DeploymentOptionsSection,
  MostRecentBcpBlogPostsSection,
  NewsletterSection,
} from "@/components/widgets";
import { useAnimationIntersectionObserver } from "@/hooks";
import {
  FONT_FAMILY_HEADING,
  IsDesktop,
  IsMobile,
  IsSmallDesktop,
  IsSmallTablet,
  IsTablet,
  MAX_CONTENT_WIDTH,
  THEME_COLORS,
} from "@/style";

// Icons
import ChevronDownIconSvg from "@/images/icons/chevron-down.svg";
import CircleDownIconSvg from "@/images/icons/circle-down.svg";

const DOWNLOAD_BASE_URL = "https://cdn.bananacakepop.com/app/";

const WEB_STABLE_URL = "https://eat.bananacakepop.com";

const WEB_INSIDER_URL = "https://insider.bananacakepop.com";

const TITLE = "Banana Cake Pop / GraphQL IDE";

const BananaCakePopPage: FC = () => {
  const appInfos = useAppInfos();

  useAnimationIntersectionObserver();

  return (
    <SiteLayout>
      <SEO
        title={TITLE}
        description="Banana Cake Pop is an incredible, beautiful, and feature-rich GraphQL IDE for developers that works with any GraphQL APIs."
      />
      <Hero>
        <Product>
          <ProductDetails>
            <ProductDetailsHeader>
              <ProductNameFirstPart>Banana</ProductNameFirstPart>
              <ProductName>Cake Pop</ProductName>
              <ProductDescription>
                ~ Next-Level GraphQL IDE ~
              </ProductDescription>
            </ProductDetailsHeader>
            <ProductDownload appInfos={appInfos} />
            <ProductDetailsFooter></ProductDetailsFooter>
          </ProductDetails>
          <ProductImage>
            <BananaCakePopImage />
          </ProductImage>
        </Product>
      </Hero>
      <CompaniesSection />
      <ContentSection title="Features" noBackground>
        <CardsContainer>
          <Card>
            <CardOffer>
              <header>
                <h5>Authentication Flows</h5>
              </header>
              <p>
                Choose between various authentication flows like basic, bearer
                or OAuth 2.
              </p>
            </CardOffer>
          </Card>
          <Card>
            <CardOffer>
              <header>
                <h5>Organization Workspaces</h5>
              </header>
              <p>
                Organize your GraphQL APIs and collaborate with colleagues
                across your organization with ease.
              </p>
            </CardOffer>
          </Card>
          <Card>
            <CardOffer>
              <header>
                <h5>Document Synchronization</h5>
              </header>
              <p>
                Keep your documents safe across all your devices and your teams.
              </p>
            </CardOffer>
          </Card>
          <Card>
            <CardOffer>
              <header>
                <h5>
                  PWA <SrOnly>(Progressive Web Application)</SrOnly> Support
                </h5>
              </header>
              <p>
                Use your favorite Browser to install Banana Cake Pop as PWA on
                your Device without requiring administrative privileges.
              </p>
            </CardOffer>
          </Card>
          <Card>
            <CardOffer>
              <header>
                <h5>Beautiful Themes</h5>
              </header>
              <p>
                Choose your single preferred theme or let the system
                automatically switch between dark and light theme.
              </p>
            </CardOffer>
          </Card>
          <Card>
            <CardOffer>
              <header>
                <h5>GraphQL File Upload</h5>
              </header>
              <p>
                Implements the latest version of the{" "}
                <Link to="https://github.com/jaydenseric/graphql-multipart-request-spec">
                  GraphQL multipart request spec
                </Link>
                .
              </p>
            </CardOffer>
          </Card>
          <Card>
            <CardOffer>
              <header>
                <h5>
                  Subscriptions over SSE <SrOnly>(Server-Sent Events)</SrOnly>
                </h5>
              </header>
              <p>
                Supports{" "}
                <Link to="https://github.com/enisdenjo/graphql-sse">
                  GraphQL subscriptions over Server-Sent Events
                </Link>
                .
              </p>
            </CardOffer>
          </Card>
          <Card>
            <CardOffer>
              <header>
                <h5>Performant GraphQL IDE</h5>
              </header>
              <p>
                Lagging apps can be frustrating. We do not accept that and keep
                always an eye on performance so that you can get your task done
                fast.
              </p>
            </CardOffer>
          </Card>
          <Card>
            <CardOffer>
              <header>
                <h5>
                  Subscriptions over WS <SrOnly>(WebSockets)</SrOnly>
                </h5>
              </header>
              <p>
                Supports{" "}
                <Link to="https://github.com/enisdenjo/graphql-ws">
                  GraphQL subscriptions over WebSocket
                </Link>{" "}
                as well as the{" "}
                <Link to="https://github.com/apollographql/subscriptions-transport-ws/blob/master/PROTOCOL.md">
                  Apollo subscription protocol
                </Link>
                .
              </p>
            </CardOffer>
          </Card>
        </CardsContainer>
      </ContentSection>
      <DeploymentOptionsSection />
      <NewsletterSection />
      <MostRecentBcpBlogPostsSection />
    </SiteLayout>
  );
};

export default BananaCakePopPage;

interface DownloadHeroProps {
  readonly appInfos?: AppInfos;
}

const ProductDownload: FC<DownloadHeroProps> = ({ appInfos }) => {
  if (!appInfos) {
    return (
      <DownloadLinkPlaceholder>
        <Spinner colorSelector={({ primaryButtonText }) => primaryButtonText} />
      </DownloadLinkPlaceholder>
    );
  }

  const { activeStable: active, stable, insider } = appInfos;

  switch (active?.os) {
    case "linux":
      return (
        <DownloadButton
          url={DOWNLOAD_BASE_URL + active.appImage.filename}
          text={"Download " + active.appImage.text}
          filename={active.appImage.filename}
          stable={stable}
          insider={insider}
        />
      );

    case "mac":
      return (
        <DownloadButton
          url={DOWNLOAD_BASE_URL + active.universal.filename}
          text={"Download " + active.universal.text}
          filename={active.universal.filename}
          stable={stable}
          insider={insider}
        />
      );

    case "windows":
      return (
        <DownloadButton
          url={DOWNLOAD_BASE_URL + active.executable.filename}
          text={"Download " + active.executable.text}
          filename={active.executable.filename}
          stable={stable}
          insider={insider}
        />
      );

    default:
      return (
        <DownloadButton
          url={WEB_STABLE_URL}
          text="Open Web Version"
          stable={stable}
          insider={insider}
        />
      );
  }
};

interface DownloadButtonProps {
  readonly url: string;
  readonly text: string;
  readonly filename?: string;
  readonly stable: AppInfoVariant;
  readonly insider: AppInfoVariant;
}

const DownloadButton: FC<DownloadButtonProps> = ({
  url,
  text,
  filename,
  stable,
  insider,
}) => {
  const menuRef = useRef<HTMLDivElement>(null);

  const toggleMenu = useCallback<MouseEventHandler<HTMLDivElement>>((event) => {
    menuRef.current?.classList.toggle("show");
    event.stopPropagation();
  }, []);

  useEffect(() => {
    const closeHandler = () => {
      menuRef.current?.classList.remove("show");
    };

    window.addEventListener("click", closeHandler);

    return () => {
      window.removeEventListener("click", closeHandler);
    };
  }, []);

  return (
    <DownloadButtonContainer>
      <DownloadLink href={url} download={filename}>
        {text}
        <span>Stable Build</span>
      </DownloadLink>
      <DropDown onClick={toggleMenu}>
        <Icon {...ChevronDownIconSvg} />
      </DropDown>
      <DownloadMatrix ref={menuRef}>
        <table>
          <thead>
            <tr>
              <th className="os" scope="col">
                <SrOnly>OS</SrOnly>
              </th>
              <th className="type" scope="col">
                <SrOnly>Type</SrOnly>
              </th>
              <th className="stable" scope="col">
                Stable
              </th>
              <th className="insider" scope="col">
                Insider
              </th>
            </tr>
          </thead>

          <tbody>
            <tr>
              <td className="os" scope="row">
                macOS x64
              </td>
              <td className="type">Universal</td>
              <td className="stable">
                <DownloadAppLink filename={stable.macOS.universal.filename} />
              </td>
              <td className="insider">
                <DownloadAppLink filename={insider.macOS.universal.filename} />
              </td>
            </tr>
            <tr>
              <td className="os" scope="row">
                <SrOnly>macOS x64</SrOnly>
              </td>
              <td className="type">Silicon</td>
              <td className="stable">
                <DownloadAppLink filename={stable.macOS.silicon.filename} />
              </td>
              <td className="insider">
                <DownloadAppLink filename={insider.macOS.silicon.filename} />
              </td>
            </tr>
            <tr>
              <td className="os" scope="row">
                <SrOnly>macOS x64</SrOnly>
              </td>
              <td className="type">Intel</td>
              <td className="stable">
                <DownloadAppLink filename={stable.macOS.intel.filename} />
              </td>
              <td className="insider">
                <DownloadAppLink filename={insider.macOS.intel.filename} />
              </td>
            </tr>
          </tbody>

          <tbody>
            <tr>
              <td className="os" scope="row">
                Windows x64
              </td>
              <td className="type">User Installer</td>
              <td className="stable">
                <DownloadAppLink
                  filename={stable.windows.executable.filename}
                />
              </td>
              <td className="insider">
                <DownloadAppLink
                  filename={insider.windows.executable.filename}
                />
              </td>
            </tr>
          </tbody>

          <tbody>
            <tr>
              <td className="os" scope="row">
                Linux x64
              </td>
              <td className="type">AppImage</td>
              <td className="stable">
                <DownloadAppLink filename={stable.linux.appImage.filename} />
              </td>
              <td className="insider">
                <DownloadAppLink filename={insider.linux.appImage.filename} />
              </td>
            </tr>
          </tbody>

          <tfoot>
            <tr>
              <td colSpan={4}>
                <Link to={WEB_STABLE_URL}>Open Web Version</Link>
                {" | "}
                <Link to={WEB_INSIDER_URL}>Open Insider Version</Link>
              </td>
            </tr>
          </tfoot>
        </table>
      </DownloadMatrix>
    </DownloadButtonContainer>
  );
};

const DownloadAppLink: FC<{
  readonly filename: string;
}> = ({ filename }) => {
  return (
    <DownloadEditionLink to={DOWNLOAD_BASE_URL + filename} download={filename}>
      <DownloadSvg />
    </DownloadEditionLink>
  );
};

const DownloadEditionLink = styled(Link).attrs({
  rel: "noopener noreferrer",
})`
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
`;

const Product = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  gap: 128px;
  max-width: ${MAX_CONTENT_WIDTH}px;
  overflow: visible;

  ${IsDesktop(`
    flex-direction: row;
    justify-content: center;
    //width: 1100px;
  `)}

  ${IsSmallDesktop(`
    flex-direction: row;
    justify-content: center;
    //width: 1100px;
  `)}

  ${IsTablet(`
    flex-direction: row;
    justify-content: center;
    width: 100%;
  `)}

  ${IsSmallTablet(css`
    flex-direction: column;
    align-items: center;
    width: initial;
  `)}
`;

const ProductDetails = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: flex-start;
  justify-content: center;
  overflow: visible;

  ${IsSmallTablet(css`
    flex-basis: auto;
  `)}
`;

const ProductDetailsHeader = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
`;

export const ProductName = styled.h1`
  flex: 0 0 auto;
  margin-bottom: 32px;
  color: ${THEME_COLORS.textContrast};

  ${IsMobile(css`
    //font-size: 1.625em;
  `)}
`;

const ProductNameFirstPart = styled(ProductName).attrs({
  className: "big",
})`
  margin-bottom: -10px;
`;

export const ProductDescription = styled.p.attrs({
  className: "text-1",
})`
  flex: 0 0 auto;
  margin-left: 4px;
  color: ${THEME_COLORS.quaternary};
  margin-bottom: 48px;

  ${IsMobile(css`
    font-size: 1rem;
  `)}
`;

const ProductDetailsFooter = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
`;

const ProductImage = styled.div`
  display: flex;
  flex: 1 1 auto;
  align-items: center;
  justify-content: center;

  ${IsSmallTablet(css`
    margin: 0 10px;
  `)}
`;

const DownloadButtonContainer = styled.div`
  position: relative;
  z-index: 1;
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  justify-content: center;
  overflow: visible;
`;

const DownloadLink = styled.a.attrs({
  rel: "noopener noreferrer",
})`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  border: 2px solid ${THEME_COLORS.primaryButtonBorder};
  border-radius: var(--button-border-radius) 0 0 var(--button-border-radius);
  height: 64px;
  min-width: 150px;
  padding: 0 15px;
  color: ${THEME_COLORS.primaryButtonText};
  background-color: ${THEME_COLORS.primaryButton};
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 1.125rem;
  font-weight: 500;
  text-decoration: none;
  transition: background-color 0.2s ease-in-out, border-color 0.2s ease-in-out,
    color 0.2s ease-in-out;

  & > span {
    margin-top: -8px;
    font-size: 0.875rem;
    opacity: 0.75;
  }

  :hover {
    border-color: ${THEME_COLORS.primaryButtonBorderHover};
    color: ${THEME_COLORS.primaryButtonHoverText};
    background-color: ${THEME_COLORS.primaryButtonHover};
  }
`;

const DownloadLinkPlaceholder = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  border: 2px solid ${THEME_COLORS.primaryButtonBorder};
  border-radius: var(--button-border-radius);
  height: 64px;
  min-width: 180px;
  padding: 0 15px;
  color: ${THEME_COLORS.primaryButtonText};
  background-color: ${THEME_COLORS.primaryButton};
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 1.125rem;
  font-weight: 500;
  text-decoration: none;
`;

const DropDown = styled.div`
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  border: 2px solid ${THEME_COLORS.primaryButtonBorder};
  border-left-width: 0;
  border-radius: 0 var(--button-border-radius) var(--button-border-radius) 0;
  height: 64px;
  padding: 0 15px;
  background-color: ${THEME_COLORS.primaryButton};
  cursor: pointer;
  transition: background-color 0.2s ease-in-out, border-color 0.2s ease-in-out;

  svg {
    width: 16px;
    height: 16px;
    fill: ${THEME_COLORS.primaryButtonText};
    transition: fill 0.2s ease-in-out;
  }

  :hover {
    border-color: ${THEME_COLORS.primaryButtonBorderHover};
    background-color: ${THEME_COLORS.primaryButtonHover};

    svg {
      fill: ${THEME_COLORS.primaryButtonHoverText};
    }
  }
`;

const DownloadSvg = styled(Icon).attrs(CircleDownIconSvg)`
  width: 16px;
  height: 16px;
  padding: 2px;
  transition: fill 0.2s ease-in-out;
`;

const DownloadMatrix = styled.div.attrs({
  className: "text-3",
})`
  position: absolute;
  top: 68px;
  left: 2px;
  z-index: 2;
  display: none;
  flex-direction: column;
  width: auto;
  min-width: 100%;
  overflow: visible;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--button-border-radius);
  backdrop-filter: blur(2px);
  background-image: linear-gradient(
    to left top,
    #0a07214d,
    #1a28464d,
    #24496f4d,
    #286d994d,
    #2493c24d
  );
  user-select: none;

  ${IsSmallTablet(css`
    left: initial;
  `)}

  &.show {
    display: flex;
  }

  table {
    margin: 0;
    border-collapse: collapse;
    width: 100%;
    color: ${THEME_COLORS.text};

    > thead > tr > th,
    > tbody > tr > td,
    > tfoot > tr > td {
      border: 0 none;
      text-align: left;
      white-space: nowrap;
    }

    > tbody > tr:first-of-type > td {
      border-top: 1px solid ${THEME_COLORS.boxBorder};
    }

    > tbody > tr {
      background-color: initial;
    }

    > tfoot > tr > td {
      border-top: 1px solid ${THEME_COLORS.boxBorder};
      text-align: center;
    }

    td,
    th {
      padding: 5px 10px;
      font-feature-settings: "tnum";
      line-height: 1.6em;
    }

    th {
      font-weight: 600;
    }

    td.os {
      font-weight: 600;
    }

    td.stable {
      text-align: center;

      ${DownloadSvg} {
        fill: ${THEME_COLORS.primaryButton};
        transition: fill 0.2s ease-in-out;

        &:hover {
          fill: ${THEME_COLORS.primaryButtonHover};
        }
      }
    }

    td.insider {
      text-align: center;
      background-color: ${THEME_COLORS.primaryButton};

      ${DownloadSvg} {
        fill: ${THEME_COLORS.primaryButtonText};
        transition: fill 0.2s ease-in-out;

        &:hover {
          fill: ${THEME_COLORS.primaryButtonHoverText};
        }
      }
    }
  }
`;

type OS = "linux" | "mac" | "windows";

function getOS(): OS | null {
  if (navigator.userAgent.indexOf("Win") >= 0) {
    return "windows";
  }

  if (navigator.userAgent.indexOf("Mac") >= 0) {
    return "mac";
  }

  if (
    navigator.userAgent.indexOf("X11") >= 0 ||
    navigator.userAgent.indexOf("Linux") >= 0
  ) {
    return "linux";
  }

  return null;
}

interface ContentFile {
  readonly url: string;
  readonly sha512: string;
  readonly size: number;
}

interface LatestAppInfo {
  readonly version: string;
  readonly files: ContentFile[];
  readonly path: string;
  readonly sha512: string;
  readonly releaseDate: string;
}

interface AppInfoFile {
  readonly filename: string;
  readonly text: string;
}

interface LatestAppInfoBase {
  readonly os: OS;
  readonly version: string;
}

interface LatestLinuxAppInfo extends LatestAppInfoBase {
  readonly os: "linux";
  readonly appImage: AppInfoFile;
}

interface LatestMacOSAppInfo extends LatestAppInfoBase {
  readonly os: "mac";
  readonly intel: AppInfoFile;
  readonly silicon: AppInfoFile;
  readonly universal: AppInfoFile;
}

interface LatestWindowsAppInfo extends LatestAppInfoBase {
  readonly os: "windows";
  readonly executable: AppInfoFile;
}

type Variant = "latest" | "insider";

async function fetchAppInfo(variant: Variant, os: OS): Promise<LatestAppInfo> {
  const filename = getFilename(variant, os);
  const response = await fetch(
    DOWNLOAD_BASE_URL + filename + "?no-cache=" + new Date().getTime()
  );
  const text = await response.text();
  const content = parse(text) as LatestAppInfo;

  return content;
}

function getFilename(variant: Variant, os: OS): string {
  if (os === "windows") {
    return `${variant}.yml`;
  }

  return `${variant}-${os}.yml`;
}

async function fetchLinuxAppInfo(
  variant: Variant
): Promise<LatestLinuxAppInfo> {
  const { version, files } = await fetchAppInfo(variant, "linux");

  return {
    os: "linux",
    version,
    appImage: {
      filename: files[0].url,
      text: "Linux",
    },
  };
}

async function fetchMacOSAppInfo(
  variant: Variant
): Promise<LatestMacOSAppInfo> {
  const { version, files } = await fetchAppInfo(variant, "mac");

  return {
    os: "mac",
    version,
    intel: {
      filename: files[4].url,
      text: "Mac Intel",
    },
    silicon: {
      filename: files[3].url,
      text: "Mac Silicon",
    },
    universal: {
      filename: files[5].url,
      text: "Mac Universal",
    },
  };
}

async function fetchWindowsAppInfo(
  variant: Variant
): Promise<LatestWindowsAppInfo> {
  const { version, files } = await fetchAppInfo(variant, "windows");

  return {
    os: "windows",
    version,
    executable: {
      filename: files[0].url,
      text: "Windows",
    },
  };
}

interface AppInfos {
  readonly activeStable?:
    | LatestLinuxAppInfo
    | LatestMacOSAppInfo
    | LatestWindowsAppInfo;
  readonly stable: AppInfoVariant;
  readonly insider: AppInfoVariant;
}

interface AppInfoVariant {
  readonly linux: LatestLinuxAppInfo;
  readonly macOS: LatestMacOSAppInfo;
  readonly windows: LatestWindowsAppInfo;
}

function useAppInfos(): AppInfos | undefined {
  const [appInfos, setAppInfos] = useState<AppInfos | undefined>(undefined);

  useEffect(() => {
    Promise.all([
      fetchLinuxAppInfo("latest"),
      fetchLinuxAppInfo("insider"),
      fetchMacOSAppInfo("latest"),
      fetchMacOSAppInfo("insider"),
      fetchWindowsAppInfo("latest"),
      fetchWindowsAppInfo("insider"),
    ]).then(
      ([linux, linuxInsider, macOS, macOSInsider, windows, windowsInsider]) => {
        const os = getOS();
        const stable = {
          linux,
          macOS,
          windows,
        };
        const insider = {
          linux: linuxInsider,
          macOS: macOSInsider,
          windows: windowsInsider,
        };

        switch (os) {
          case "linux":
            setAppInfos({
              activeStable: linux,
              stable,
              insider,
            });
            break;

          case "mac":
            setAppInfos({
              activeStable: macOS,
              stable,
              insider,
            });
            break;

          case "windows":
            setAppInfos({
              activeStable: windows,
              stable,
              insider,
            });
            break;

          default:
            setAppInfos({
              stable,
              insider,
            });
            break;
        }
      }
    );
  }, [setAppInfos]);

  return appInfos;
}
