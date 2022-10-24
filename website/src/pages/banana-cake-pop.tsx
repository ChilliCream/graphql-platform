import { OutboundLink } from "gatsby-plugin-google-analytics";
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
import { BananaCakePop } from "../components/images/banana-cake-pop";
import { Layout } from "../components/layout";
import { Link } from "../components/misc/link";
import { Intro } from "../components/misc/page-elements";
import { SEO } from "../components/misc/seo";
import {
  CompaniesSection,
  MostRecentBcpBlogPostsSection,
} from "../components/widgets";
import ArrowDownIconSvg from "../images/arrow-down.svg";
import CircleDownIconSvg from "../images/circle-down.svg";
import {
  FONT_FAMILY_HEADING,
  IsDesktop,
  IsPhablet,
  IsSmallDesktop,
  IsTablet,
  THEME_COLORS,
} from "../shared-style";

const DOWNLOAD_STABLE_BASE_URL =
  "https://download.chillicream.com/bananacakepop/";

const DOWNLOAD_INSIDER_BASE_URL =
  "https://download.chillicream.com/bananacakepopinsider/";

const WEB_STABLE_URL = "https://eat.bananacakepop.com";

const TITLE = "Banana Cake Pop / GraphQL IDE";

const BananaCakePopPage: FC = () => {
  const appInfos = useAppInfos();

  return (
    <Layout>
      <SEO title={TITLE} />
      <Intro>
        <Product>
          <ProductDetails>
            <ProductDetailsHeader>
              <ProductName>Banana Cake Pop</ProductName>
              <ProductDescription>\\ GraphQL IDE for Devs</ProductDescription>
            </ProductDetailsHeader>
            <ProductDownload appInfos={appInfos} />
            <ProductDetailsFooter></ProductDetailsFooter>
          </ProductDetails>
          <ProductImage>
            <BananaCakePop />
          </ProductImage>
        </Product>
      </Intro>
      <MostRecentBcpBlogPostsSection />
      <CompaniesSection />
    </Layout>
  );
};

export default BananaCakePopPage;

interface DownloadHeroProps {
  readonly appInfos?: AppInfos;
}

