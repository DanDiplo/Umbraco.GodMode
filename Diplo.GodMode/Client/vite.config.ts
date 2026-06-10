import { defineConfig } from "vite";

// Bundle the GodMode backoffice extension as a single ES module that the
// Umbraco 17 backoffice loads via its native import map.
//
// During development the output is written into the *package* project's
// App_Plugins folder. The .csproj packs that folder under
// `staticwebassets/App_Plugins`, so the host project (Diplo.GodMode.Testsite) picks the
// files up automatically through the project reference.
export default defineConfig({
    build: {
        lib: {
            entry: "src/index.ts",
            formats: ["es"],
            fileName: () => "index.js"
        },
        outDir: "../wwwroot/App_Plugins/DiploGodMode",
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            external: [/^@umbraco/],
            output: {
                chunkFileNames: "[name]-[hash].js",
                assetFileNames: "[name]-[hash][extname]"
            }
        }
    },
    base: "/App_Plugins/DiploGodMode/"
});

