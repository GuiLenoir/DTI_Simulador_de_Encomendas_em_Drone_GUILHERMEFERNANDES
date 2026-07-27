import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: "./src/test/setup.ts",
    coverage: {
      reporter: ["text", "html"],
      exclude: [
        "dist/**",
        "src/main.tsx",
        "src/vite-env.d.ts",
        "vite.config.ts"
      ]
    }
  }
});
