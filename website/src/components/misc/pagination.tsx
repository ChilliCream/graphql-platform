import { Link } from "gatsby";
import React, { FunctionComponent } from "react";
import styled from "styled-components";

interface PaginationProperties {
  currentPage: number;
  linkPrefix: string;
  totalPages: number;
}

export const Pagination: FunctionComponent<PaginationProperties> = ({
  currentPage,
  linkPrefix,
  totalPages,
}) => {
  const links: string[] = [];

  for (let i = 0; i < totalPages; i++) {
    if (i === 0) {
      links.push(`${linkPrefix}`);
    } else {
      links.push(`${linkPrefix}/${i + 1}`);
    }
  }

  return (
    <Container>
      {links.map((link, index) => (
        <Page>
          <Link to={link}>{index + 1}</Link>
        </Page>
      ))}
    </Container>
  );
};

const Container = styled.ol``;

const Page = styled.li``;
