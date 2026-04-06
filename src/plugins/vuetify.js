import { createVuetify } from "vuetify";
import * as components from "vuetify/components";
import * as directives from "vuetify/directives";
import { en, zhHans } from "vuetify/locale";

export default createVuetify({
  components,
  directives,
  locale: {
    locale: "zhHans",
    fallback: "en",
    messages: { zhHans, en },
  },
  theme: {
    defaultTheme: "light",
    themes: {
      light: {
        colors: {
          primary: "#2f6fed",
          secondary: "#1f9d8b",
          tertiary: "#d9a441",
          error: "#c85c5c",
          success: "#208f7a",
          warning: "#d9a441",
          background: "#efebe4",
          surface: "#ffffff",
          "surface-variant": "#e8edf1",
          "on-surface-variant": "#4f5966",
        },
      },
      dark: {
        colors: {
          primary: "#8eaefc",
          secondary: "#55b8ab",
          tertiary: "#e0b865",
          error: "#f08b8b",
          success: "#68c5b1",
          warning: "#e0b865",
          background: "#0d1115",
          surface: "#171f27",
          "surface-variant": "#202a34",
          "on-surface-variant": "#d2d8df",
        },
      },
    },
  },
  defaults: {
    VAppBar: {
      elevation: 0,
      color: "transparent",
    },
    VTextField: {
      variant: "solo-filled",
      density: "comfortable",
      flat: true,
    },
    VTextarea: {
      variant: "solo-filled",
      density: "comfortable",
      flat: true,
    },
    VBtn: {
      rounded: "xl",
      style: "text-transform: none; letter-spacing: 0;",
    },
    VCard: {
      rounded: "xl",
      elevation: 0,
    },
    VSheet: {
      rounded: "xl",
    },
    VDialog: {
      rounded: "xl",
    },
    VChip: {
      rounded: "xl",
    },
  },
});
