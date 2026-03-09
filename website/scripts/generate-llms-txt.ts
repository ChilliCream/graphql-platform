import fs from "fs";
import path from "path";
import {
  generateLlmsTxt,
  generateLlmsFullTxt,
} from "../lib/llms-txt-generator";

const outDir = path.resolve(__dirname, "../out");

if (!fs.existsSync(outDir)) {
  console.error(`Output directory not found: ${outDir}`);
  console.error("Run 'next build' first to generate the output directory.");
  process.exit(1);
}

console.log("Generating llms.txt...");
const llmsTxt = generateLlmsTxt();
fs.writeFileSync(path.join(outDir, "llms.txt"), llmsTxt, "utf-8");
console.log(
  `  Written: out/llms.txt (${(llmsTxt.length / 1024).toFixed(1)} KB)`
);

console.log("Generating llms-full.txt...");
const llmsFullTxt = generateLlmsFullTxt();
fs.writeFileSync(path.join(outDir, "llms-full.txt"), llmsFullTxt, "utf-8");
console.log(
  `  Written: out/llms-full.txt (${(llmsFullTxt.length / 1024).toFixed(1)} KB)`
);

console.log("Done.");
