(function () {
  "use strict";

  var REPO = "hjsong21/TrayShot";
  var RELEASES_URL = "https://github.com/" + REPO + "/releases";

  var systemLight = window.matchMedia("(prefers-color-scheme: light)");

  /* ----------------------------------------------------
     1. Theme Management (Dark / Light)
  ---------------------------------------------------- */
  var themeToggleBtn = document.getElementById("theme-toggle");

  function getSavedTheme() {
    return localStorage.getItem("trayshot_theme") || (systemLight.matches ? "light" : "dark");
  }

  function applyTheme(theme) {
    document.documentElement.setAttribute("data-theme", theme);
    localStorage.setItem("trayshot_theme", theme);
    if (themeToggleBtn) {
      themeToggleBtn.setAttribute("aria-label", theme === "light" ? t("a11y.theme.toDark") : t("a11y.theme.toLight"));
    }
  }

  if (themeToggleBtn) {
    themeToggleBtn.addEventListener("click", function () {
      var current = document.documentElement.getAttribute("data-theme") || "dark";
      var next = current === "light" ? "dark" : "light";
      applyTheme(next);
    });
  }

  applyTheme(getSavedTheme());

  systemLight.addEventListener("change", function (e) {
    if (!localStorage.getItem("trayshot_theme")) {
      applyTheme(e.matches ? "light" : "dark");
    }
  });

  /* ----------------------------------------------------
     2. Language & i18n
  ---------------------------------------------------- */
  var TABLE = window.TRAYSHOT_I18N || {};
  var currentLang = "ko";

  function getBrowserLang() {
    var navLang = (navigator.language || navigator.userLanguage || "ko").toLowerCase();
    return navLang.startsWith("ko") ? "ko" : "en";
  }

  function t(key, params) {
    var entry = TABLE[key];
    if (!entry) return key;
    var str = entry[currentLang] || entry["en"] || key;
    if (params) {
      Object.keys(params).forEach(function (k) {
        str = str.replace(new RegExp("{" + k + "}", "g"), params[k]);
      });
    }
    return str;
  }

  function applyLanguage(lang) {
    currentLang = lang;
    document.documentElement.setAttribute("lang", lang);
    localStorage.setItem("trayshot_lang", lang);

    // Update buttons state
    document.querySelectorAll(".lang-btn").forEach(function (btn) {
      btn.setAttribute("aria-pressed", btn.getAttribute("data-lang") === lang ? "true" : "false");
    });

    // Update all elements with data-i18n
    document.querySelectorAll("[data-i18n]").forEach(function (el) {
      var key = el.getAttribute("data-i18n");
      var text = t(key);
      if (text) {
        el.textContent = text;
      }
    });

    // Update document title & meta tags
    document.title = t("meta.title");
    var descMeta = document.querySelector('meta[name="description"]');
    if (descMeta) descMeta.setAttribute("content", t("meta.description"));

    // Repaint release download button
    paintDownload();
  }

  document.querySelectorAll(".lang-btn").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var lang = btn.getAttribute("data-lang");
      if (lang) applyLanguage(lang);
    });
  });

  /* ----------------------------------------------------
     3. GitHub Release Info & Stars
  ---------------------------------------------------- */
  var releaseState = { kind: "version", version: "1.1.0" };

  function paintDownload() {
    var label = document.getElementById("download-label");
    if (!label) return;
    if (releaseState.kind === "version") {
      label.textContent = t("hero.download.versioned", { version: releaseState.version });
    } else {
      label.textContent = t("hero.download.fallback");
    }
  }

  function loadReleaseInfo() {
    if (!("fetch" in window)) return;
    fetch("https://api.github.com/repos/" + REPO + "/releases/latest")
      .then(function (res) { return res.ok ? res.json() : null; })
      .then(function (data) {
        if (data && data.tag_name) {
          var version = String(data.tag_name).replace(/^v/, "");
          releaseState = { kind: "version", version: version };
          var downloadBtn = document.getElementById("download-btn");
          if (downloadBtn && data.html_url) {
            downloadBtn.href = data.html_url;
          }
          paintDownload();
        }
      })
      .catch(function () {});
  }

  function loadStarCount() {
    if (!("fetch" in window)) return;
    fetch("https://api.github.com/repos/" + REPO)
      .then(function (res) { return res.ok ? res.json() : null; })
      .then(function (data) {
        if (data && typeof data.stargazers_count === "number") {
          var el = document.getElementById("ghstar-count");
          if (el) {
            el.textContent = String(data.stargazers_count);
            el.hidden = false;
          }
        }
      })
      .catch(function () {});
  }

  // Initial load
  var savedLang = localStorage.getItem("trayshot_lang") || getBrowserLang();
  applyLanguage(savedLang);
  loadReleaseInfo();
  loadStarCount();

})();
