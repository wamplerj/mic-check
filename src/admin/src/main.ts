import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import vuetify from './plugins/vuetify';
import '@mdi/font/css/materialdesignicons.css';
import { useAuthStore } from './stores/auth';
import { useContextStore } from './stores/context';

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(router);
app.use(vuetify);

// Restore persisted auth and context state before the first route navigation
const authStore = useAuthStore();
authStore.loadFromStorage();

const contextStore = useContextStore();
contextStore.loadFromStorage();

app.mount('#app');
