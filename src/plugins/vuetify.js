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
          "on-primary": "#f8fbff",
          secondary: "#1f9d8b",
          "on-secondary": "#f5fffc",
          tertiary: "#d9a441",
          "on-tertiary": "#fff9ef",
          error: "#c85c5c",
          "on-error": "#fff7f7",
          success: "#208f7a",
          "on-success": "#f4fffb",
          warning: "#d9a441",
          "on-warning": "#fff9ef",
          background: "#f4f6f9",
          surface: "#ffffff",
          "surface-variant": "#ebf0f6",
          "on-surface-variant": "#5a6472",
        },
      },
      dark: {
        colors: {
          primary: "#4b5563",
          "on-primary": "#f7f9fc",
          secondary: "#5b6472",
          "on-secondary": "#f7f9fc",
          tertiary: "#736a59",
          "on-tertiary": "#fdf8ef",
          error: "#916868",
          "on-error": "#fff8f8",
          success: "#5d726a",
          "on-success": "#f5fffb",
          warning: "#7a705d",
          "on-warning": "#fffaf0",
          background: "#10151c",
          surface: "#171d24",
          "surface-variant": "#232c37",
          "on-surface-variant": "#d6dde5",
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
