import { graphql } from "gatsby";
import React, {
  FC,
  MouseEvent,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";
import { useDispatch, useSelector } from "react-redux";
import styled, { css } from "styled-components";

import { IconContainer } from "@/components/misc/icon-container";
import { Link } from "@/components/misc/link";
import { DocPageNavigationFragment } from "@/graphql-types";
import { BoxShadow, IsTablet, THEME_COLORS } from "@/shared-style";
import { State } from "@/state";
import { closeTOC } from "@/state/common";

// Icons
import ArrowDownIconSvg from "@/images/arrow-down.svg";
import ArrowUpIconSvg from "@/images/arrow-up.svg";
import ProductSwitcherIconSvg from "@/images/th-large.svg";

import {
  DocPageStickySideBarStyle,
  MostProminentSection,
} from "./doc-page-elements";
import { DocPagePaneHeader } from "./doc-page-pane-header";

interface NavigationContainerProps {
  readonly basePath: string;
  readonly selectedPath: string;
  readonly items: Item[];
}

const NavigationContainer: FC<NavigationContainerProps> = ({
  items: rawItems,
  basePath,
  selectedPath,
}) => {
  const [expandedPaths, setExpandedPaths] = useState<string[]>([]);
  const dispatch = useDispatch();

  const items = useMemo(
    () =>
      rawItems.map<EnhancedItem>((i) => {
        let fullpath = basePath;

        if (i.path && i.path !== "index") {
          fullpath += "/" + i.path;
        }

        return {
          ...i,
          fullpath,
        };
      }),
    [rawItems]
  );

  useEffect(() => {
    for (const item of items) {
      if (containsActiveItem(selectedPath, item.fullpath)) {
        setExpandedPaths((old) => [...old, item.fullpath]);
      }
    }
  }, [selectedPath, basePath, items]);

  const toggleItem = (itemPath: string) => () => {
    if (expandedPaths.includes(itemPath)) {
      setExpandedPaths((old) => old.filter((e) => e !== itemPath));
    } else {
      setExpandedPaths((old) => [...old, itemPath]);
    }
  };

  return (
    <NavigationList>
      {items.map(({ fullpath, title, items: subItems }) => {
        const isActive = subItems ? containsActiveItem : isActiveItem;

        return (
          <NavigationItem
            key={fullpath}
            active={isActive(selectedPath, fullpath)}
            onClick={() => !subItems && dispatch(closeTOC())}
          >
            {subItems ? (
              <NavigationGroup expanded={expandedPaths.includes(fullpath)}>
                <NavigationGroupToggle onClick={toggleItem(fullpath)}>
                  {title}

                  <IconContainer size={16}>
                    <ArrowDownIconSvg className="arrow-down" />
                    <ArrowUpIconSvg className="arrow-up" />
                  </IconContainer>
                </NavigationGroupToggle>

                <NavigationGroupContent>
                  <NavigationContainer
                    items={subItems}
                    basePath={fullpath}
                    selectedPath={selectedPath}
                  />
                </NavigationGroupContent>
              </NavigationGroup>
            ) : (
              <NavigationLink to={fullpath}>{title}</NavigationLink>
            )}
          </NavigationItem>
        );
      })}
    </NavigationList>
  );
};

export interface DocPageNavigationProps {
  readonly data: DocPageNavigationFragment;
  readonly selectedPath: string;
  readonly selectedProduct: string;
  readonly selectedVersion: string;
}

export const DocPageNavigation: FC<DocPageNavigationProps> = ({
  data,
  selectedPath,
  selectedProduct,
  selectedVersion,
}) => {
  const height = useSelector<State, string>((state) => {
    return state.common.articleViewportHeight;
  });
  const showTOC = useSelector<State, boolean>((state) => state.common.showTOC);
  const dispatch = useDispatch();

  const [productSwitcherOpen, setProductSwitcherOpen] = useState(false);
  const [versionSwitcherOpen, setVersionSwitcherOpen] = useState(false);

  const products = data.config?.products || [];
  const activeProduct = products.find((p) => p?.path === selectedProduct);
  const activeVersion = activeProduct?.versions?.find(
    (v) => v?.path === selectedVersion
  );

  // catch if someone clicks outside of the product switcher
  // to close the modal
  const handleClick = useCallback(() => {
    setProductSwitcherOpen(false);
    setVersionSwitcherOpen(false);
  }, []);

  useEffect(() => {
    window.addEventListener("click", handleClick);

    return () => {
      window.removeEventListener("click", handleClick);
    };
  }, [handleClick]);

  const toggleProductSwitcher = (e: MouseEvent) => {
    e.stopPropagation();

    setVersionSwitcherOpen(false);
    setProductSwitcherOpen((old) => !old);
  };

  const toggleVersionSwitcher = (e: MouseEvent) => {
    e.stopPropagation();

    setProductSwitcherOpen(false);
    setVersionSwitcherOpen((old) => !old);
  };

  const hasVersions =
    !activeProduct?.versions || activeProduct.versions.length > 1;

  const subItems: Item[] =
    activeVersion?.items
      ?.filter((item) => !!item)
      .map<Item>((item) => ({
        path: item!.path!,
        title: item!.title!,
        items: item!.items
          ? item?.items
              .filter((item) => !!item)
              .map<Item>((item) => ({
                path: item!.path!,
                title: item!.title!,
              }))
          : undefined,
      })) ?? [];

  const basePath = `/docs/${activeProduct!.path!}${
    !!activeVersion?.path?.length ? "/" + activeVersion.path! : ""
  }`;

  return (
    <Navigation height={height} show={showTOC}>
      <DocPagePaneHeader
        title="Table of contents"
        showWhenScreenWidthIsSmallerThan={1070}
        onClose={() => dispatch(closeTOC())}
      />

      <ProductSwitcher>
        <ProductSwitcherButton
          fullWidth={!hasVersions}
          onClick={toggleProductSwitcher}
        >
          {activeProduct?.title}

          <IconContainer size={16}>
            <ProductSwitcherIconSvg />
          </IconContainer>
        </ProductSwitcherButton>

        {hasVersions && (
          <ProductSwitcherButton onClick={toggleVersionSwitcher}>
            {activeVersion?.title}

            <IconContainer size={16}>
              {versionSwitcherOpen ? <ArrowUpIconSvg /> : <ArrowDownIconSvg />}
            </IconContainer>
          </ProductSwitcherButton>
        )}
      </ProductSwitcher>

      <ProductSwitcherDialog
        open={productSwitcherOpen}
        onClick={() => dispatch(closeTOC())}
      >
        {products.map((product) => {
          if (!product) {
            return null;
          }

          return (
            <ProductLink
              active={product === activeProduct}
              key={product.path!}
              to={`/docs/${product.path!}`}
            >
              <ProductTitle>{product.title!}</ProductTitle>
              <ProductDescription>{product.description!}</ProductDescription>
            </ProductLink>
          );
        })}
      </ProductSwitcherDialog>

      <ProductVersionDialog
        open={versionSwitcherOpen}
        onClick={() => dispatch(closeTOC())}
      >
        {activeProduct?.versions?.map((version, index) => {
          const newVersionUrl = selectedPath.replace(
            "/" + selectedVersion,
            "/" + version!.path
          );

          return (
            <VersionLink key={version!.path! + index} to={newVersionUrl}>
              {version!.title!}
            </VersionLink>
          );
        })}
      </ProductVersionDialog>

      {!productSwitcherOpen && activeVersion?.items && (
        <ScrollContainer>
          <MostProminentSection>
            <NavigationContainer
              basePath={basePath}
              items={subItems}
              selectedPath={selectedPath}
            />
          </MostProminentSection>
        </ScrollContainer>
      )}
    </Navigation>
  );
};

function isActiveItem(selectedPath: string, itemPath: string) {
  return selectedPath === itemPath;
}

function containsActiveItem(selectedPath: string, itemPath: string) {
  return (selectedPath + "/").startsWith(itemPath + "/");
}

export const DocPageNavigationGraphQLFragment = graphql`
  fragment DocPageNavigation on Query {
    config: file(
      sourceInstanceName: { eq: "docs" }
      relativePath: { eq: "docs.json" }
    ) {
      products: childrenDocsJson {
        path
        title
        description
        versions {
          path
          title
          items {
            path
            title
            items {
              path
              title
            }
          }
        }
      }
    }
  }
`;

interface Item {
  path: string;
  title: string;
  items?: Item[];
}

type EnhancedItem = Item & {
  fullpath: string;
};

export interface NavigationProps {
  readonly height: string;
  readonly show: boolean;
}

export const Navigation = styled.nav<NavigationProps>`
  ${DocPageStickySideBarStyle}

  padding: 25px 0 0;
  transition: margin-left 250ms;
  background-color: white;
  overflow-y: hidden;
  margin-bottom: 50px;
  display: flex;
  flex-direction: column;

  ${({ show }) => show && `margin-left: 0 !important;`}

  ${({ height }) =>
    IsTablet(`
      margin-left: -100%;
      height: ${height};
      position: fixed;
      top: 60px;
      left: 0;

      ${BoxShadow}
  `)}
`;

const ProductSwitcherButton = styled.button<{ readonly fullWidth?: boolean }>`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--border-radius);
  padding: 7px 10px;
  height: 38px;
  font-size: 0.833em;
  transition: background-color 0.2s ease-in-out;
  ${({ fullWidth }) => fullWidth && `width: 100%;`}

  > ${IconContainer} {
    margin-left: auto;
    padding-left: 6px;

    > svg {
      fill: ${THEME_COLORS.text};
    }
  }

  :hover:enabled {
    background-color: ${THEME_COLORS.boxHighlight};
  }

  :disabled {
    cursor: default;
  }
`;

const ProductSwitcher = styled.div`
  margin: 6px 14px 20px 14px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-shrink: 0;

  > ${ProductSwitcherButton}:not(:last-child) {
    margin-right: 4px;
    flex: 1;
  }

  @media only screen and (min-width: 1070px) {
    margin: 0 0 20px 0;
  }
`;

const ProductSwitcherDialog = styled.div<{ open: boolean }>`
  display: ${({ open }) => (open ? "flex" : "none")};
  flex: 1 1 100%;
  flex-direction: column;
  padding: 0 10px;
  background-color: ${THEME_COLORS.textContrast};

  @media only screen and (min-width: 1070px) {
    top: 135px;
    position: fixed;
    z-index: 10;
    flex-direction: row;
    flex-wrap: wrap;
    border-radius: var(--border-radius);
    padding: 10px;
    width: 700px;
    height: initial;
    box-shadow: 0 3px 6px 0 rgba(0, 0, 0, 0.25);
  }
`;

const ProductVersionDialog = styled.div<{ open: boolean }>`
  display: ${({ open }) => (open ? "flex" : "none")};
  flex-direction: column;
  padding: 10px;
  background-color: ${THEME_COLORS.textContrast};
  position: absolute;
  border-radius: var(--border-radius);
  border: 1px solid ${THEME_COLORS.boxBorder};
  top: 110px;
  right: 14px;

  @media only screen and (min-width: 1070px) {
    top: 66px;
    right: 0;
  }

  > :not(:last-child) {
    margin-bottom: 2px;
  }
`;

interface LinkProps {
  readonly active: boolean;
}

const ProductLink = styled(Link).withConfig<LinkProps>({
  shouldForwardProp(prop, defaultValidatorFn) {
    return prop === "active" ? false : defaultValidatorFn(prop);
  },
})`
  flex: 0 0 auto;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--border-radius);
  margin: 5px;
  padding: 10px;
  font-size: 0.833em;
  color: ${THEME_COLORS.text};
  cursor: pointer;

  @media only screen and (min-width: 1070px) {
    flex: 0 0 calc(50% - 32px);
  }

  transition: background-color 0.2s ease-in-out;

  ${({ active }) =>
    active &&
    css`
      background-color: ${THEME_COLORS.boxHighlight};
    `}

  :hover {
    background-color: ${THEME_COLORS.boxHighlight};
  }
`;

const VersionLink = styled(Link)`
  font-size: 0.833em;
  color: ${THEME_COLORS.text};
  cursor: pointer;
  padding: 6px 9px;
  transition: background-color 0.2s ease-in-out;
  border-radius: var(--border-radius);

  :hover {
    background-color: ${THEME_COLORS.boxHighlight};
  }
`;

const ProductTitle = styled.h6`
  font-size: 1em;
`;

const ProductDescription = styled.p`
  margin-bottom: 0;
`;

const NavigationList = styled.ol`
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0 18px 0px;
  list-style-type: none;

  @media only screen and (min-width: 1070px) {
    display: flex;
    padding: 0 4px 0px;
  }
`;

const NavigationGroupToggle = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  min-height: 20px;
  font-size: 0.833em;
`;

const NavigationGroupContent = styled.div`
  > ${NavigationList} {
    padding: 5px 10px;
  }
`;

const NavigationGroup = styled.div<{ expanded: boolean }>`
  display: flex;
  flex-direction: column;
  cursor: pointer;

  > ${NavigationGroupContent} {
    display: ${({ expanded }) => (expanded ? "initial" : "none")};
  }

  > ${NavigationGroupToggle} > ${IconContainer} {
    margin-left: auto;

    > .arrow-down {
      display: ${({ expanded }) => (expanded ? "none" : "initial")};
      fill: ${THEME_COLORS.text};
    }

    > .arrow-up {
      display: ${({ expanded }) => (expanded ? "initial" : "none")};
      fill: ${THEME_COLORS.text};
    }
  }
`;

const NavigationLink = styled(Link)`
  font-size: 0.833em;
  color: ${THEME_COLORS.text};

  :hover {
    color: #000;
  }
`;

const NavigationItem = styled.li<{ active: boolean }>`
  flex: 0 0 auto;
  margin: 5px 0;
  padding: 0;
  min-height: 20px;
  line-height: initial;

  ${({ active }) =>
    active &&
    css`
      > ${NavigationLink}, > ${NavigationGroup} > ${NavigationGroupToggle} {
        font-weight: 600;
      }
    `}
`;

export const ScrollContainer = styled.div`
  overflow-y: auto;
  padding-bottom: 10px;
`;
