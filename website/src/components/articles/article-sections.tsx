import { graphql, Link } from "gatsby";
import React, { FunctionComponent, useMemo } from "react";
import { useDispatch } from "react-redux";
import styled from "styled-components";
import { ArticleSectionsFragment } from "../../../graphql-types";
import { closeAside } from "../../state/common";
import { MostProminentSection } from "../doc-page/doc-page-elements";
const slugger = require("github-slugger").slug;

const MAX_DEPTH = 3;

function getTocItemsFromHeadings(
  headings: ArticleSectionsFragment["headings"]
): TableOfContentItem[] {
  const items: TableOfContentItem[] = [];

  if (!headings || headings?.length < 1) {
    return items;
  }

  // this represents a path to the current item
  let parents: TableOfContentItem[] = [];

  for (const heading of headings) {
    if (!heading?.value) {
      continue;
    }

    const headingDepth = heading.depth ?? 0;

    if (headingDepth > MAX_DEPTH) {
      continue;
    }

    const item: TableOfContentItem = {
      title: heading.value,
      slug: slugger(heading.value),
      items: [],
    };

    if (headingDepth === 1) {
      items.push(item);

      parents = [item];
    } else {
      // we went up in depth, so lets remove parents until we find the parent
      // directly above us
      while (parents.length >= headingDepth) {
        parents.pop();
      }

      const parent = parents[parents.length - 1];

      item.slug = `${parent.slug}-${item.slug}`;

      parent.items?.push(item);

      parents.push(item);
    }
  }

  return items;
}

interface ArticleSectionsProperties {
  readonly data: ArticleSectionsFragment;
}

export const ArticleSections: FunctionComponent<ArticleSectionsProperties> = ({
  data,
}) => {
  const dispatch = useDispatch();

  const tocItems = useMemo(() => getTocItemsFromHeadings(data.headings), [
    data.headings,
  ]);

  console.log({ tocItems });

  if (tocItems.length < 1) {
    return null;
  }

  const handleCloseClick = () => {
    dispatch(closeAside());
  };

  return (
    <Container>
      <Title>In this article</Title>
      <MostProminentSection>
        <div onClick={handleCloseClick}>
          <TableOfContent items={tocItems} />
        </div>
      </MostProminentSection>
    </Container>
  );

  // const dispatch = useDispatch();
  // const yScrollPosition$ = useObservable(
  //   (state) => state.common.yScrollPosition
  // );

  // const handleCloseClick = useCallback(() => {
  //   dispatch(closeAside());
  // }, [dispatch]);

  // useEffect(() => {
  //   const headings = (
  //     (data.tableOfContents.items as TableOfContentItem[]) ?? []
  //   )
  //     .flatMap((item) => [item, ...(item.items ?? [])])
  //     .map<Heading>((item) => ({
  //       id: item.url,
  //       title: item.title,
  //       position:
  //         (document.getElementById(item.url.substring(1))?.offsetTop ?? 80) -
  //         80,
  //     }))
  //     .reverse();
  //   let currentActiveId: string | undefined;
  //   let currentActiveClass: string = "";
  //   // let timeoutHandler: number | undefined;
  //   let subscription: Subscription | undefined;

  //   if (headings.length > 0) {
  //     subscription = yScrollPosition$.subscribe((yScrollPosition) => {
  //       let newActiveId: string | undefined;
  //       let title: string | undefined;

  //       for (let i = 0; i < headings.length; i++) {
  //         if (yScrollPosition >= headings[i].position) {
  //           newActiveId = headings[i].id;
  //           title = headings[i].title;
  //           break;
  //         }
  //       }

  //       if (currentActiveId !== newActiveId) {
  //         if (currentActiveId) {
  //           document.getElementById(
  //             harmonizeId(currentActiveId)
  //           )!.className = currentActiveClass;
  //         }

  //         currentActiveId = newActiveId;
  //         // clearTimeout(timeoutHandler);

  //         if (currentActiveId) {
  //           const element = document.getElementById(
  //             harmonizeId(currentActiveId)
  //           )!;

  //           currentActiveClass = element.className;
  //           element.className = currentActiveClass + " active";
  //           // timeoutHandler = window.setTimeout(() => {
  //           //   window.history.pushState(
  //           //     undefined,
  //           //     title ?? "ChilliCream Docs", // todo: default heading should be the doc title
  //           //     `./${currentActiveId ?? ""}`
  //           //   );
  //           // }, 250);
  //         }
  //         // else {
  //         //   timeoutHandler = window.setTimeout(() => {
  //         //     window.history.pushState(
  //         //       undefined,
  //         //       "ChilliCream Docs", // todo: default heading should be the doc title
  //         //       "./"
  //         //     );
  //         //   }, 250);
  //         // }
  //       }
  //     });
  //   }

  //   return () => {
  //     subscription?.unsubscribe();
  //   };
  // }, [data]);
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
        <TocListItem key={item.slug} id={"link-" + item.slug}>
          <TocLink to={"#" + item.slug}>{item.title}</TocLink>
          {item.items && <TableOfContent items={item.items ?? []} />}
        </TocListItem>
      ))}
    </TocItemContainer>
  );
};

interface TableOfContentItem {
  readonly title: string;
  slug: string;
  readonly items?: TableOfContentItem[];
}

export const ArticleSectionsGraphQLFragment = graphql`
  fragment ArticleSections on Mdx {
    headings {
      depth
      value
    }
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
  padding: 0 25px 2px;
  list-style-type: none;

  @media only screen and (min-width: 1320px) {
    padding: 0 20px 2px;
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
