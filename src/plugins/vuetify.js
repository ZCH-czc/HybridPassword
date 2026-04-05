import { createVuetify } from "vuetify";
import * as components from "vuetify/components";
import * as directives from "vuetify/directives";
import { zhHans } from "vuetify/locale";

export default createVuetify({
  components,
  directives,
  locale: {
    locale: "zhHans",
    messages: { zhHans },
  },
  theme: {
    defaultTheme: "light",
    themes: {
      light: {
        colors: {
          primary: "#1a73e8",
          secondary: "#d2e3fc",
          error: "#d93025",
          success: "#188038",
          warning: "#f9ab00",
          background: "#f6f8fc",
          surface: "#ffffff",
          "surface-variant": "#eef3fd",
          "on-surface-variant": "#3c4043",
        },
      },
      dark: {
        colors: {
          primary: "#8ab4f8",
          secondary: "#273244",
          error: "#f28b82",
          success: "#81c995",
          warning: "#fdd663",
          background: "#0f141c",
          surface: "#171c24",
          "surface-variant": "#1f2631",
          "on-surface-variant": "#d3d9e3",
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
      style: "text-transform: none;",
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
