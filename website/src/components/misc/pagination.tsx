import { Link } from "gatsby";
import React, { FC } from "react";
import styled from "styled-components";

import { THEME_COLORS } from "@/shared-style";

export interface PaginationProps {
  readonly currentPage: number;
  readonly linkPrefix: string;
  readonly totalPages: number;
}

export const Pagination: FC<PaginationProps> = ({
  currentPage,
  linkPrefix,
  totalPages,
}) => {
  const items: { page: number; link: string }[] = [];

  for (let i = 0; i < totalPages; i++) {
    const page = i + 1;
    const suffix = page === 1 ? "" : "/" + page;

    items.push({ page, link: linkPrefix + suffix });
  }

  return (
    <Container>
      {items.map((item) => (
        <Page
          key={`page-${item.page}`}
          className={item.page === currentPage ? "active" : undefined}
        >
          <PageLink to={item.link}>{item.page}</PageLink>
        </Page>
      ))}
    </Container>
  );
};

const Container = styled.ol`
  margin: 0 0 60px;
  padding: 0;
  list-style-type: none;
`;

const Page = styled.li`
  display: inline-block;
  margin: 0 5px;
  border-radius: var(--border-radius);
  padding: 0;
  background-color: ${THEME_COLORS.primary};

  &.active,
  &:hover {
    background-color: ${THEME_COLORS.secondary};
  }
`;

const PageLink = styled(Link)`
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  line-height: 30px;
  color: ${THEME_COLORS.textContrast};
`;
