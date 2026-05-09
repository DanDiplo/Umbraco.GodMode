import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      entry: "src/index.ts",
      formats: ["es"],
      fileName: () => "index.js"
    },
    outDir: "../wwwroot/App_Plugins/DiploGodModeAI",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
      output: {
        chunkFileNames: "[name].js",
        assetFileNames: "[name][extname]"
      }
    }
  },
  base: "/App_Plugins/DiploGodModeAI/"
});
