import { graphql, Link } from "gatsby";
import GithubSlugger from "github-slugger";
import React, { FC, useEffect, useMemo, useState } from "react";
import { useDispatch } from "react-redux";
import { asyncScheduler } from "rxjs";
import { throttleTime } from "rxjs/operators";
import styled, { css } from "styled-components";

import { MostProminentSection } from "@/components/doc-page/doc-page-elements";
import { ArticleSectionsFragment } from "@/graphql-types";
import { THEME_COLORS } from "@/shared-style";
import { useObservable } from "@/state";
import { closeAside } from "@/state/common";

const MAX_TOC_DEPTH = 2;

export interface ArticleSectionsProps {
  readonly data: ArticleSectionsFragment;
}

export const ArticleSections: FC<ArticleSectionsProps> = ({ data }) => {
  const dispatch = useDispatch();

  const tocItems = useMemo(
    () => getTocItemsFromHeadings(data.headings),
    [data.headings]
  );

  const activeHeadingLink = useActiveHeadingLink(tocItems);

  const handleCloseClick = () => {
    dispatch(closeAside());
  };

  return tocItems.length < 1 ? null : (
    <Container>
      <Title>In this article</Title>
      <MostProminentSection>
        <div onClick={handleCloseClick}>
          <TableOfContent
            items={tocItems}
            activeHeadingLink={activeHeadingLink}
          />
        </div>
      </MostProminentSection>
    </Container>
  );
};

interface TableOfContentProps {
  readonly items: TableOfContentItem[];
  readonly activeHeadingLink?: string;
}

const TableOfContent: FC<TableOfContentProps> = ({
  items,
  activeHeadingLink,
}) => {
  return (
    <TocItemContainer>
      {items.map((item) => (
        <TocListItem
          key={item.headingLink}
          active={activeHeadingLink === item.headingLink}
        >
          <TocLink to={"#" + item.headingLink}>{item.title}</TocLink>
          {item.items && (
            <TableOfContent
              items={item.items ?? []}
              activeHeadingLink={activeHeadingLink}
            />
          )}
        </TocListItem>
      ))}
    </TocItemContainer>
  );
};

interface TableOfContentItem {
  readonly title: string;
  readonly headingLink: string;
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

const Container = styled.section``;

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
  color: ${THEME_COLORS.text};

  :hover {
    color: #000;
  }
`;

interface TocListItemProps {
  readonly active: boolean;
}

const TocListItem = styled.li<TocListItemProps>`
  flex: 0 0 auto;
  margin: 5px 0;
  padding: 0;
  line-height: initial;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: normal;

  > ${TocItemContainer} {
    padding-right: 0;
  }

  ${({ active }) =>
    active &&
    css`
      > ${TocLink} {
        font-weight: 600;
      }
    `}
`;

function getTocItemsFromHeadings(
  headings: ArticleSectionsFragment["headings"]
): TableOfContentItem[] {
  const items: TableOfContentItem[] = [];

  if (!headings?.length) {
    return items;
  }

  const slugger = new GithubSlugger();

  // this represents a path to the current item
  const parents: TableOfContentItem[] = [];

  for (const heading of headings) {
    if (!heading?.value) {
      continue;
    }

    const item: TableOfContentItem = {
      title: heading.value,
      headingLink: slugger.slug(heading.value),
      items: [],
    };

    const headingDepth = heading.depth;

    if (!headingDepth || headingDepth > MAX_TOC_DEPTH) {
      continue;
    }

    // we went up in depth, so lets remove parents until we find the parent
    // directly above us
    while (parents.length >= headingDepth) {
      parents.pop();
    }

    const parent = parents[parents.length - 1];

    parents.push(item);

    if (parent?.items) {
      parent.items.push(item);
    } else {
      items.push(item);
    }
  }

  return items;
}

interface Heading {
  readonly link: string;
  readonly element: HTMLElement | null;
}

function useActiveHeadingLink(items: TableOfContentItem[]): string | undefined {
  const [activeHeadingLink, setActiveHeadingLink] = useState<string>();

  const yScrollPosition$ = useObservable(
    (state) => state.common.yScrollPosition
  );

  useEffect(() => {
    const headings =
      items
        ?.flatMap((item) => [item, ...(item.items ?? [])])
        .map<Heading>((item) => ({
          link: item.headingLink,
          element: document.getElementById(item.headingLink),
        }))
        .reverse() ?? [];

    const subscription = yScrollPosition$
      .pipe(
        throttleTime(100, asyncScheduler, { leading: true, trailing: true })
      )
      .subscribe((yScrollPosition) => {
        // the yScrollPosition is the scrollTop relative to the main-content.
        // the offsetTop of the headings is relative to the article-sections.
        // the article-section has some space between itself and the main-content.
        // that's why we add this space to get accurate results.
        yScrollPosition += 95;

        // traverse headings from bottom to top
        for (let i = 0; i < headings.length; i++) {
          const currentHeading = headings[i];

          if (!currentHeading.element) {
            continue;
          }

          const headingPosition = currentHeading.element.offsetTop;

          // the active item is above the current item
          if (yScrollPosition < headingPosition) {
            // there are no other active items above us
            if (i === headings.length - 1) {
              setActiveHeadingLink(undefined);
            }

            continue;
          }

          setActiveHeadingLink(currentHeading.link);
          break;
        }
      });

    return () => {
      subscription.unsubscribe();
    };
  }, [items]);

  return activeHeadingLink;
}
