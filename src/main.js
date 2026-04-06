import { createApp } from "vue";
import App from "@/App.vue";
import vuetify from "@/plugins/vuetify";
import { registerPwaIfSupported } from "@/utils/pwa";
import "@fontsource-variable/plus-jakarta-sans";
import "@mdi/font/css/materialdesignicons.css";
import "vuetify/styles";
import "@/styles/main.css";

createApp(App).use(vuetify).mount("#app");

void registerPwaIfSupported();
