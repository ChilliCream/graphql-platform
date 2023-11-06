import React, { FC, useEffect } from "react";
import { useCookies } from "react-cookie";
import { useDispatch, useSelector } from "react-redux";
import styled from "styled-components";

import { Button } from "@/components/misc/button";
import { State } from "@/state";
import { hideCookieConsent, showCookieConsent } from "@/state/common";
import { Link } from "./link";

export const CookieConsent: FC = () => {
  const show = useSelector<State, boolean>(
    (state) => state.common.showCookieConsent
  );
  const dispatch = useDispatch();
  const cookieName = "chillicream-cookie-consent";
  const [cookies, setCookie] = useCookies([cookieName]);
  const consentCookieValue = cookies[cookieName];

  const clickDismiss = () => {
    const expires = new Date();

    expires.setFullYear(new Date().getFullYear() + 1);

    setCookie(cookieName, "true", { path: "/", expires });
  };

  useEffect(() => {
    if (consentCookieValue === "true") {
      dispatch(hideCookieConsent());
    } else {
      dispatch(showCookieConsent());
    }
  }, [consentCookieValue]);

  return (
    <Dialog
      role="dialog"
      aria-live="polite"
      aria-label="cookieconsent"
      aria-describedby="cookieconsent:desc"
      show={show}
    >
      {show && (
        <Container>
          <Message id="cookieconsent:desc">
            This website uses cookies to ensure you get the best experience on
            our website.
            <LearnMoreLink prefetch={false} to="/legal/cookie-policy.html">
              Learn more
            </LearnMoreLink>
          </Message>
          <Button
            aria-label="dismiss cookie message"
            role="button"
            onClick={clickDismiss}
          >
            Got it!
          </Button>
        </Container>
      )}{" "}
    </Dialog>
  );
};

const Dialog = styled.div<{ show: boolean }>`
  position: fixed;
  bottom: 0;
  z-index: 30;
  width: 100vw;
  background-color: #ffb806;
  display: ${({ show }) => (show ? "visible" : "none")};
`;

const Container = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: space-between;

  padding: 15px 20px;

  @media only screen and (min-width: 400px) {
    flex-direction: row;

    ${Button} {
      flex: 0 0 160px;
    }
  }
`;

const Message = styled.div`
  flex: 0 1 auto;
  padding-bottom: 20px;
  font-size: var(--font-size);
  line-height: 1.667em;
  color: #4f3903;

  @media only screen and (min-width: 400px) {
    padding-bottom: initial;
    padding-right: 20px;
  }
`;

const LearnMoreLink = styled(Link)`
  text-decoration: underline;
  color: #4f3903;
`;
