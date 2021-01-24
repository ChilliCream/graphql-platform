import styled from "styled-components";

export const List = styled.div`
    display: flex;
    gap: 2rem;
    background-color: #e7e9eb;

    ~ .gatsby-highlight {
        margin-top: 0;
    }

    ~ p {
        margin-top: 14px;
    }
`;