import { graphql } from "gatsby";
import React, {
  FunctionComponent,
  useState,
  useEffect,
  MouseEvent,
  useCallback,
} from "react";
import { useDispatch, useSelector } from "react-redux";
import styled from "styled-components";
import { DocPageNavigationFragment } from "../../../graphql-types";
import { State } from "../../state";
import { toggleNavigationGroup } from "../../state/common";
import { IconContainer } from "./icon-container";
import { Link } from "./link";

import ArrowDownIconSvg from "../../images/arrow-down.svg";
import ArrowUpIconSvg from "../../images/arrow-up.svg";
import ProductSwitcherIconSvg from "../../images/th-large.svg";

interface DocPageNavigationProperties {
  data: DocPageNavigationFragment;
  selectedProduct: string;
}

export const DocPageNavigation: FunctionComponent<DocPageNavigationProperties> = ({
  data,
  selectedProduct,
}) => {
  const expandedPaths = useSelector<State, string[]>(
    (state) => state.common.expandedPaths
  );
  const dispatch = useDispatch();
  const [productSwitcherOpen, setProductSwitcherOpen] = useState(false);
  const currentProduct =
    data.config?.products &&
    data.config.products.find((product) => product?.path === selectedProduct);

  const handleClickDialog = useCallback((event: MouseEvent<HTMLDivElement>) => {
    event.stopPropagation();
  }, []);

  const handleCloseClick = useCallback(() => {
    setProductSwitcherOpen(false);
  }, []);

  const handleToggleClick = useCallback(
    (event: MouseEvent<HTMLButtonElement>, isOpen) => {
      setProductSwitcherOpen(!isOpen);
      event.stopPropagation();
    },
    []
  );

  const handleToggleExpand = useCallback((path: string) => {
    dispatch(toggleNavigationGroup({ path }));
  }, []);

  const buildNavigationStructure = (items: Item[], basePath: string) => (
    <NavigationList>
      {items.map(({ path, title, items: subItems }) => {
        const itemPath =
          !subItems && path === "index" ? basePath : basePath + "/" + path;

        return (
          <NavigationItem key={itemPath}>
            {subItems ? (
              <NavigationGroup
                expanded={expandedPaths.indexOf(itemPath) !== -1}
              >
                <NavigationGroupToggle
                  onClick={() => handleToggleExpand(itemPath)}
                >
                  {title}
                  <IconContainer size={16}>
                    <ArrowDownIconSvg className="arrow-down" />
                    <ArrowUpIconSvg className="arrow-up" />
                  </IconContainer>
                </NavigationGroupToggle>
                <NavigationGroupContent>
                  {buildNavigationStructure(subItems, itemPath)}
                </NavigationGroupContent>
              </NavigationGroup>
            ) : (
              <NavigationLink to={itemPath}>{title}</NavigationLink>
            )}
          </NavigationItem>
        );
      })}
    </NavigationList>
  );

  useEffect(() => {
    window.addEventListener("click", handleCloseClick);

    return () => {
      window.removeEventListener("click", handleCloseClick);
    };
  }, [handleCloseClick]);

  return (
    <Navigation>
      <FixedContainer>
        <ProductSwitcher>
          <ProductSwitcherButton
            onClick={(e) => handleToggleClick(e, productSwitcherOpen)}
          >
            {currentProduct?.title}
            <IconContainer size={16}>
              <ProductSwitcherIconSvg />
            </IconContainer>
          </ProductSwitcherButton>
          <ProductSwitcherDialog
            open={productSwitcherOpen}
            onClick={handleClickDialog}
          >
            {data.config?.products &&
              data.config.products.map((product) =>
                product === currentProduct ? (
                  <CurrentProduct onClick={handleCloseClick}>
                    <ProductTitle>{product!.title!}</ProductTitle>
                    <ProductDescription>
                      {product!.description!}
                    </ProductDescription>
                  </CurrentProduct>
                ) : (
                  <ProductLink to={`/docs/${product!.path!}`}>
                    <ProductTitle>{product!.title!}</ProductTitle>
                    <ProductDescription>
                      {product!.description!}
                    </ProductDescription>
                  </ProductLink>
                )
              )}
          </ProductSwitcherDialog>
        </ProductSwitcher>
        {currentProduct?.items &&
          buildNavigationStructure(
            currentProduct.items
              .filter((item) => !!item)
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
              })),
            `/docs/${currentProduct.path!}`
          )}
      </FixedContainer>
    </Navigation>
  );
};

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
`;

interface Item {
  path: string;
  title: string;
  items?: Item[];
}

const Navigation = styled.nav`
  display: none;
  flex: 0 0 250px;
  flex-direction: column;
  z-index: 1;

  * {
    user-select: none;
  }

  @media only screen and (min-width: 1050px) {
    display: flex;
  }
`;

const FixedContainer = styled.div`
  position: fixed;
  padding: 25px 0 250px;
  width: 250px;
  overflow: initial;
`;

const ProductSwitcher = styled.div``;

const ProductSwitcherButton = styled.button`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 6px 14px 20px;
  border: 1px solid #ccc;
  border-radius: 5px;
  padding: 7px 5px;
  width: calc(100% - 28px);
  font-size: 0.833em;
  transition: background-color 0.2s ease-in-out;

  > ${IconContainer} {
    margin-left: auto;

    > svg {
      fill: #666;
    }
  }

  :hover {
    background-color: #ddd;
  }
`;

const ProductSwitcherDialog = styled.div<{ open: boolean }>`
  position: fixed;
  top: 130px;
  z-index: 10;
  display: ${(props) => (props.open ? "flex" : "none")};
  flex-direction: row;
  flex-wrap: wrap;
  margin: 0 14px;
  padding: 10px;
  border-radius: 5px;
  width: 700px;
  background-color: #fff;
  box-shadow: 0 3px 6px 0 rgba(0, 0, 0, 0.25);
`;

const CurrentProduct = styled.div`
  flex: 0 0 calc(50% - 32px);
  border: 1px solid #ccc;
  border-radius: 5px;
  margin: 5px;
  padding: 10px;
  font-size: 0.833em;
  color: #666;
  background-color: #ddd;
`;

const ProductLink = styled(Link)`
  flex: 0 0 calc(50% - 32px);
  border: 1px solid #ccc;
  border-radius: 5px;
  margin: 5px;
  padding: 10px;
  font-size: 0.833em;
  color: #666;
  transition: background-color 0.2s ease-in-out;

  :hover {
    background-color: #ddd;
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
  padding: 0 20px 20px;
`;

const NavigationItem = styled.li`
  flex: 0 0 auto;
  margin: 5px 0;
  padding: 0;
  min-height: 20px;
  list-style-type: none;
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
    padding: 5px 0;
  }
`;

const NavigationGroup = styled.div<{ expanded: boolean }>`
  display: flex;
  flex-direction: column;
  cursor: pointer;

  > ${NavigationGroupContent} {
    display: ${(props) => (props.expanded ? "initial" : "none")};
  }

  > ${NavigationGroupToggle} > ${IconContainer} {
    margin-left: auto;

    > .arrow-down {
      display: ${(props) => (props.expanded ? "none" : "initial")};
      fill: #666;
    }

    > .arrow-up {
      display: ${(props) => (props.expanded ? "initial" : "none")};
      fill: #666;
    }
  }
`;

const NavigationLink = styled(Link)`
  font-size: 0.833em;
  color: #666;

  :hover {
    color: #000;
  }
`;
