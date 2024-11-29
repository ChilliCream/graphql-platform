import { Link } from "gatsby";
import React, { FC } from "react";
import styled from "styled-components";

import { FONT_FAMILY_HEADING, THEME_COLORS } from "@/style";

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
        <Page key={`page-${item.page}`}>
          <PageLink
            to={item.link}
            className={item.page === currentPage ? "active" : undefined}
          >
            {item.page}
          </PageLink>
        </Page>
      ))}
    </Container>
  );
};

const Container = styled.ol`
  display: flex;
  gap: 8px;
  margin: 0 0 60px;
  padding: 0;
  list-style-type: none;
`;

const Page = styled.li`
  display: inline-block;
  margin: 0;
  padding: 0;
`;

const PageLink = styled(Link)`
  display: flex;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  width: 30px;
  height: 30px;
  line-height: 30px;
  border: 2px solid ${THEME_COLORS.primaryButtonBorder};
  border-radius: var(--button-border-radius);
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 1rem;
  font-weight: 500;
  text-decoration: none;
  color: ${THEME_COLORS.primaryButtonText};
  background-color: ${THEME_COLORS.primaryButton};
  transition: background-color 0.2s ease-in-out, border-color 0.2s ease-in-out,
    color 0.2s ease-in-out;

  &.active {
    color: ${THEME_COLORS.background};
  }

  :hover {
    border-color: ${THEME_COLORS.primaryButtonBorder};
    color: ${THEME_COLORS.primaryButtonHoverText};
    background-color: ${THEME_COLORS.primaryButtonHover};
  }
`;
