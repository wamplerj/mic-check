import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';

// Styles
import '@core/scss/template/index.scss';
import '@layouts/styles/index.scss';
import router from './router';
import installVuetify from './plugins/vuetify';
import { useAuthStore } from './stores/auth';
import { useContextStore } from './stores/context';

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(router);
installVuetify(app);

// Restore persisted auth and context state before the first route navigation
const authStore = useAuthStore();
authStore.loadFromStorage();

const contextStore = useContextStore();
contextStore.loadFromStorage();

app.mount('#app');
