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
import { DocPageNavigationFragment } from "../../../graphql-types";
import ArrowDownIconSvg from "../../images/arrow-down.svg";
import ArrowUpIconSvg from "../../images/arrow-up.svg";
import ProductSwitcherIconSvg from "../../images/th-large.svg";
import { BoxShadow, IsTablet } from "../../shared-style";
import { State } from "../../state";
import { closeTOC } from "../../state/common";
import { IconContainer } from "../misc/icon-container";
import { Link } from "../misc/link";
import {
  DocPageStickySideBarStyle,
  MostProminentSection,
} from "./doc-page-elements";
import { DocPagePaneHeader } from "./doc-page-pane-header";

interface NavigationContainerProps {
  basePath: string;
  selectedPath: string;
  items: Item[];
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

interface DocPageNavigationProps {
  data: DocPageNavigationFragment;
  selectedPath: string;
  selectedProduct: string;
  selectedVersion: string;
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

  return (
    <Navigation height={height} show={showTOC}>
      <DocPagePaneHeader
        title="Table of contents"
        showWhenScreenWidthIsSmallerThan={1070}
        onClose={() => dispatch(closeTOC())}
      />

      <ProductSwitcher>
        <ProductSwitcherButton onClick={toggleProductSwitcher}>
          {activeProduct?.title}

          <IconContainer size={16}>
            <ProductSwitcherIconSvg />
          </IconContainer>
        </ProductSwitcherButton>

        <ProductSwitcherButton
          disabled={!hasVersions}
          onClick={toggleVersionSwitcher}
        >
          {activeVersion?.title}

          {hasVersions && (
            <IconContainer size={12}>
              {versionSwitcherOpen ? <ArrowUpIconSvg /> : <ArrowDownIconSvg />}
            </IconContainer>
          )}
        </ProductSwitcherButton>
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
        {activeProduct?.versions?.map((version, index) => (
          <VersionLink
            key={version!.path! + index}
            to={`/docs/${activeProduct.path}/${version!.path!}`}
          >
            {version!.title!}
          </VersionLink>
        ))}
      </ProductVersionDialog>

      {!productSwitcherOpen && activeVersion?.items && (
        <MostProminentSection>
          <NavigationContainer
            basePath={`/docs/${activeProduct!.path!}${
              !!activeVersion?.path?.length ? "/" + activeVersion.path! : ""
            }`}
            items={subItems}
            selectedPath={selectedPath}
          />
        </MostProminentSection>
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

export const Navigation = styled.nav<{ height: string; show: boolean }>`
  ${DocPageStickySideBarStyle}
  padding: 25px 0 0;
  transition: margin-left 250ms;
  background-color: white;

  ${({ show }) => show && `margin-left: 0 !important;`}

  ${({ height }) =>
    IsTablet(`
      margin-left: -105%;
      height: ${height};
      position: fixed;
      top: 60px;
      left: 0;
      ${BoxShadow}
  `)}
`;

const ProductSwitcherButton = styled.button`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  border: 1px solid var(--box-border-color);
  border-radius: var(--border-radius);
  padding: 7px 10px;
  height: 38px;
  font-size: 0.833em;
  transition: background-color 0.2s ease-in-out;

  > ${IconContainer} {
    margin-left: auto;
    padding-left: 6px;

    > svg {
      fill: var(--text-color);
    }
  }

  :hover:enabled {
    background-color: var(--box-highlight-color);
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
  background-color: var(--text-color-contrast);

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
  background-color: var(--text-color-contrast);
  position: absolute;
  border-radius: var(--border-radius);
  border: 1px solid var(--box-border-color);
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

const ProductLink = styled(Link)<LinkProps>`
  flex: 0 0 auto;
  border: 1px solid var(--box-border-color);
  border-radius: var(--border-radius);
  margin: 5px;
  padding: 10px;
  font-size: 0.833em;
  color: var(--text-color);
  cursor: pointer;

  @media only screen and (min-width: 1070px) {
    flex: 0 0 calc(50% - 32px);
  }

  transition: background-color 0.2s ease-in-out;

  ${({ active }) => active && `background-color: var(--box-highlight-color);`}

  :hover {
    background-color: var(--box-highlight-color);
  }
`;

const VersionLink = styled(Link)`
  font-size: 0.833em;
  color: var(--text-color);
  cursor: pointer;
  padding: 6px 9px;
  transition: background-color 0.2s ease-in-out;
  border-radius: var(--border-radius);

  :hover {
    background-color: var(--box-highlight-color);
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
  padding: 0 18px 20px;
  list-style-type: none;

  @media only screen and (min-width: 1070px) {
    display: flex;
    padding: 0 4px 20px;
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
      fill: var(--text-color);
    }

    > .arrow-up {
      display: ${({ expanded }) => (expanded ? "initial" : "none")};
      fill: var(--text-color);
    }
  }
`;

const NavigationLink = styled(Link)`
  font-size: 0.833em;
  color: var(--text-color);

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
        font-weight: bold;
      }
    `}
`;