const ProductDownload: FC<DownloadHeroProps> = ({ appInfos }) => {
  if (!appInfos) {
    return null;
  }

  const { activeStable: active, stable, insider } = appInfos;

  switch (active?.os) {
    case "linux":
      return (
        <DownloadButton
          url={DOWNLOAD_STABLE_BASE_URL + active.appImage.filename}
          text={"Download " + active.appImage.text}
          filename={active.appImage.filename}
          stable={stable}
          insider={insider}
        />
      );

    case "mac":
      return (
        <DownloadButton
          url={DOWNLOAD_STABLE_BASE_URL + active.universal.filename}
          text={"Download " + active.universal.text}
          filename={active.universal.filename}
          stable={stable}
          insider={insider}
        />
      );

    case "windows":
      return (
        <DownloadButton
          url={DOWNLOAD_STABLE_BASE_URL + active.executable.filename}
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
        <ArrowDownIconSvg />
      </DropDown>
      <DownloadMatrix ref={menuRef}>
        <table>
          <thead>
            <tr>
              <th className="os" scope="col">
                <span className="sr-only">OS</span>
              </th>
              <th className="type" scope="col">
                <span className="sr-only">Type</span>
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
                <DownloadVariantLink
                  filename={stable.macOS.universal.filename}
                  baseUrl={DOWNLOAD_STABLE_BASE_URL}
                />
              </td>
              <td className="insider">
                <DownloadVariantLink
                  filename={stable.macOS.universal.filename}
                  baseUrl={DOWNLOAD_INSIDER_BASE_URL}
                />
              </td>
            </tr>
            <tr>
              <td className="os" scope="row">
                <span className="sr-only">macOS x64</span>
              </td>
              <td className="type">Silicon</td>
              <td className="stable">
                <DownloadVariantLink
                  filename={stable.macOS.silicon.filename}
                  baseUrl={DOWNLOAD_STABLE_BASE_URL}
                />
              </td>
              <td className="insider">
                <DownloadVariantLink
                  filename={stable.macOS.silicon.filename}
                  baseUrl={DOWNLOAD_INSIDER_BASE_URL}
                />
              </td>
            </tr>
            <tr>
              <td className="os" scope="row">
                <span className="sr-only">macOS x64</span>
              </td>
              <td className="type">Intel</td>
              <td className="stable">
                <DownloadVariantLink
                  filename={stable.macOS.intel.filename}
                  baseUrl={DOWNLOAD_STABLE_BASE_URL}
                />
              </td>
              <td className="insider">
                <DownloadVariantLink
                  filename={stable.macOS.intel.filename}
                  baseUrl={DOWNLOAD_INSIDER_BASE_URL}
                />
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
                <DownloadVariantLink
                  filename={stable.windows.executable.filename}
                  baseUrl={DOWNLOAD_STABLE_BASE_URL}
                />
              </td>
              <td className="insider">
                <DownloadVariantLink
                  filename={stable.windows.executable.filename}
                  baseUrl={DOWNLOAD_INSIDER_BASE_URL}
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
                <DownloadVariantLink
                  filename={stable.linux.appImage.filename}
                  baseUrl={DOWNLOAD_STABLE_BASE_URL}
                />
              </td>
              <td className="insider">
                <DownloadVariantLink
                  filename={stable.linux.appImage.filename}
                  baseUrl={DOWNLOAD_INSIDER_BASE_URL}
                />
              </td>
            </tr>
          </tbody>

          <tfoot>
            <tr>
              <td colSpan={4}>
                <Link to={WEB_STABLE_URL}>Open Web Version</Link>
              </td>
            </tr>
          </tfoot>
        </table>
      </DownloadMatrix>
    </DownloadButtonContainer>
  );
};

const DownloadVariantLink: FC<{
  readonly filename: string;
  readonly baseUrl: string;
}> = ({ filename, baseUrl }) => {
  return (
    <Link to={baseUrl + filename} download={filename} rel="noopener noreferrer">
      <DownloadSvg />
    </Link>
  );
};

const Product = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  overflow: visible;

  ${IsDesktop(css`
    flex-direction: row;
    justify-content: center;
    width: 1100px;
  `)}

  ${IsSmallDesktop(css`
    flex-direction: row;
    justify-content: center;
    width: 1100px;
  `)}

  ${IsTablet(css`
    flex-direction: row;
    justify-content: center;
    width: 100%;
  `)}

  ${IsPhablet(css`
    flex-direction: column;
    align-items: center;
    width: initial;
  `)}
`;

const ProductDetails = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  margin: 20px 30px 100px;
  overflow: visible;

  ${IsPhablet(css`
    flex-basis: auto;
    margin: 20px 40px;
  `)}
`;

const ProductDetailsHeader = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  margin-bottom: 20px;
`;

export const ProductName = styled.h1`
  flex: 0 0 auto;
  margin-bottom: 0;
  font-weight: normal;
  font-size: 2.222em;
  color: ${THEME_COLORS.textContrast};
`;

export const ProductDescription = styled.h2`
  flex: 0 0 auto;
  margin-bottom: 0;
  font-weight: normal;
  font-size: 1.625em;
  color: ${THEME_COLORS.quaternary};
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
  margin: 20px 30px;

  ${IsPhablet(css`
    flex-basis: auto;
    margin: 20px 40px;
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

const DownloadLink = styled(OutboundLink).attrs({
  rel: "noopener noreferrer",
})`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  border-radius: var(--border-radius) 0 0 var(--border-radius);
  height: 60px;
  padding: 0 15px;
  color: ${THEME_COLORS.primaryButtonText};
  background-color: ${THEME_COLORS.primaryButton};
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 1em;
  font-weight: 500;
  text-decoration: none;
  transition: background-color 0.2s ease-in-out, color 0.2s ease-in-out;

  & > span {
    margin-top: 0.25em;
    font-size: 0.833em;
    opacity: 0.75;
  }

  :hover {
    color: ${THEME_COLORS.primaryButtonHoverText};
    background-color: ${THEME_COLORS.primaryButtonHover};
  }
`;

const DropDown = styled.div`
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  margin-left: 2px;
  border-radius: 0 var(--border-radius) var(--border-radius) 0;
  height: 60px;
  padding: 0 15px;
  background-color: ${THEME_COLORS.primaryButton};
  cursor: pointer;
  transition: background-color 0.2s ease-in-out;

  svg {
    width: 16px;
    height: 16px;
    fill: ${THEME_COLORS.primaryButtonText};
    transition: fill 0.2s ease-in-out;
  }

  :hover {
    background-color: ${THEME_COLORS.primaryButtonHover};

    svg {
      fill: ${THEME_COLORS.primaryButtonHoverText};
    }
  }
`;

const DownloadSvg = styled(CircleDownIconSvg)`
  width: 16px;
  height: 16px;
  transition: fill 0.2s ease-in-out;
`;

const DownloadMatrix = styled.div`
  position: absolute;
  top: 62px;
  left: 0;
  z-index: 2;
  display: none;
  flex-direction: column;
  width: auto;
  min-width: 100%;
  overflow: visible;
  border-radius: var(--border-radius);
  background-color: ${THEME_COLORS.background};
  box-shadow: 0px 3px 6px 0px rgba(0, 0, 0, 0.25);
  user-select: none;

  &.show {
    display: flex;
  }

  table {
    margin: 0;
    border-collapse: collapse;
    width: 100%;
    color: ${THEME_COLORS.primary};

    > thead > tr > th,
    > tbody > tr > td,
    > tfoot > tr > td {
      border: 0 none;
      text-align: left;
      white-space: nowrap;
    }

    > tbody > tr:first-of-type > td {
      border-top: 1px solid ${THEME_COLORS.quaternary};
    }

    > tbody > tr {
      background-color: initial;
    }

    > tfoot > tr > td {
      border-top: 1px solid ${THEME_COLORS.quaternary};
      text-align: center;
    }

    td,
    th {
      padding: 5px 10px;
      font-feature-settings: "tnum";
      font-size: var(--font-size);
      line-height: 1.667em;
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
      }
    }

    td.insider {
      text-align: center;
      background-color: ${THEME_COLORS.quaternary};

      ${DownloadSvg} {
        fill: ${THEME_COLORS.primary};
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
  const baseUrl =
    variant === "latest" ? DOWNLOAD_STABLE_BASE_URL : DOWNLOAD_INSIDER_BASE_URL;
  const response = await fetch(baseUrl + filename);
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
