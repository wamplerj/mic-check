# MicCheck Admin SPA — Implementation Output

All source files have been created under `src/admin/`. Node.js/npm is not installed on this machine, so the build and tests could not be run. See setup instructions below.

## Getting Started

```bash
# Install Node.js (via nvm is recommended)
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.7/install.sh | bash
source ~/.bashrc
nvm install --lts

# Then from the admin directory:
cd /home/james/src/mic-check/src/admin
npm install
npm run build   # produces dist/
npm test        # runs 6 Jest tests
```

Or install via your package manager:
```bash
sudo apt install nodejs npm   # Ubuntu/Debian
```

---

## Files Created

### Config files (`src/admin/`)
- `package.json` — scripts: `build`, `serve`, `test`; all deps pinned
- `webpack.config.js` — vue-loader, ts-loader, sass-loader, VuetifyPlugin, HtmlWebpackPlugin
- `tsconfig.json`, `jest.config.js`, `babel.config.js`

### Application (`src/admin/src/`)
- `main.ts` / `App.vue` — entry point
- `plugins/vuetify.ts` — Vuetify 3 with MDI icons + light theme
- `router/index.ts` — routes for `/`, `/features`, `/environments`, `/projects`, `/profile`, `/settings`
- `assets/logo.svg` — vector microphone on stand with sound-wave arcs
- `components/AppHeader.vue` — logo + nav links (center) + John Doe profile + gear (right)
- `components/AppLayout.vue` — collapsible side drawer + `v-main` content area
- `views/` — 6 stub pages (Dashboard, Features, Environments, Projects, Profile, Settings)

### Tests (`src/admin/tests/`)
- `setup.ts` — Vuetify global registration + ResizeObserver stub for jsdom
- `__mocks__/styleMock.js` — stubs CSS/SCSS imports in Jest
- `__mocks__/fileMock.js` — stubs SVG/image imports in Jest
- `unit/AppHeader.spec.ts` — 6 tests covering all acceptance criteria

---

## Stack

| Concern | Technology |
|---|---|
| Framework | Vue 3 + TypeScript |
| UI components | Vuetify 3 |
| Bundler | webpack 5 |
| Tests | Jest 29 + @vue/test-utils 2 |
| Routing | vue-router 4 |
| Icons | @mdi/font (Material Design Icons) |

---

## npm Commands

| Command | Purpose |
|---|---|
| `npm install` | Install all dependencies |
| `npm run build` | Production webpack bundle → `dist/` |
| `npm run serve` | Dev server on :8080 with HMR |
| `npm test` | Run all Jest unit tests |
