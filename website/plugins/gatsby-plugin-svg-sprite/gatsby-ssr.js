const React = require("react");

const AttachPoint = [
  <svg
    key="sprite"
    id="__SVG_SPRITE_NODE__"
    xmlns="http://www.w3.org/2000/svg"
    xmlnsXlink="http://www.w3.org/1999/xlink"
    style={{ display: "none" }}
    aria-hidden="true"
  />,
];

exports.onRenderBody = ({ setPostBodyComponents }) => {
  setPostBodyComponents(AttachPoint);
};
