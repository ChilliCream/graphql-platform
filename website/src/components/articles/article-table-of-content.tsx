import { Link } from "@/components/misc";
import GithubSlugger from "github-slugger";
import React, { FC, useEffect, useMemo, useState } from "react";
import { useDispatch } from "react-redux";
import { asyncScheduler } from "rxjs";
import { throttleTime } from "rxjs/operators";
import styled, { css } from "styled-components";

import { ScrollContainer } from "@/components/article-elements";
import { useObservable } from "@/state";
import { closeAside } from "@/state/common";
import { THEME_COLORS } from "@/style";

const MAX_TOC_DEPTH = 2;

interface ArticleSectionsData {
  headings?: Array<{
    depth?: number;
    value?: string;
  } | null>;
}

export interface ArticleTableOfContentProps {
  readonly data: ArticleSectionsData;
}

export const ArticleTableOfContent: FC<ArticleTableOfContentProps> = ({
  data,
}) => {
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
    <ScrollContainer>
      <Title>In this Article</Title>
      <div onClick={handleCloseClick}>
        <TableOfContent
          items={tocItems}
          activeHeadingLink={activeHeadingLink}
        />
      </div>
    </ScrollContainer>
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

const TocItemContainer = styled.ul.attrs({
  className: "text-3",
})`
  display: block;
  margin: 0;
  padding: 0 25px;
  list-style-type: none;

  @media only screen and (min-width: 1320px) {
    padding: 0;
  }
`;

const TocLink = styled(Link)`
  color: ${THEME_COLORS.text};
  transition: color 0.2s ease-in-out;

  :hover {
    color: ${THEME_COLORS.linkHover};
  }
`;

interface TocListItemProps {
  readonly active: boolean;
}

const TocListItem = styled.li<TocListItemProps>`
  flex: 0 0 auto;
  margin: 8px 0;
  padding: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: normal;

  > ${TocItemContainer} {
    margin-left: 12px;
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
  headings?: Array<{
    depth?: number;
    value?: string;
  } | null>
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

export const Title = styled.h3`
  margin-bottom: 12px;
  padding: 0 25px;
  font-size: 0.875rem;
  font-weight: 600;

  @media only screen and (min-width: 1320px) {
    padding: 0;
  }
`;
