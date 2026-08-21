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
    document.querySelectorAll("[data-i18n-html]").forEach(function (el) {
      var html = t(el.getAttribute("data-i18n-html"));
      if (html) el.innerHTML = html;
    });
    document.querySelectorAll("[data-i18n-aria]").forEach(function (el) {
      var text = t(el.getAttribute("data-i18n-aria"));
      if (text) el.setAttribute("aria-label", text);
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

  /* ---- 4-2. Gallery Scene Animation ---- */
  var sceneRootGallery = document.querySelector('[data-scene="gallery"]');

  if (sceneRootGallery) {
    var galDesk       = sceneRootGallery.querySelector(".scene-desk-gallery");
    var winGallery    = sceneRootGallery.querySelector("#win-gallery");
    var winPreview    = sceneRootGallery.querySelector("#win-preview");
    var cursorGal     = sceneRootGallery.querySelector("#cursor-gallery");
    var captionGal    = sceneRootGallery.querySelector(".caption-gallery");
    var replayGal     = sceneRootGallery.querySelector(".replay-gallery button");
    var previewTitle  = sceneRootGallery.querySelector("#preview-title");
    var activeKeyBadge= sceneRootGallery.querySelector("#active-key-badge");
    var closePreviewBtn = sceneRootGallery.querySelector(".preview-close-btn");

    var itemDashboard = sceneRootGallery.querySelector("#gitem-dashboard");
    var itemFigma     = sceneRootGallery.querySelector("#gitem-figma");
    var itemSheets    = sceneRootGallery.querySelector("#gitem-sheets");
    var itemCode      = sceneRootGallery.querySelector("#gitem-code");
    var itemVideo     = sceneRootGallery.querySelector("#gitem-video");
    var itemChat      = sceneRootGallery.querySelector("#gitem-chat");
    var allItems      = sceneRootGallery.querySelectorAll(".gallery-item");

    var pslide1       = sceneRootGallery.querySelector("#pslide-1");
    var pslide2       = sceneRootGallery.querySelector("#pslide-2");
    var pslide3       = sceneRootGallery.querySelector("#pslide-3");

    var galTimers = [];

    function galAt(ms, fn) { galTimers.push(setTimeout(fn, ms)); }
    function galStop() { galTimers.forEach(clearTimeout); galTimers = []; }

    function galBoxOf(el) {
      if (!el || !galDesk) return { x: 0, y: 0, w: 0, h: 0 };
      var o = galDesk.getBoundingClientRect();
      var b = el.getBoundingClientRect();
      return { x: b.left - o.left, y: b.top - o.top, w: b.width, h: b.height };
    }

    function galCenterOf(el) {
      var b = galBoxOf(el);
      return { x: b.x + b.w / 2, y: b.y + b.h / 2 };
    }

    function galPlace(x, y) {
      if (!cursorGal) return;
      cursorGal.style.transition = "none";
      cursorGal.style.transform = "translate(" + x + "px," + y + "px)";
      cursorGal.offsetHeight;
    }

    function galMove(x, y, ms) {
      if (!cursorGal) return;
      cursorGal.style.transition = "transform " + ms + "ms cubic-bezier(0.42,0,0.24,1), opacity 0.25s ease";
      cursorGal.style.transform = "translate(" + x + "px," + y + "px)";
    }

    function galMoveToEl(el, ms) {
      var p = galCenterOf(el);
      galMove(p.x, p.y, ms);
    }

    function showSlide(idx) {
      [pslide1, pslide2, pslide3].forEach(function (s, i) {
        if (!s) return;
        if (i === idx) s.classList.add("active");
        else s.classList.remove("active");
      });
      allItems.forEach(function (it) { it.classList.remove("focused"); });

      if (idx === 0) {
        if (previewTitle) previewTitle.textContent = "스크린샷 2026-08-18 232922.webp (2 / 9)";
        if (itemDashboard) itemDashboard.classList.add("focused");
      } else if (idx === 1) {
        if (previewTitle) previewTitle.textContent = "스크린샷 2026-08-18 233247.webp (3 / 9)";
        if (itemFigma) itemFigma.classList.add("focused");
      } else if (idx === 2) {
        if (previewTitle) previewTitle.textContent = "스크린샷 2026-08-18 233510.webp (4 / 9)";
        if (itemCode) itemCode.classList.add("focused");
      }
    }

    function showKeyBadge(keyText) {
      if (!activeKeyBadge) return;
      activeKeyBadge.textContent = keyText;
      activeKeyBadge.style.animation = "none";
      activeKeyBadge.offsetHeight;
      activeKeyBadge.style.animation = "pulse-key 0.4s ease";
    }

    function resetGallery() {
      galStop();
      if (cursorGal) {
        cursorGal.className = "cursor cursor-gallery";
        cursorGal.setAttribute("data-mode", "arrow");
        galPlace(520, 360);
      }

      if (winGallery) winGallery.classList.remove("shown");
      if (winPreview) winPreview.classList.remove("shown");

      allItems.forEach(function (it) { it.classList.remove("focused"); });
      showSlide(0);

      if (captionGal) captionGal.textContent = t("scene.caption.idle");
      sceneRootGallery.classList.remove("done");
    }

    function playGallery() {
      resetGallery();
      console.log("[TrayShot] gallery scene animation starting");

      // Step 1: Spotlight gallery summons from tray
      galAt(500, function () {
        if (winGallery) winGallery.classList.add("shown");
        if (captionGal) captionGal.textContent = t("gallery.caption.1");
      });

      // Step 2: Cursor enters & moves to Dashboard KPI capture card
      galAt(1300, function () {
        if (cursorGal) cursorGal.classList.add("on");
        if (itemDashboard) galMoveToEl(itemDashboard, 1000);
      });

      // Step 3: Selection focus & Spacebar press -> QuickLook Preview pops up
      galAt(2500, function () {
        if (itemDashboard) itemDashboard.classList.add("focused");
      });

      galAt(3200, function () {
        if (cursorGal) cursorGal.classList.remove("on");
        showKeyBadge("Space");
        showSlide(0);
        if (winPreview) winPreview.classList.add("shown");
        if (captionGal) captionGal.textContent = t("gallery.caption.2");
      });

      // Step 4: Arrow Right -> Navigate to Figma Design capture
      galAt(5200, function () {
        showKeyBadge("→");
        showSlide(1);
        if (captionGal) captionGal.textContent = t("gallery.caption.3");
      });

      // Step 5: Arrow Right -> Navigate to VS Code IDE capture
      galAt(7000, function () {
        showKeyBadge("→");
        showSlide(2);
        if (captionGal) captionGal.textContent = t("gallery.caption.4");
      });

      // Step 6: Press Esc -> Preview dismisses
      galAt(8800, function () {
        showKeyBadge("Esc");
        if (winPreview) winPreview.classList.remove("shown");
        if (captionGal) captionGal.textContent = t("gallery.caption.5");
      });

      // Step 7: Gallery dismisses & safe management note
      galAt(10200, function () {
        if (winGallery) winGallery.classList.remove("shown");
        if (cursorGal) cursorGal.classList.remove("on");
        if (captionGal) captionGal.textContent = t("gallery.caption.6");
      });

      // Step 8: Loop
      galAt(12000, function () {
        console.log("[TrayShot] gallery scene animation complete, looping in 3s");
        sceneRootGallery.classList.add("done");
        galAt(3000, playGallery);
      });
    }

    // Replay button handler
    if (replayGal) {
      replayGal.addEventListener("click", function (e) {
        e.stopPropagation();
        playGallery();
      });
    }

    // Click scene to replay
    sceneRootGallery.addEventListener("click", function () { playGallery(); });

    // Interactive controls when clicked directly
    if (closePreviewBtn) {
      closePreviewBtn.addEventListener("click", function (e) {
        e.stopPropagation();
        if (winPreview) winPreview.classList.remove("shown");
      });
    }

    if (itemDashboard) {
      itemDashboard.addEventListener("click", function (e) {
        e.stopPropagation();
        showSlide(0);
        if (winPreview) winPreview.classList.add("shown");
      });
    }
    if (itemFigma) {
      itemFigma.addEventListener("click", function (e) {
        e.stopPropagation();
        showSlide(1);
        if (winPreview) winPreview.classList.add("shown");
      });
    }
    if (itemCode) {
      itemCode.addEventListener("click", function (e) {
        e.stopPropagation();
        showSlide(2);
        if (winPreview) winPreview.classList.add("shown");
      });
    }

    // Auto-start gallery scene
    console.log("[TrayShot] gallery scene elements found, scheduling play in 500ms");
    setTimeout(playGallery, 500);
  } else {
    console.warn("[TrayShot] scene[data-scene=gallery] not found in DOM");
  }

  /* =========================================================================
     3. Interactive Scene 3: Automatic WebP & Contextual JPG Format Conversion
  ========================================================================= */
  var sceneRootConvert = document.querySelector(".scene[data-scene='convert']");
  if (sceneRootConvert) {
    var deskConvert = sceneRootConvert.querySelector(".scene-desk-convert");
    var winBrowserNews = document.getElementById("win-browser-news");
    var newsTargetBox = document.getElementById("news-target-box");
    var selConvert = document.getElementById("sel-convert");
    var flashConvert = document.getElementById("flash-convert");
    var qdWidgetConvert = document.getElementById("qd-widget-convert");
    var winGalleryConvert = document.getElementById("win-gallery-convert");
    var gitemHynix = document.getElementById("gitem-hynix");
    var badgeHynix = document.getElementById("badge-hynix");
    var gitemHynixJpg = document.getElementById("gitem-hynix-jpg");
    var galleryToastMsg = document.getElementById("gallery-toast-msg");
    var galleryToastText = document.getElementById("gallery-toast-text");
    var thumbContextMenu = document.getElementById("thumb-context-menu");
    var ctxRowFormat = document.getElementById("ctx-row-format");
    var subItemJpg = document.getElementById("sub-item-jpg");
    var cursorConvert = document.getElementById("cursor-convert");
    var captionConvert = sceneRootConvert.querySelector(".caption-convert");
    var replayBtnConvert = sceneRootConvert.querySelector(".replay-convert button");

    var pillWebp = document.getElementById("pill-webp");
    var pillJpg = document.getElementById("pill-jpg");

    var convertTimers = [];
    function convertAt(ms, fn) {
      var id = setTimeout(fn, ms);
      convertTimers.push(id);
      return id;
    }
    function convertStop() {
      convertTimers.forEach(clearTimeout);
      convertTimers = [];
    }

    function convertBoxOf(el) {
      if (!el || !deskConvert) return { x: 0, y: 0, w: 0, h: 0 };
      var r = el.getBoundingClientRect();
      var c = deskConvert.getBoundingClientRect();
      return { x: r.left - c.left, y: r.top - c.top, w: r.width, h: r.height };
    }
    function convertCenterOf(el) {
      var b = convertBoxOf(el);
      return { x: b.x + b.w / 2, y: b.y + b.h / 2 };
    }
    function convertPlace(x, y) {
      if (!cursorConvert) return;
      cursorConvert.style.transition = "none";
      cursorConvert.style.transform = "translate(" + x + "px," + y + "px)";
      cursorConvert.offsetHeight;
    }
    function convertMove(x, y, ms) {
      if (!cursorConvert) return;
      cursorConvert.style.transition = "transform " + ms + "ms cubic-bezier(0.42,0,0.24,1), opacity 0.25s ease";
      cursorConvert.style.transform = "translate(" + x + "px," + y + "px)";
    }
    function convertMoveToEl(el, ms) {
      var p = convertCenterOf(el);
      convertMove(p.x, p.y, ms);
    }

    function setActivePillConvert(step) {
      if (pillWebp) pillWebp.classList.toggle("active", step === "webp");
      if (pillJpg) pillJpg.classList.toggle("active", step === "jpg");
    }

    function resetConvertScene() {
      convertStop();
      if (cursorConvert) {
        cursorConvert.className = "cursor cursor-convert";
        cursorConvert.setAttribute("data-mode", "arrow");
        convertPlace(120, 200);
      }

      if (selConvert) {
        selConvert.classList.remove("on");
        selConvert.style.transition = "none";
        selConvert.style.width = "0px";
        selConvert.style.height = "0px";
        selConvert.style.opacity = "";
      }
      if (flashConvert) flashConvert.style.opacity = "0";
      if (qdWidgetConvert) qdWidgetConvert.classList.remove("shown");
      if (winGalleryConvert) winGalleryConvert.classList.add("shown");
      if (gitemHynix) gitemHynix.classList.remove("shown");
      if (badgeHynix) {
        badgeHynix.textContent = "PNG";
        badgeHynix.className = "badge-format badge-png";
      }
      if (gitemHynixJpg) gitemHynixJpg.classList.remove("shown");
      if (galleryToastMsg) galleryToastMsg.classList.remove("shown");
      if (thumbContextMenu) thumbContextMenu.classList.remove("shown");
      if (ctxRowFormat) ctxRowFormat.classList.remove("active-hover");
      if (subItemJpg) subItemJpg.classList.remove("highlight", "clicked");

      if (captionConvert) captionConvert.textContent = t("scene.caption.idle");
      sceneRootConvert.classList.remove("done");
    }

    // ---- Step 1: WebP Auto Conversion Sequence ----
    function playWebpStep(onComplete) {
      resetConvertScene();
      setActivePillConvert("webp");
      console.log("[TrayShot] playing WebP Auto Conversion step");

      var selConvertSize = selConvert ? selConvert.querySelector(".sel-convert-size") : null;
      var tb = convertBoxOf(newsTargetBox);
      var w = (tb.w > 0) ? tb.w : 356;
      var h = (tb.h > 0) ? tb.h : 165;
      var startX = (tb.x > 0) ? (tb.x + 4) : 40;
      var startY = (tb.y > 0) ? (tb.y + 4) : 155;
      var region = { x: startX, y: startY, w: w - 8, h: h - 8 };

      if (selConvert) {
        selConvert.classList.remove("on");
        selConvert.style.transition = "none";
        selConvert.style.left = region.x + "px";
        selConvert.style.top = region.y + "px";
        selConvert.style.width = "0px";
        selConvert.style.height = "0px";
        selConvert.style.opacity = "";
        selConvert.offsetHeight;
      }

      // 1. Snipping crosshair cursor appears and moves to start point
      convertAt(300, function () {
        if (cursorConvert) {
          cursorConvert.classList.add("on");
          cursorConvert.setAttribute("data-mode", "cross");
        }
        convertPlace(region.x - 20, region.y - 20);
        convertMove(region.x, region.y, 500);
        if (captionConvert) captionConvert.textContent = t("convert.caption.webp.1");
      });

      // 2. Drag selection box naturally expands with mouse movement
      convertAt(1000, function () {
        if (selConvert) {
          selConvert.classList.add("on");
          selConvert.style.transition = "width 1300ms cubic-bezier(0.42,0,0.24,1), height 1300ms cubic-bezier(0.42,0,0.24,1)";
          selConvert.style.width = region.w + "px";
          selConvert.style.height = region.h + "px";
          if (selConvertSize) selConvertSize.textContent = Math.round(region.w) + " \u00d7 " + Math.round(region.h);
        }
        convertMove(region.x + region.w, region.y + region.h, 1300);
      });

      // 3. Shutter Flash + QuickDrop Floating Overlay Thumbnail appears
      convertAt(2500, function () {
        if (flashConvert) {
          flashConvert.style.opacity = "0.75";
          setTimeout(function () { flashConvert.style.opacity = "0"; }, 150);
        }
        if (selConvert) {
          selConvert.classList.remove("on");
          selConvert.style.transition = "none";
          selConvert.style.width = "0px";
          selConvert.style.height = "0px";
        }
        if (cursorConvert) {
          cursorConvert.setAttribute("data-mode", "arrow");
          cursorConvert.classList.remove("on");
        }
        if (qdWidgetConvert) qdWidgetConvert.classList.add("shown");
      });

      // 4. Item appears in Gallery as PNG (in 2nd position)
      convertAt(3100, function () {
        if (gitemHynix) gitemHynix.classList.add("shown");
        if (captionConvert) captionConvert.textContent = t("convert.caption.webp.2");
      });

      // 5. Hide QuickDrop Widget
      convertAt(4000, function () {
        if (qdWidgetConvert) qdWidgetConvert.classList.remove("shown");
      });

      // 6. Engine triggers -> PNG turns to WEBP badge + Gallery Toast!
      convertAt(4700, function () {
        if (badgeHynix) {
          badgeHynix.style.transform = "scale(1.25)";
          badgeHynix.textContent = "WEBP";
          badgeHynix.className = "badge-format badge-webp";
          setTimeout(function () { badgeHynix.style.transform = "scale(1)"; }, 200);
        }
        if (galleryToastMsg) {
          if (galleryToastText) galleryToastText.textContent = "무손실 WebP로 자동 변환됨 (-65%)";
          galleryToastMsg.classList.add("shown");
        }
        if (captionConvert) captionConvert.textContent = t("convert.caption.webp.3");
      });

      // 7. Hide gallery toast & finish step
      convertAt(6800, function () {
        if (galleryToastMsg) galleryToastMsg.classList.remove("shown");
      });

      convertAt(7500, function () {
        if (onComplete) {
          onComplete();
        } else {
          sceneRootConvert.classList.add("done");
        }
      });
    }

    // ---- Step 2: Context Menu JPG Conversion Sequence ----
    function playJpgStep(onComplete) {
      resetConvertScene();
      setActivePillConvert("jpg");
      console.log("[TrayShot] playing JPG Context Menu Conversion step");

      // Pre-set WebP item in gallery
      if (gitemHynix) gitemHynix.classList.add("shown");
      if (badgeHynix) {
        badgeHynix.textContent = "WEBP";
        badgeHynix.className = "badge-format badge-webp";
      }

      // 1. Move cursor to thumbnail
      convertAt(400, function () {
        if (cursorConvert) cursorConvert.classList.add("on");
        if (gitemHynix) convertMoveToEl(gitemHynix, 700);
        if (captionConvert) captionConvert.textContent = t("convert.caption.jpg.1");
      });

      // 2. Right-click -> context menu appears
      convertAt(1400, function () {
        if (thumbContextMenu && gitemHynix && winGalleryConvert) {
          var ib = gitemHynix.getBoundingClientRect();
          var gb = winGalleryConvert.getBoundingClientRect();
          thumbContextMenu.style.left = (ib.left - gb.left + 20) + "px";
          thumbContextMenu.style.top = (ib.top - gb.top + 30) + "px";
          thumbContextMenu.classList.add("shown");
        }
      });

      // 3. Move cursor to '포맷 변환' row -> submenu opens
      convertAt(2200, function () {
        if (ctxRowFormat) {
          convertMoveToEl(ctxRowFormat, 600);
          ctxRowFormat.classList.add("active-hover");
        }
        if (captionConvert) captionConvert.textContent = t("convert.caption.jpg.2");
      });

      // 4. Move cursor to 'JPG' sub-item and highlight on hover
      convertAt(3200, function () {
        if (subItemJpg) {
          convertMoveToEl(subItemJpg, 500);
          setTimeout(function () {
            subItemJpg.classList.add("highlight");
          }, 400);
        }
      });

      // 5. Click 'JPG' item
      convertAt(4000, function () {
        if (subItemJpg) subItemJpg.classList.add("clicked");
      });

      // 5. Menu closes -> Toast shows -> New converted JPG item added to gallery!
      convertAt(4400, function () {
        if (thumbContextMenu) thumbContextMenu.classList.remove("shown");
        if (galleryToastMsg) {
          if (galleryToastText) galleryToastText.textContent = "JPG 포맷 변환 완료";
          galleryToastMsg.classList.add("shown");
        }
        if (gitemHynixJpg) gitemHynixJpg.classList.add("shown");
        if (captionConvert) captionConvert.textContent = t("convert.caption.jpg.3");
      });

      // 6. Hide toast & finish
      convertAt(6400, function () {
        if (galleryToastMsg) galleryToastMsg.classList.remove("shown");
        if (cursorConvert) cursorConvert.classList.remove("on");
      });

      convertAt(7200, function () {
        sceneRootConvert.classList.add("done");
        if (onComplete) onComplete();
      });
    }

    // ---- Full Sequential Loop: WebP -> JPG ----
    function playFullConvertSequence() {
      playWebpStep(function () {
        convertAt(1200, function () {
          playJpgStep(function () {
            console.log("[TrayShot] convert scene animation complete, looping in 3s");
            sceneRootConvert.classList.add("done");
            convertAt(3000, playFullConvertSequence);
          });
        });
      });
    }

    // Pill Tab Events
    if (pillWebp) {
      pillWebp.addEventListener("click", function (e) {
        e.stopPropagation();
        playWebpStep(function () {
          convertAt(3000, playFullConvertSequence);
        });
      });
    }
    if (pillJpg) {
      pillJpg.addEventListener("click", function (e) {
        e.stopPropagation();
        playJpgStep(function () {
          convertAt(3000, playFullConvertSequence);
        });
      });
    }

    // Replay button
    if (replayBtnConvert) {
      replayBtnConvert.addEventListener("click", function (e) {
        e.stopPropagation();
        playFullConvertSequence();
      });
    }

    sceneRootConvert.addEventListener("click", function (e) {
      if (e.target.closest(".scene-pills") || e.target.closest(".replay-convert")) return;
      playFullConvertSequence();
    });

    console.log("[TrayShot] convert scene elements found, scheduling play in 600ms");
    setTimeout(playFullConvertSequence, 600);
  }

  /* =========================================================================
     4. Interactive Scene 4: Panel Resizing & Live Theme Switching
  ========================================================================= */
  var sceneRootTheme = document.querySelector(".scene[data-scene='theme']");
  if (sceneRootTheme) {
    var deskTheme = sceneRootTheme.querySelector(".scene-desk-theme");
    var winGalleryTheme = document.getElementById("win-gallery-theme");
    var galleryResizeHandle = document.getElementById("gallery-resize-handle");
    var gearBtnTheme = document.getElementById("gear-btn-theme");
    var galleryCtxMenu = document.getElementById("gallery-ctx-menu");
    var ctxItemSettings = document.getElementById("ctx-item-settings");
    var winPrefModal = document.getElementById("win-pref-modal");
    var prefCloseBtn = document.getElementById("pref-close-btn");
    var themeSelectTrigger = document.getElementById("theme-select-trigger");
    var themeDropdownMenu = document.getElementById("theme-dropdown-menu");
    var optLight = document.getElementById("opt-light");
    var optDark = document.getElementById("opt-dark");
    var themeCurrLabel = document.getElementById("theme-curr-label");
    var cursorTheme = document.getElementById("cursor-theme");
    var captionTheme = sceneRootTheme.querySelector(".caption-theme");
    var replayBtnTheme = sceneRootTheme.querySelector(".replay-theme button");

    var pillSize = document.getElementById("pill-size");
    var pillTheme = document.getElementById("pill-theme");

    var themeTimers = [];
    function themeAt(ms, fn) {
      var id = setTimeout(fn, ms);
      themeTimers.push(id);
      return id;
    }
    function themeStop() {
      themeTimers.forEach(clearTimeout);
      themeTimers = [];
    }

    function themeBoxOf(el) {
      if (!el || !deskTheme) return { x: 0, y: 0, w: 0, h: 0 };
      var r = el.getBoundingClientRect();
      var c = deskTheme.getBoundingClientRect();
      return { x: r.left - c.left, y: r.top - c.top, w: r.width, h: r.height };
    }
    function themeCenterOf(el) {
      var b = themeBoxOf(el);
      return { x: b.x + b.w / 2, y: b.y + b.h / 2 };
    }
    function themePlace(x, y) {
      if (!cursorTheme) return;
      cursorTheme.style.transition = "none";
      cursorTheme.style.transform = "translate(" + x + "px," + y + "px)";
      cursorTheme.offsetHeight;
    }
    function themeMove(x, y, ms) {
      if (!cursorTheme) return;
      cursorTheme.style.transition = "transform " + ms + "ms cubic-bezier(0.42,0,0.24,1), opacity 0.25s ease";
      cursorTheme.style.transform = "translate(" + x + "px," + y + "px)";
    }
    function themeMoveToEl(el, ms) {
      var p = themeCenterOf(el);
      themeMove(p.x, p.y, ms);
    }

    function setActivePill(step) {
      if (pillSize) pillSize.classList.toggle("active", step === "size");
      if (pillTheme) pillTheme.classList.toggle("active", step === "theme");
    }

    function resetThemeScene() {
      themeStop();
      if (cursorTheme) {
        cursorTheme.className = "cursor cursor-theme";
        cursorTheme.setAttribute("data-mode", "arrow");
        themePlace(540, 380);
      }

      sceneRootTheme.classList.remove("dark-theme");
      if (winGalleryTheme) {
        winGalleryTheme.classList.remove("shown");
        winGalleryTheme.classList.remove("expanded");
        winGalleryTheme.classList.remove("resizing");
      }
      if (gearBtnTheme) gearBtnTheme.classList.remove("clicked");
      if (galleryCtxMenu) galleryCtxMenu.classList.remove("shown");
      if (winPrefModal) winPrefModal.classList.remove("shown");
      if (themeDropdownMenu) themeDropdownMenu.classList.remove("shown");
      if (themeCurrLabel) themeCurrLabel.textContent = "라이트 모드 (Light)";

      // Reset dropdown options: Light active, Dark inactive
      if (optLight) {
        optLight.classList.add("active");
        var rl = optLight.querySelector(".opt-radio");
        if (rl) rl.classList.add("active");
      }
      if (optDark) {
        optDark.classList.remove("active");
        var rd = optDark.querySelector(".opt-radio");
        if (rd) rd.classList.remove("active");
      }

      if (captionTheme) captionTheme.textContent = t("scene.caption.idle");
      sceneRootTheme.classList.remove("done");
    }

    // ---- Step 1: Panel Width Resize Sequence ----
    function playSizeStep(onComplete) {
      resetThemeScene();
      setActivePill("size");
      console.log("[TrayShot] playing Panel Resize step");

      // 1. Gallery open
      themeAt(300, function () {
        if (winGalleryTheme) winGalleryTheme.classList.add("shown");
        if (captionTheme) captionTheme.textContent = t("theme.caption.size.1");
      });

      // 2. Cursor moves to Left Resize Handle
      themeAt(900, function () {
        if (cursorTheme) cursorTheme.classList.add("on");
        if (galleryResizeHandle) themeMoveToEl(galleryResizeHandle, 700);
      });

      // 3. Switch to ew-resize cursor (↔)
      themeAt(1700, function () {
        if (cursorTheme) cursorTheme.setAttribute("data-mode", "resize");
        if (winGalleryTheme) winGalleryTheme.classList.add("resizing");
      });

      // 4. Drag left to expand (440px -> 580px / 4-columns) with exact 1:1 sync
      themeAt(2200, function () {
        var gBox = themeBoxOf(winGalleryTheme);
        var rightEdge = gBox.x + gBox.w; // The fixed right boundary of gallery
        var handleY = galleryResizeHandle ? themeCenterOf(galleryResizeHandle).y : 220;
        var targetX = rightEdge - 580 + 5; // Left handle center of expanded 580px gallery
        themeMove(targetX, handleY, 800);
        if (winGalleryTheme) winGalleryTheme.classList.add("expanded");
        if (captionTheme) captionTheme.textContent = t("theme.caption.size.2");
      });

      // 5. Hold wide 4-column state
      themeAt(4000, function () {
        if (captionTheme) captionTheme.textContent = t("theme.caption.size.3");
      });

      // 6. Drag back right to shrink (580px -> 440px standard) with exact 1:1 sync
      themeAt(4600, function () {
        var gBox = themeBoxOf(winGalleryTheme);
        var rightEdge = gBox.x + gBox.w;
        var handleY = galleryResizeHandle ? themeCenterOf(galleryResizeHandle).y : 220;
        var targetX = rightEdge - 440 + 5; // Left handle center of standard 440px gallery
        themeMove(targetX, handleY, 800);
        if (winGalleryTheme) winGalleryTheme.classList.remove("expanded");
      });

      // 7. Release drag and revert to arrow cursor
      themeAt(5600, function () {
        if (cursorTheme) cursorTheme.setAttribute("data-mode", "arrow");
        if (winGalleryTheme) winGalleryTheme.classList.remove("resizing");
      });

      themeAt(6600, function () {
        if (onComplete) {
          onComplete();
        } else {
          sceneRootTheme.classList.add("done");
        }
      });
    }

    // ---- Step 2: Theme Switching Sequence ----
    function playThemeStep(onComplete) {
      resetThemeScene();
      setActivePill("theme");
      console.log("[TrayShot] playing Theme Switch step");

      // 1. Light gallery open
      themeAt(300, function () {
        if (winGalleryTheme) winGalleryTheme.classList.add("shown");
        if (captionTheme) captionTheme.textContent = t("theme.caption.theme.1");
      });

      // 2. Cursor moves to Gear
      themeAt(900, function () {
        if (cursorTheme) cursorTheme.classList.add("on");
        if (gearBtnTheme) themeMoveToEl(gearBtnTheme, 700);
      });

      // 3. Click Gear -> context menu opens
      themeAt(1700, function () {
        if (gearBtnTheme) gearBtnTheme.classList.add("clicked");
        if (galleryCtxMenu) galleryCtxMenu.classList.add("shown");
      });

      // 4. Click '설정...' -> Preferences Window opens
      themeAt(2400, function () {
        if (ctxItemSettings) themeMoveToEl(ctxItemSettings, 500);
      });

      themeAt(3100, function () {
        if (galleryCtxMenu) galleryCtxMenu.classList.remove("shown");
        if (gearBtnTheme) gearBtnTheme.classList.remove("clicked");
        if (winPrefModal) winPrefModal.classList.add("shown");
        if (captionTheme) captionTheme.textContent = t("theme.caption.theme.2");
      });

      // 5. Cursor moves to Theme dropdown trigger & clicks
      themeAt(4200, function () {
        if (themeSelectTrigger) themeMoveToEl(themeSelectTrigger, 700);
      });

      themeAt(5100, function () {
        if (themeDropdownMenu) themeDropdownMenu.classList.add("shown");
      });

      // 6. Move cursor to "다크 모드 (Dark)" option
      themeAt(5800, function () {
        if (optDark) themeMoveToEl(optDark, 500);
      });

      // 7. Click "다크 모드 (Dark)" -> selection changes & dark theme activates!
      themeAt(6500, function () {
        if (optLight) {
          optLight.classList.remove("active");
          var rl = optLight.querySelector(".opt-radio");
          if (rl) rl.classList.remove("active");
        }
        if (optDark) {
          optDark.classList.add("active");
          var rd = optDark.querySelector(".opt-radio");
          if (rd) rd.classList.add("active");
        }
        if (themeCurrLabel) themeCurrLabel.textContent = "다크 모드 (Dark)";
        sceneRootTheme.classList.add("dark-theme");
        if (captionTheme) captionTheme.textContent = t("theme.caption.theme.3");
      });

      // 8. Dropdown closes after showing the selected Dark state
      themeAt(7300, function () {
        if (themeDropdownMenu) themeDropdownMenu.classList.remove("shown");
      });

      // 9. Cursor moves to Close button on modal and clicks
      themeAt(8200, function () {
        if (prefCloseBtn) themeMoveToEl(prefCloseBtn, 700);
      });

      themeAt(9100, function () {
        if (winPrefModal) winPrefModal.classList.remove("shown");
        if (captionTheme) captionTheme.textContent = t("theme.caption.theme.4");
      });

      // 10. Dark gallery closes & done
      themeAt(10600, function () {
        if (winGalleryTheme) winGalleryTheme.classList.remove("shown");
        if (cursorTheme) cursorTheme.classList.remove("on");
      });

      themeAt(11600, function () {
        sceneRootTheme.classList.add("done");
        if (onComplete) onComplete();
      });
    }

    // ---- Full Sequential Loop: Size -> Theme ----
    function playFullSequence() {
      playSizeStep(function () {
        themeAt(1200, function () {
          playThemeStep(function () {
            console.log("[TrayShot] theme scene animation complete, looping in 3s");
            sceneRootTheme.classList.add("done");
            themeAt(3000, playFullSequence);
          });
        });
      });
    }

    // Interactive Pill Tab button events
    if (pillSize) {
      pillSize.addEventListener("click", function (e) {
        e.stopPropagation();
        playSizeStep(function () {
          themeAt(3000, playFullSequence);
        });
      });
    }
    if (pillTheme) {
      pillTheme.addEventListener("click", function (e) {
        e.stopPropagation();
        playThemeStep(function () {
          themeAt(3000, playFullSequence);
        });
      });
    }

    if (replayBtnTheme) {
      replayBtnTheme.addEventListener("click", function (e) {
        e.stopPropagation();
        playFullSequence();
      });
    }
    sceneRootTheme.addEventListener("click", function (e) {
      if (e.target.closest(".scene-pills") || e.target.closest(".replay-theme")) return;
      playFullSequence();
    });

    // Auto-start complete sequence
    setTimeout(playFullSequence, 700);
  } else {
    console.warn("[TrayShot] scene[data-scene=theme] not found in DOM");
  }

  /* ---- 5. Init ---- */
  applyLanguage(localStorage.getItem("trayshot_lang") || getBrowserLang());
  loadReleaseInfo();
  loadStarCount();

})();
