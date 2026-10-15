/* پنل مدیریت: منوی جمع‌شونده، برد مراحل، برنامه‌ی هفتگی */
(function () {
  "use strict";
  const $ = (s, r = document) => r.querySelector(s);
  const $$ = (s, r = document) => Array.from(r.querySelectorAll(s));
  const token = () => $("meta[name=csrf]")?.content || "";

  window.toast = function (msg, isError) {
    let t = $(".toast");
    if (!t) { t = document.createElement("div"); t.className = "toast"; t.setAttribute("role", "status"); document.body.appendChild(t); }
    t.textContent = msg;
    t.classList.toggle("error", !!isError);
    t.classList.add("show");
    clearTimeout(t._h);
    t._h = setTimeout(() => t.classList.remove("show"), 2600);
  };

  window.postJson = async function (url, body) {
    const res = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json", "RequestVerificationToken": token() },
      body: JSON.stringify(body),
    });
    let data = null;
    try { data = await res.json(); } catch { /* بدون بدنه */ }
    if (!res.ok) throw new Error(data?.error || "خطا در ذخیره. دوباره تلاش کنید.");
    return data;
  };

  /* ---------- منوی کناری ---------- */
  function initSidebar() {
    const app = $(".app");
    if (!app) return;
    const key = "celavimed.side.collapsed";
    try { if (localStorage.getItem(key) === "1") app.classList.add("collapsed"); } catch { /* حالت خصوصی */ }
    $$("[data-side-toggle]").forEach((b) => b.addEventListener("click", () => {
      if (window.matchMedia("(max-width: 960px)").matches) {
        app.classList.toggle("mobile-open");
      } else {
        app.classList.toggle("collapsed");
        try { localStorage.setItem(key, app.classList.contains("collapsed") ? "1" : "0"); } catch { /* */ }
      }
    }));
    $(".overlay")?.addEventListener("click", () => app.classList.remove("mobile-open"));
    // عنوان منو در حالت جمع‌شده به‌صورت tooltip
    $$(".nav-link").forEach((a) => a.setAttribute("title", a.textContent.trim()));
  }

  /* ---------- برد مراحل ---------- */
  function initBoard() {
    const board = $(".board");
    if (!board) return;
    const canEdit = board.dataset.canEdit === "1";
    const lanes = $$(".lane", board);
    const stageIds = lanes.map((l) => +l.dataset.stage);

    function refreshCounts() {
      lanes.forEach((l) => {
        const n = $$(".ticket", l).length;
        $(".lane-count", l).textContent = n;
        $(".lane-empty", l)?.classList.toggle("sr-only", n > 0);
      });
    }

    async function move(ticket, stageId) {
      const from = ticket.closest(".lane");
      const to = lanes.find((l) => +l.dataset.stage === stageId);
      if (!to || from === to) return;
      $(".lane-body", to).appendChild(ticket);
      sortLane(to);
      refreshCounts();
      updateButtons(ticket);
      try {
        await postJson(board.dataset.moveUrl, { id: +ticket.dataset.id, stageId });
        toast(`به «${to.dataset.name}» منتقل شد`);
      } catch (e) {
        $(".lane-body", from).appendChild(ticket);
        sortLane(from);
        refreshCounts();
        updateButtons(ticket);
        toast(e.message, true);
      }
    }

    function sortLane(lane) {
      const body = $(".lane-body", lane);
      $$(".ticket", body).sort((a, b) => a.dataset.sort.localeCompare(b.dataset.sort)).forEach((t) => body.appendChild(t));
    }

    function updateButtons(ticket) {
      const idx = stageIds.indexOf(+ticket.closest(".lane").dataset.stage);
      const prev = $("[data-step=prev]", ticket), next = $("[data-step=next]", ticket);
      if (prev) prev.disabled = idx <= 0;
      if (next) {
        next.disabled = idx >= stageIds.length - 1;
        const nextLane = lanes[idx + 1];
        $("span", next).textContent = nextLane ? nextLane.dataset.name : "پایان";
      }
    }

    $$(".ticket", board).forEach((t) => {
      updateButtons(t);
      if (!canEdit) return;
      t.draggable = true;
      t.addEventListener("dragstart", (e) => { t.classList.add("dragging"); e.dataTransfer.setData("text/plain", t.dataset.id); e.dataTransfer.effectAllowed = "move"; });
      t.addEventListener("dragend", () => t.classList.remove("dragging"));
      $$("[data-step]", t).forEach((b) => b.addEventListener("click", () => {
        const idx = stageIds.indexOf(+t.closest(".lane").dataset.stage);
        const target = stageIds[idx + (b.dataset.step === "next" ? 1 : -1)];
        if (target) move(t, target);
      }));
    });

    if (canEdit) lanes.forEach((l) => {
      l.addEventListener("dragover", (e) => { e.preventDefault(); l.classList.add("drag-over"); });
      l.addEventListener("dragleave", (e) => { if (!l.contains(e.relatedTarget)) l.classList.remove("drag-over"); });
      l.addEventListener("drop", (e) => {
        e.preventDefault();
        l.classList.remove("drag-over");
        const t = board.querySelector(`.ticket[data-id="${e.dataTransfer.getData("text/plain")}"]`);
        if (t) move(t, +l.dataset.stage);
      });
    });
    refreshCounts();
  }

  /* ---------- برنامه‌ی هفتگی ---------- */
  function initWeek() {
    const week = $(".week");
    if (!week) return;
    const tpl = $("#range-tpl");

    function count(range) {
      const s = $("[data-f=start]", range).value, e = $("[data-f=end]", range).value, len = +$("[data-f=len]", range).value;
      const out = $(".range-count", range);
      if (!s || !e || !len) { out.textContent = ""; return; }
      const toMin = (v) => { const [h, m] = v.split(":").map(Number); return h * 60 + m; };
      const n = Math.max(0, Math.floor((toMin(e) - toMin(s)) / len));
      out.textContent = n > 0 ? `${n} نوبت` : "بازه نامعتبر";
      out.style.color = n > 0 ? "" : "var(--danger)";
    }

    function reindex() {
      let i = 0;
      $$(".week-day", week).forEach((day) => {
        const ranges = $$(".range", day);
        day.classList.toggle("is-off", ranges.length === 0);
        $(".day-off-note", day)?.classList.toggle("sr-only", ranges.length > 0);
        ranges.forEach((r) => {
          $$("[data-f]", r).forEach((inp) => {
            const map = { day: "DayOfWeek", start: "StartTime", end: "EndTime", len: "SlotMinutes" };
            inp.name = `Ranges[${i}].${map[inp.dataset.f]}`;
          });
          i++;
        });
      });
    }

    function bind(range) {
      $$("input, select", range).forEach((el) => el.addEventListener("input", () => count(range)));
      $(".range-del", range).addEventListener("click", () => { range.remove(); reindex(); });
      count(range);
    }

    $$(".range", week).forEach(bind);
    $$("[data-add-range]", week).forEach((btn) => btn.addEventListener("click", () => {
      const day = btn.closest(".week-day");
      const node = tpl.content.firstElementChild.cloneNode(true);
      $("[data-f=day]", node).value = day.dataset.day;
      const last = $$(".range", day).pop();
      if (last) {
        $("[data-f=start]", node).value = $("[data-f=end]", last).value;
        $("[data-f=len]", node).value = $("[data-f=len]", last).value;
      }
      $(".ranges", day).appendChild(node);
      bind(node);
      reindex();
      $("[data-f=start]", node).focus();
    }));
    $$("[data-copy-to-all]", week).forEach((btn) => btn.addEventListener("click", () => {
      const src = btn.closest(".week-day");
      const srcRanges = $$(".range", src);
      if (!srcRanges.length) return;
      $$(".week-day", week).forEach((day) => {
        if (day === src || day.dataset.day === "5") return; // جمعه را دست نمی‌زنیم
        $$(".range", day).forEach((r) => r.remove());
        srcRanges.forEach((r) => {
          const c = r.cloneNode(true);
          $("[data-f=day]", c).value = day.dataset.day;
          $$("select", r).forEach((s, i) => { $$("select", c)[i].value = s.value; });
          $(".ranges", day).appendChild(c);
          bind(c);
        });
      });
      reindex();
      toast("بازه‌ها در روزهای دیگر هم کپی شد؛ برای ثبت، ذخیره را بزنید.");
    }));
    reindex();
  }

  /* ---------- پیش‌نمایش عکس ---------- */
  function initPhoto() {
    $$("[data-photo-input]").forEach((inp) => inp.addEventListener("change", () => {
      const f = inp.files?.[0];
      const img = $(inp.dataset.photoInput);
      if (f && img) {
        const url = URL.createObjectURL(f);
        if (img.tagName === "IMG") img.src = url;
        else { const n = document.createElement("img"); n.className = img.className.replace("doc-photo-fallback", ""); n.id = img.id; n.src = url; n.alt = ""; img.replaceWith(n); }
      }
    }));
  }

  /* ---------- تایید قبل از حذف ---------- */
  function initConfirm() {
    document.addEventListener("submit", (e) => {
      const msg = e.target.dataset?.confirm;
      if (msg && !confirm(msg)) e.preventDefault();
    });
  }

  /* ---------- مودال ---------- */
  function initModals() {
    $$("[data-open-modal]").forEach((b) => b.addEventListener("click", () => $(b.dataset.openModal)?.showModal()));
    $$("dialog [data-close]").forEach((b) => b.addEventListener("click", () => b.closest("dialog").close()));
  }

  document.addEventListener("DOMContentLoaded", () => {
    initSidebar();
    initBoard();
    initWeek();
    initPhoto();
    initConfirm();
    initModals();
    const flash = $("[data-flash]");
    if (flash) toast(flash.dataset.flash, flash.dataset.flashError === "1");
  });
})();
