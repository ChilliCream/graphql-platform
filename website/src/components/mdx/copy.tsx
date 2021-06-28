import React, { FunctionComponent, useState } from "react";
import styled from "styled-components";

function copyToClipboard(content: string): void {
  const el = document.createElement(`textarea`);
  el.value = content;
  el.setAttribute(`readonly`, ``);
  el.style.position = `absolute`;
  el.style.left = `-9999px`;
  document.body.appendChild(el);
  el.select();
  document.execCommand(`copy`);
  document.body.removeChild(el);
}

interface CoopyProps {
  content: string;
}

export const Copy: FunctionComponent<CoopyProps> = ({ content }) => {
  const [showToast, setShowToast] = useState(false);

  return (
    <>
      <CopyIconButton
        onClick={() => {
          copyToClipboard(content);

          setShowToast(true);
          setTimeout(() => {
            setShowToast(false);
          }, 3000);
        }}
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          width="24"
          height="24"
          viewBox="0 0 24 24"
        >
          <path fill="none" d="M0 0h24v24H0V0z" />
          <path d="M16 1H2v16h2V3h12V1zm-1 4l6 6v12H6V5h9zm-1 7h5.5L14 6.5V12z" />
        </svg>
      </CopyIconButton>
      {showToast && <CopySuccessToast />}
    </>
  );
};

const CopySuccessToast: FunctionComponent = () => {
  return (
    <ToastContainer>
      <ToastText>Copied code example</ToastText>
    </ToastContainer>
  );
};

const ToastText = styled.div`
  font-size: 1.25rem;
  font-family: sans-serif;
  font-weight: bold;
  color: #fff;
`;

const ToastContainer = styled.div`
  position: fixed;
  left: 50%;
  bottom: 30px;
  transform: translateX(-50%);
  z-index: 9999;

  background-color: var(--brand-color);
  box-shadow: 0px 3px 6px 0px #828282;
  padding: 20px;
  border-radius: 4px;
  opacity: 0;
  animation: animation 3s cubic-bezier(0.98, 0.01, 0.53, 0.47);

  @keyframes animation {
    0%,
    50% {
      opacity: 1;
    }

    50%,
    100% {
      opacity: 0;
    }
  }
`;

const CopyIconButton = styled.button`
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 0 0 0 4px;
  padding: 8px 8px;
  background-color: #aaa;

  > svg {
    width: 18px;
    height: 18px;
    fill: #2d2d2d;
  }
`;
