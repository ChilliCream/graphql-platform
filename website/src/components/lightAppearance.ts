/** Story `beforeEach` hook that renders the story under the Light appearance. */
export function lightAppearance() {
  const previous = document.documentElement.getAttribute("data-theme");
  document.documentElement.dataset.theme = "light";
  return () => {
    if (previous === null) {
      document.documentElement.removeAttribute("data-theme");
    } else {
      document.documentElement.setAttribute("data-theme", previous);
    }
  };
}
