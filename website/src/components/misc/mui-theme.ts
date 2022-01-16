import { createTheme, Theme } from "@mui/material";

export const MUI_THEME: Theme = createTheme({
  typography: {
    htmlFontSize: 12,
    fontFamily: [
      "system",
      "BlinkMacSystemFont",
      "Segoe UI",
      "Helvetica",
      "Arial",
      "sans-serif",
      "Apple Color Emoji",
      "Segoe UI Emoji",
      "Segoe UI Symbol",
    ].join(","),
  },
  /*components: {
    MuiAutocomplete: {
      styleOverrides: {
        root: {
          flex: "1 1 auto !important",
          "& .MuiAutocomplete-endAdornment": {
            right: "1px !important",
          },
          "& .MuiAutocomplete-clearIndicator": {
            display: "flex !important",
            flex: "0 0 28px !important",
            alignItems: "center !important",
            justifyContent: "center !important",
            marginLeft: "4px !important",
            width: "28px !important",
            height: "28px !important",
            borderRadius: "8px !important",
            backgroundColor: `${THEME_COLORS.components.buttonIcon} !important`,
            transition:
              "background-color 0.2s ease, opacity 0.2s ease !important",
            "& > svg": {
              fill: `${THEME_COLORS.components.buttonIconText} !important`,
              transition: "fill 0.2s ease !important",
            },
          },
          "& .MuiAutocomplete-clearIndicator:hover": {
            backgroundColor: `${THEME_COLORS.components.buttonIconHover} !important`,
            "& > svg": {
              fill: `${THEME_COLORS.components.buttonIconHoverText} !important`,
            },
          },
        },
      },
    },
    MuiButton: {
      defaultProps: {
        size: "small",
      },
      styleOverrides: {
        root: {
          borderRadius: "8px",
          textTransform: "none",
        },
      },
    },
    MuiFab: {
      defaultProps: {
        size: "small",
      },
    },
    MuiFilledInput: {
      defaultProps: {
        margin: "dense",
      },
    },
    MuiFormControl: {
      defaultProps: {
        margin: "dense",
      },
    },
    MuiFormHelperText: {
      defaultProps: {
        margin: "dense",
      },
    },
    MuiIconButton: {
      defaultProps: {
        size: "small",
      },
    },
    MuiInputBase: {
      defaultProps: {
        margin: "dense",
      },
      styleOverrides: {
        root: {
          borderRadius: "8px !important",
          padding: "0 !important",
          fontSize: "1em !important",
          backgroundColor: `${THEME_COLORS.components.field} !important`,
          color: `${THEME_COLORS.components.fieldText} !important`,
          transition:
            "background-color 0.2s ease, border-color 0.2s ease, color 0.2s ease, opacity 0.2s ease !important",
          "&:hover, &.Mui-focused": {
            backgroundColor: `${THEME_COLORS.components.fieldHover} !important`,
            color: `${THEME_COLORS.components.fieldHoverText} !important`,
          },
          "&.Mui-error": {
            color: `${THEME_COLORS.states.failure} !important`,
          },
          "& > .MuiInputBase-input": {
            height: "16px !important",
            padding: "6px 8px !important",
          },
          "& > .MuiInputBase-input::placeholder": {
            color: `${THEME_COLORS.components.fieldPlaceholder} !important`,
            transition: "color 0.2s ease !important",
          },
          "&:hover > .MuiInputBase-input::placeholder, &.Mui-focused > .MuiInputBase-input::placeholder":
            {
              color: `${THEME_COLORS.components.fieldHoverPlaceholder} !important`,
            },
          "& > .MuiOutlinedInput-notchedOutline": {
            border: `1px solid ${THEME_COLORS.components.fieldBorder} !important`,
            borderRadius: "8px !important",
          },
          "&.Mui-error > .MuiOutlinedInput-notchedOutline, &.Mui-error:hover > .MuiOutlinedInput-notchedOutline, &.Mui-error.Mui-focused > .MuiOutlinedInput-notchedOutline":
            {
              borderColor: `${THEME_COLORS.states.failure} !important`,
              color: `${THEME_COLORS.states.failure} !important`,
            },
          "&:hover > .MuiOutlinedInput-notchedOutline, &.Mui-focused > .MuiOutlinedInput-notchedOutline":
            {
              border: `1px solid ${THEME_COLORS.components.fieldHoverBorder} !important`,
            },
        },
      },
    },
    MuiInputLabel: {
      defaultProps: {
        margin: "dense",
      },
    },
    MuiLink: {
      styleOverrides: {
        root: {
          cursor: "pointer",
          color: THEME_COLORS.components.linkText,
          textDecorationColor: THEME_COLORS.components.linkText,
          "&:hover": {
            color: THEME_COLORS.components.linkHoverText,
            textDecorationColor: THEME_COLORS.components.linkHoverText,
          },
        },
      },
    },
    MuiListItem: {
      defaultProps: {
        dense: true,
      },
    },
    MuiOutlinedInput: {
      defaultProps: {
        margin: "dense",
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          padding: "8px 12px !important",
          "&.MuiPaper-rounded": {
            borderRadius: "8px !important",
          },
          "&.MuiAutocomplete-paper": {
            margin: "4px !important",
            padding: "0 !important",
            boxShadow: `0 1px 4px 0 ${THEME_COLORS.base.shadow} !important`,
          },
          "&.MuiAlert-root": {
            margin: "6px 0 !important",
          },
          "& > .MuiAlert-message": {
            padding: "0 !important",
            fontSize: "0.857em !important",
          },
          "& > .MuiAutocomplete-listbox": {
            padding: "4px !important",
            backgroundColor: `${THEME_COLORS.base.background} !important`,
          },
          "& > .MuiAutocomplete-listbox > .MuiAutocomplete-option": {
            borderRadius: "4px !important",
            padding: "6px 12px !important",
            fontSize: "0.750em !important",
            backgroundColor: `${THEME_COLORS.base.item} !important`,
            color: `${THEME_COLORS.base.itemText} !important`,
          },
          "& > .MuiAutocomplete-listbox > .MuiAutocomplete-option:hover, & > .MuiAutocomplete-listbox > .MuiAutocomplete-option.Mui-focused":
            {
              backgroundColor: `${THEME_COLORS.base.itemHover} !important`,
              color: `${THEME_COLORS.base.itemHoverText} !important`,
            },
        },
      },
    },
    MuiSwitch: {
      defaultProps: {
        size: "small",
      },
    },
    MuiTab: {
      defaultProps: {
        disableRipple: true,
      },
      styleOverrides: {
        root: {
          borderTopLeftRadius: "8px",
          borderTopRightRadius: "8px",
          paddingTop: 0,
          paddingRight: "8px",
          paddingBottom: 0,
          paddingLeft: "8px",
          minWidth: "initial",
          minHeight: "30px",
          fontWeight: "bold",
          textTransform: "none",
          "&": {
            color: THEME_COLORS.base.tabText,
          },
          "&:hover": {
            color: THEME_COLORS.base.tabHoverText,
          },
          "&.Mui-selected": {
            color: THEME_COLORS.base.tabActiveText,
          },
          "&.Mui-selected:hover": {
            color: THEME_COLORS.base.tabActiveText,
          },
        },
      },
    },
    MuiTable: {
      defaultProps: {
        size: "small",
      },
    },
    MuiTabs: {
      styleOverrides: {
        root: {
          minHeight: "40px",
          "& .MuiTabs-flexContainer": {
            overflow: "unset",
          },
          "& .MuiTabs-indicator": {
            backgroundColor: THEME_COLORS.base.tabActiveText,
          },
          "& button": {
            minHeight: "40px",
            fontSize: "1em",
          },
        },
      },
    },
    MuiTextField: {
      defaultProps: {
        margin: "dense",
      },
      styleOverrides: {
        root: {
          margin: 0,
        },
      },
    },
    MuiTooltip: {
      styleOverrides: {
        tooltip: {
          margin: "4px !important",
          backgroundColor: `${THEME_COLORS.components.tooltip} !important`,
          boxShadow: `0 1px 4px 0 ${THEME_COLORS.base.shadow} !important`,
          color: `${THEME_COLORS.components.tooltipText} !important`,
        },
      },
    },
    MuiToolbar: {
      defaultProps: {
        variant: "dense",
      },
      styleOverrides: {
        root: {
          width: "100%",
          height: "40px",
          padding: "0px !important",
        },
      },
    },
  },*/
});
