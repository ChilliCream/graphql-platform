import styled from "styled-components";

export const Button = styled.button`
  padding: 10px;
  border-radius: var(--border-radius);
  font-size: var(--font-size);
  color: var(--text-color-contrast);

  background-color: var(--brand-color);
  transition: background-color 0.2s ease-in-out;

  &:hover {
    background-color: var(--brand-color-hover);
  }
`;
