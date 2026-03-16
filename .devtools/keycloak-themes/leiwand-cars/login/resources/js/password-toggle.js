(function () {
  function initPasswordToggles() {
    var toggles = document.querySelectorAll("[data-password-toggle]");

    toggles.forEach(function (toggle) {
      toggle.addEventListener("click", function () {
        var targetId = toggle.getAttribute("data-target");
        if (!targetId) return;

        var input = document.getElementById(targetId);
        if (!input) return;

        var isHidden = input.getAttribute("type") === "password";
        input.setAttribute("type", isHidden ? "text" : "password");
        toggle.classList.toggle("is-visible", isHidden);
        toggle.setAttribute(
          "aria-label",
          isHidden ? "Hide password" : "Show password",
        );
      });
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initPasswordToggles);
  } else {
    initPasswordToggles();
  }
})();
