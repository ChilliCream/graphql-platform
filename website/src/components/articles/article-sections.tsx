import { graphql, Link } from "gatsby";
import React, { FunctionComponent, useCallback, useEffect } from "react";
import { useDispatch } from "react-redux";
import { Subscription } from "rxjs";
import styled from "styled-components";
import { ArticleSectionsFragment } from "../../../graphql-types";
import { useObservable } from "../../state";
import { closeAside } from "../../state/common";
import { MostProminentSection } from "../doc-page/doc-page-elements";

interface ArticleSectionsProperties {
  readonly data: ArticleSectionsFragment;
}

export const ArticleSections: FunctionComponent<ArticleSectionsProperties> = ({
  data,
}) => {
  const dispatch = useDispatch();
  const yScrollPosition$ = useObservable(
    (state) => state.common.yScrollPosition
  );

  const handleCloseClick = useCallback(() => {
    dispatch(closeAside());
  }, [dispatch]);

  useEffect(() => {
    const headings = (
      (data.tableOfContents.items as TableOfContentItem[]) ?? []
    )
      .flatMap((item) => [item, ...(item.items ?? [])])
      .map<Heading>((item) => ({
        id: item.url,
        title: item.title,
        position:
          (document.getElementById(item.url.substring(1))?.offsetTop ?? 80) -
          80,
      }))
      .reverse();
    let currentActiveId: string | undefined;
    let currentActiveClass: string = "";
    let timeoutHandler: number | undefined;
    let subscription: Subscription | undefined;

    if (headings.length > 0) {
      subscription = yScrollPosition$.subscribe((yScrollPosition) => {
        let newActiveId: string | undefined;
        let title: string | undefined;

        for (let i = 0; i < headings.length; i++) {
          if (yScrollPosition >= headings[i].position) {
            newActiveId = headings[i].id;
            title = headings[i].title;
            break;
          }
        }

        if (currentActiveId !== newActiveId) {
          if (currentActiveId) {
            document.getElementById(
              harmonizeId(currentActiveId)
            )!.className = currentActiveClass;
          }

          currentActiveId = newActiveId;
          clearTimeout(timeoutHandler);

          if (currentActiveId) {
            const element = document.getElementById(
              harmonizeId(currentActiveId)
            )!;

            currentActiveClass = element.className;
            element.className = currentActiveClass + " active";
            timeoutHandler = window.setTimeout(() => {
              window.history.pushState(
                undefined,
                title ?? "ChilliCream Docs", // todo: default heading should be the doc title
                `./${currentActiveId ?? ""}`
              );
            }, 250);
          } else {
            timeoutHandler = window.setTimeout(() => {
              window.history.pushState(
                undefined,
                "ChilliCream Docs", // todo: default heading should be the doc title
                "./"
              );
            }, 250);
          }
        }
      });
    }

    return () => {
      subscription?.unsubscribe();
    };
  }, [data]);

  const tocItems: TableOfContentItem[] = data.tableOfContents.items ?? [];

  return tocItems.length > 0 ? (
    <Container>
      <Title>In this article</Title>
      <MostProminentSection>
        <div onClick={handleCloseClick}>
          <TableOfContent items={tocItems} />
        </div>
      </MostProminentSection>
    </Container>
  ) : null;
};

interface Heading {
  readonly id: string;
  readonly title: string;
  readonly position: number;
}

interface TableOfContentProps {
  readonly items: TableOfContentItem[];
}

const TableOfContent: FunctionComponent<TableOfContentProps> = ({ items }) => {
  return (
    <TocItemContainer>
      {items.map((item) => (
        <TocListItem key={item.url} id={harmonizeId(item.url)}>
          <TocLink to={item.url}>{item.title}</TocLink>
          {item.items && <TableOfContent items={item.items ?? []} />}
        </TocListItem>
      ))}
    </TocItemContainer>
  );
};

function harmonizeId(id: string): string {
  return id.replace("#", "link-");
}

interface TableOfContentItem {
  readonly title: string;
  readonly url: string;
  readonly items?: TableOfContentItem[];
}

export const ArticleSectionsGraphQLFragment = graphql`
  fragment ArticleSections on Mdx {
    tableOfContents(maxDepth: 2)
  }
`;

const Container = styled.section`
  margin-bottom: 20px;
`;

const Title = styled.h6`
  padding: 0 25px;
  font-size: 0.833em;

  @media only screen and (min-width: 1320px) {
    padding: 0 20px;
  }
`;

const TocItemContainer = styled.ul`
  display: block;
  margin: 0;
  padding: 0 25px 10px;
  list-style-type: none;

  @media only screen and (min-width: 1320px) {
    padding: 0 20px 10px;
  }
`;

const TocLink = styled((props) => <Link {...props} />)`
  font-size: 0.833em;
  color: #666;

  :hover {
    color: #000;
  }
`;

const TocListItem = styled.li`
  flex: 0 0 auto;
  margin: 5px 0;
  padding: 0;
  line-height: initial;

  > ${TocItemContainer} {
    padding-right: 0;
  }

  &.active > ${TocLink} {
    font-weight: bold;
  }
`;
