import { fileURLToPath } from "node:url";
import { crx } from "@crxjs/vite-plugin";
import { defineConfig } from "vite";
import manifest from "./manifest.json";

export default defineConfig({
  root: fileURLToPath(new URL(".", import.meta.url)),
  plugins: [crx({ manifest })],
  build: {
    outDir: "../../dist",
    emptyOutDir: true
  }
});
