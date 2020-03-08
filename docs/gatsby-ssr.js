// const React = require("react");

// exports.onRenderBody = ({ setPostBodyComponents }) => {
//   setPostBodyComponents([
//     <script
//       dangerouslySetInnerHTML={{
//         __html: `
//           window.addEventListener("scroll", function() {
//             console.log("TESTSTSTSTS");
//             const content = document.getElementById("content");
//             const header = document.getElementById("header");
//             const className = "test";
//             const classNames = header.className.split(/\s+/);

//             if (content.scrollTop > 5) {
//               if (classNames.indexOf(className) === -1) {
//                 header.className += " " + className;
//               }
//             } else {
//               const index = classNames.indexOf(className);

//               if (index !== -1) {
//                 classNames.splice(index, 1);
//                 header.className = classNames.join(" ");
//               }
//             }
//           });
//         `,
//       }}
//     />,
//   ]);
// };
