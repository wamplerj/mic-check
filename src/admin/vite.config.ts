import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import vuetify from 'vite-plugin-vuetify';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig({
  plugins: [
    vue(),
    vuetify({ autoImport: true }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  build: {
    outDir: 'dist',
    rollupOptions: {
      output: {
        entryFileNames: 'js/[name].[hash].js',
        chunkFileNames: 'js/[name].[hash].js',
        assetFileNames: ({ name }) => {
          if (name?.match(/\.(woff2?|eot|ttf|otf)$/)) return 'fonts/[name].[hash][extname]';
          if (name?.match(/\.(png|jpe?g|gif|webp|svg)$/)) return 'img/[name].[hash][extname]';
          return 'assets/[name].[hash][extname]';
        },
      },
    },
  },
});
