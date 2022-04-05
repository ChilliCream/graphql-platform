import styled from "styled-components";

export const List = styled.div`
  display: flex;
  gap: 2rem;
  background-color: #e8ecf5;

  ~ .gatsby-highlight {
    margin-top: 0;
  }

  ~ p {
    margin-top: 14px;
  }
`;
