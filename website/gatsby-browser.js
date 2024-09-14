// https://github.com/FormidableLabs/prism-react-renderer/issues/53#issuecomment-546653848
import Prism from "prismjs";
import "./src/style/prism-theme.css";

(typeof global !== "undefined" ? global : window).Prism = Prism;
require("prismjs/components/prism-csharp");
require("prismjs/components/prism-graphql");
require("prismjs/components/prism-json");
require("prismjs/components/prism-bash");
require("prismjs/components/prism-sql");
