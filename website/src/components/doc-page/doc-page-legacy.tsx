import React, { FC, useEffect } from "react";
import { useCookies } from "react-cookie";
import { useDispatch, useSelector } from "react-redux";
import styled from "styled-components";

import { Link } from "@/components/misc/link";
import { State } from "@/state";
import { hideLegacyDocHeader, showLegacyDocInfo } from "@/state/common";

// Icons
import TimesIconSvg from "@/images/times.svg";

export const DocPageLegacy: FC = () => {
  const show = useSelector<State, boolean>(
    (state) => state.common.showLegacyDocInfo
  );
  const dispatch = useDispatch();
  const cookieName = "chillicream-legacy-doc-info";
  const [cookies, setCookie] = useCookies([cookieName]);
  const cookieValue = cookies[cookieName];

  const clickDismiss = () => {
    setCookie(cookieName, "true", { path: "/" });
  };

  useEffect(() => {
    if (cookieValue === "true") {
      dispatch(hideLegacyDocHeader());
    } else {
      dispatch(showLegacyDocInfo());
    }
  }, [cookieValue]);

  return (
    <Dialog
      role="dialog"
      aria-live="polite"
      aria-label="legacydoc"
      aria-describedby="legacydoc:desc"
      show={show}
    >
      <Container>
        <Message id="legacydoc:desc">
          <strong>Important:</strong> This documentation covers Hot Chocolate
          11. For version 10 and lower click{" "}
          <LegacyDocLink to="https://hotchocolate.io">here</LegacyDocLink>.
        </Message>
        <CloseButton
          aria-label="dismiss cookie message"
          role="button"
          onClick={clickDismiss}
        >
          <TimesIconSvg />
        </CloseButton>
      </Container>
    </Dialog>
  );
};

const Dialog = styled.div<{ show: boolean }>`
  display: ${({ show }) => (show ? "initial" : "none")};
  background-color: #ffb806;

  @media only screen and (min-width: 860px) {
    > .gatsby-image-wrapper {
      border-radius: var(--border-radius) var(--border-radius) 0 0;
    }
  }
`;

const Container = styled.div`
  display: flex;
  flex-direction: row;
  padding: 10px 20px;

  @media only screen and (min-width: 860px) {
    padding: 10px 50px;
  }
`;

const Message = styled.div`
  flex: 0 1 auto;
  font-size: 0.889em;
  line-height: 1.667em;
  color: #4f3903;
`;

const LegacyDocLink = styled(Link)`
  text-decoration: underline;
  color: #4f3903;
`;

const CloseButton = styled.button`
  flex: 0 0 auto;
  margin-left: auto;
  cursor: pointer;

  > svg {
    fill: #4f3903;
    width: 24px;
    height: 24px;
  }
`;
