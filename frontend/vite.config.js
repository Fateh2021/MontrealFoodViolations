import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

export default defineConfig({
  plugins: [vue()],
  base: './',
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5067'
    }
  },
  build: {
    outDir: '../src/MontrealFoodViolations.Api/wwwroot',
    emptyOutDir: true,
    assetsDir: 'assets'
  }
});
