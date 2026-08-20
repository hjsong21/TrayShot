(function () {
  "use strict";

  var REPO = "hjsong21/TrayShot";
  var systemLight = window.matchMedia("(prefers-color-scheme: light)");

  /* ---- 1. Theme ---- */
  var themeToggleBtn = document.getElementById("theme-toggle");

  function getSavedTheme() {
    return localStorage.getItem("trayshot_theme") || (systemLight.matches ? "light" : "dark");
  }

  function applyTheme(theme) {
    document.documentElement.setAttribute("data-theme", theme);
    localStorage.setItem("trayshot_theme", theme);
  }

  if (themeToggleBtn) {
    themeToggleBtn.addEventListener("click", function () {
      applyTheme(document.documentElement.getAttribute("data-theme") === "light" ? "dark" : "light");
    });
  }
  applyTheme(getSavedTheme());

  systemLight.addEventListener("change", function (e) {
    if (!localStorage.getItem("trayshot_theme")) applyTheme(e.matches ? "light" : "dark");
  });

  /* ---- 2. i18n ---- */
  var TABLE = window.TRAYSHOT_I18N || {};
  var currentLang = "ko";

  function getBrowserLang() {
    return (navigator.language || "ko").toLowerCase().startsWith("ko") ? "ko" : "en";
  }

  function t(key, params) {
    var entry = TABLE[key];
    if (!entry) return key;
    var str = entry[currentLang] || entry["en"] || key;
    if (params) {
      Object.keys(params).forEach(function (k) {
        str = str.split("{" + k + "}").join(params[k]);
      });
    }
    return str;
  }

  function applyLanguage(lang) {
    currentLang = lang;
    document.documentElement.setAttribute("lang", lang);
    localStorage.setItem("trayshot_lang", lang);

    document.querySelectorAll(".lang-btn").forEach(function (btn) {
      btn.setAttribute("aria-pressed", btn.getAttribute("data-lang") === lang ? "true" : "false");
    });
    document.querySelectorAll("[data-i18n]").forEach(function (el) {
      var text = t(el.getAttribute("data-i18n"));
      if (text) el.textContent = text;
    });

    document.title = t("meta.title");
    var dm = document.querySelector('meta[name="description"]');
    if (dm) dm.setAttribute("content", t("meta.description"));

    paintDownload();
  }

  document.querySelectorAll(".lang-btn").forEach(function (btn) {
    btn.addEventListener("click", function () {
      applyLanguage(btn.getAttribute("data-lang"));
    });
  });

  /* ---- 3. Release & Stars ---- */
  var releaseVersion = "1.1.0";

  function paintDownload() {
    var label = document.getElementById("download-label");
    if (label) label.textContent = t("hero.download.versioned", { version: releaseVersion });
  }

  function loadReleaseInfo() {
    if (!window.fetch) return;
    fetch("https://api.github.com/repos/" + REPO + "/releases/latest")
      .then(function (r) { return r.ok ? r.json() : null; })
      .then(function (d) {
        if (d && d.tag_name) {
          releaseVersion = String(d.tag_name).replace(/^v/, "");
          var btn = document.getElementById("download-btn");
          if (btn && d.html_url) btn.href = d.html_url;
          paintDownload();
        }
      }).catch(function () {});
  }

  function loadStarCount() {
    if (!window.fetch) return;
    fetch("https://api.github.com/repos/" + REPO)
      .then(function (r) { return r.ok ? r.json() : null; })
      .then(function (d) {
        if (d && typeof d.stargazers_count === "number") {
          var el = document.getElementById("ghstar-count");
          if (el) { el.textContent = String(d.stargazers_count); el.hidden = false; }
        }
      }).catch(function () {});
  }

  /* ---- 4. Capture Scene Animation ---- */
  /* This mirrors the exact working pattern from debug_scene.html */

  var sceneRoot = document.querySelector('[data-scene="capture"]');

  if (sceneRoot) {
    var desk     = sceneRoot.querySelector(".scene-desk");
    var cursor   = sceneRoot.querySelector(".cursor");
    var caption  = sceneRoot.querySelector(".caption");
    var replay   = sceneRoot.querySelector(".replay-layer button");
    var sel      = sceneRoot.querySelector(".sel");
    var selSize  = sceneRoot.querySelector(".sel-size");
    var flash    = sceneRoot.querySelector(".flash");
    var qdWidget = sceneRoot.querySelector("#qd-widget");
    var drop     = sceneRoot.querySelector(".win-drop");
    var attach   = sceneRoot.querySelector(".mail-inline-img");
    var payload  = sceneRoot.querySelector(".payload");
    var target   = sceneRoot.querySelector("#capture-target");

    var sceneTimers = [];

    function sceneAt(ms, fn) { sceneTimers.push(setTimeout(fn, ms)); }
    function sceneStop() { sceneTimers.forEach(clearTimeout); sceneTimers = []; }

    function boxOf(el) {
      if (!el || !desk) return { x: 0, y: 0, w: 0, h: 0 };
      var o = desk.getBoundingClientRect();
      var b = el.getBoundingClientRect();
      return { x: b.left - o.left, y: b.top - o.top, w: b.width, h: b.height };
    }

    function centerOf(el) {
      var b = boxOf(el);
      return { x: b.x + b.w / 2, y: b.y + b.h / 2 };
    }

    function place(x, y) {
      cursor.style.transition = "none";
      cursor.style.transform = "translate(" + x + "px," + y + "px)";
      cursor.offsetHeight; // force reflow
    }

    function move(x, y, ms) {
      cursor.style.transition = "transform " + ms + "ms cubic-bezier(0.42,0,0.24,1), opacity 0.25s ease";
      cursor.style.transform = "translate(" + x + "px," + y + "px)";
    }

    function moveToEl(el, ms) {
      var p = centerOf(el);
      move(p.x, p.y, ms);
    }

    function reset() {
      sceneStop();
      cursor.className = "cursor";
      cursor.setAttribute("data-mode", "cross");
      place(40, 60);

      sel.className = "sel";
      sel.style.cssText = "position:absolute; transition:none; width:0; height:0;";

      flash.className = "flash";
      payload.className = "payload";
      attach.className = "mail-inline-img";
      drop.className = "win-drop";

      qdWidget.className = "qd-overlay";
      qdWidget.id = "qd-widget";

      caption.textContent = t("scene.caption.idle");
      sceneRoot.classList.remove("done");
    }

    function playScene() {
      reset();
      console.log("[TrayShot] scene animation starting");

      var tb = boxOf(target);
      var region = { x: tb.x - 3, y: tb.y - 3, w: tb.w + 6, h: tb.h + 6 };
      sel.style.left = region.x + "px";
      sel.style.top = region.y + "px";

      // Step 1: Crosshair cursor appears
      sceneAt(400, function () {
        cursor.classList.add("on");
        caption.textContent = t("capture.caption.1");
      });

      sceneAt(800, function () {
        move(region.x, region.y, 600);
      });

      // Step 2: Drag selection
      sceneAt(1500, function () {
        sel.classList.add("on");
        sel.style.transition = "width 1400ms cubic-bezier(0.42,0,0.24,1), height 1400ms cubic-bezier(0.42,0,0.24,1)";
        sel.style.width = region.w + "px";
        sel.style.height = region.h + "px";
        selSize.textContent = Math.round(region.w) + " \u00d7 " + Math.round(region.h);
        move(region.x + region.w, region.y + region.h, 1400);
      });

      // Step 3: Flash capture
      sceneAt(3000, function () {
        sel.classList.remove("on");
        flash.classList.add("fire");
        caption.textContent = t("capture.caption.2");
      });

      // Step 4: QD widget appears
      sceneAt(3300, function () {
        flash.classList.remove("fire");
        qdWidget.classList.add("shown");
        caption.textContent = t("capture.caption.3");
      });

      // Step 5: Cursor moves to QD widget
      sceneAt(3900, function () {
        cursor.setAttribute("data-mode", "arrow");
        moveToEl(qdWidget, 1600);
      });

      sceneAt(5600, function () {
        caption.textContent = t("capture.caption.4");
      });

      // Step 6: Press & lift
      sceneAt(6400, function () {
        cursor.classList.add("press");
        payload.classList.add("lift");
        qdWidget.classList.add("lifted");
        caption.textContent = t("capture.caption.5");
      });

      // Step 7: Drag to Gmail
      sceneAt(6800, function () {
        moveToEl(drop, 1800);
      });

      sceneAt(7800, function () {
        drop.classList.add("over");
      });

      // Step 8: Drop
      sceneAt(8700, function () {
        cursor.classList.remove("press");
        payload.classList.add("drop");
        drop.classList.remove("over");
        drop.classList.add("dropped");
        attach.classList.add("shown");
        qdWidget.classList.remove("shown");
        qdWidget.classList.remove("lifted");
        caption.textContent = t("capture.caption.6");
      });

      sceneAt(9400, function () {
        var here = centerOf(drop);
        move(here.x + 80, here.y + 60, 800);
        cursor.classList.remove("on");
      });

      sceneAt(10500, function () {
        console.log("[TrayShot] scene animation complete, looping in 3s");
        sceneRoot.classList.add("done");
        sceneAt(3000, playScene);
      });
    }

    // Wire up replay button
    if (replay) {
      replay.addEventListener("click", function (e) {
        e.stopPropagation();
        playScene();
      });
    }

    // Click anywhere on scene to replay
    sceneRoot.addEventListener("click", function () { playScene(); });

    // Auto-start after 300ms (matches debug_scene.html)
    console.log("[TrayShot] scene elements found, scheduling play in 300ms");
    setTimeout(playScene, 300);

  } else {
    console.warn("[TrayShot] scene[data-scene=capture] not found in DOM");
  }

  /* ---- 5. Init ---- */
  applyLanguage(localStorage.getItem("trayshot_lang") || getBrowserLang());
  loadReleaseInfo();
  loadStarCount();

})();
