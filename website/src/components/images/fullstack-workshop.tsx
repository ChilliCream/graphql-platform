import React, { FC } from "react";
import styled from "styled-components";

export const FullstackWorkshopImage: FC = () => {
  return (
    <Container>
      <img
        src="/blog/2024-04-01-fullstack-workshop/header.png"
        alt="Fullstack Workshop"
        style={{ width: "100%", height: "auto", borderRadius: "var(--border-radius)" }}
      />
    </Container>
  );
};

const Container = styled.div`
  padding: 30px;
`;
