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


  /* ---------- فرم‌ها: ارسال خودکار و پرش با انتخاب (به‌جای onchange درون‌خطی، به خاطر CSP) ---------- */
  function initAutoSubmit() {
    $$("select[data-autosubmit]").forEach((s) => s.addEventListener("change", () => s.form?.submit()));
    $$("select[data-nav-prefix]").forEach((s) => s.addEventListener("change", () => {
      if (/^\d+$/.test(s.value)) location.href = s.dataset.navPrefix + s.value;
    }));
  }

  /* ---------- فرم کاربر: انتخاب پزشکان فقط برای منشی ---------- */
  function initUserForm() {
    const pick = document.getElementById("doctor-pick");
    if (!pick) return;
    const sync = () => { pick.hidden = document.querySelector("#role-chips input:checked")?.value !== "Secretary"; };
    $$("#role-chips input").forEach((i) => i.addEventListener("change", sync));
    sync();
  }

  /* ---------- نوبت تلفنی: خدمات پزشک و ساعت‌های آزاد ---------- */
  function initNewAppointment() {
    const form = document.getElementById("new-appt");
    if (!form) return;
    const doc = form.querySelector("#n-doc"), svc = form.querySelector("#n-svc");
    const date = form.querySelector("input[name=Date]"), time = form.querySelector("#n-time"), box = form.querySelector("#n-slots");

    function filterServices() {
      const allowed = (doc.selectedOptions[0]?.dataset.services || "").split(",").filter(Boolean);
      for (const o of svc.options) if (o.value) o.hidden = allowed.length > 0 && !allowed.includes(o.value);
      if (svc.selectedOptions[0]?.hidden) svc.value = "";
    }
    async function loadSlots() {
      if (!doc.value || !date.value) return;
      box.innerHTML = '<span class="hint">در حال دریافت ساعت‌ها…</span>';
      const res = await fetch(`/admin/appointments/slots?doctorId=${encodeURIComponent(doc.value)}&date=${encodeURIComponent(date.value)}`);
      const slots = res.ok ? await res.json() : [];
      if (!slots.length) { box.innerHTML = '<span class="hint">این پزشک در این روز نوبت نمی‌دهد.</span>'; return; }
      box.innerHTML = "";
      for (const s of slots) {
        const l = document.createElement("label");
        const r = document.createElement("input");
        r.type = "radio"; r.name = "_slot"; r.value = s.start; r.disabled = !s.free;
        const span = document.createElement("span");
        span.textContent = s.start;
        if (!s.free) span.className = "slot-taken";
        l.append(r, span);
        if (s.start === time.value && s.free) r.checked = true;
        r.addEventListener("change", () => { time.value = s.start; });
        box.appendChild(l);
      }
    }
    doc.addEventListener("change", () => { filterServices(); time.value = ""; loadSlots(); });
    date.addEventListener("change", () => { time.value = ""; loadSlots(); });
    filterServices();
    loadSlots();
  }

  /* ---------- انبار ---------- */
  function initInventory() {
    // مشخصه‌های اختصاصی گروه: افزودن و حذف ردیف
    $$("[data-field-list]").forEach((box) => {
      const rows = $(".field-rows", box);
      const reindex = () => $$(".field-row", rows).forEach((r, i) => {
        $("[data-f=id]", r).name = `Fields[${i}].Id`;
        $("[data-f=name]", r).name = `Fields[${i}].Name`;
      });
      const bind = (r) => $("[data-remove-row]", r).addEventListener("click", () => {
        if ($$(".field-row", rows).length > 1) r.remove();
        else { $("[data-f=id]", r).value = "0"; $("[data-f=name]", r).value = ""; }
        reindex();
      });
      $$(".field-row", rows).forEach(bind);
      $("[data-add-row]", box).addEventListener("click", () => {
        const last = $$(".field-row", rows).pop();
        const n = last.cloneNode(true);
        $("[data-f=id]", n).value = "0";
        $("[data-f=name]", n).value = "";
        rows.appendChild(n);
        bind(n);
        reindex();
        $("[data-f=name]", n).focus();
      });
    });

    // فرم کالا: با عوض شدن گروه، مشخصه‌های همان گروه نمایش داده شود
    const chips = document.getElementById("group-chips");
    if (chips) {
      const sync = () => {
        const g = $("input:checked", chips)?.value;
        $$("[data-group-fields]").forEach((sec) => {
          const on = sec.dataset.groupFields === g;
          sec.hidden = !on;
          $$("input, textarea, select", sec).forEach((i) => { i.disabled = !on; });
        });
      };
      $$("input", chips).forEach((i) => i.addEventListener("change", sync));
      sync();
    }

    // شمارش بچ
    const adj = document.getElementById("adjust-modal");
    $$("[data-adjust]").forEach((b) => b.addEventListener("click", () => {
      if (!adj) return;
      $("form", adj).action = b.dataset.adjust;
      $("[data-adjust-batch]", adj).textContent = b.dataset.batch;
      $("[data-adjust-qty]", adj).textContent = b.dataset.qty;
      $("#adj-count", adj).value = "";
      adj.showModal();
      $("#adj-count", adj).focus();
    }));

    // جداکننده‌ی هزارگان برای مبالغ (ارقام فارسی هم پذیرفته می‌شود)
    const latin = (s) => s.replace(/[۰-۹]/g, (d) => "۰۱۲۳۴۵۶۷۸۹".indexOf(d)).replace(/[٠-٩]/g, (d) => "٠١٢٣٤٥٦٧٨٩".indexOf(d));
    const fmt = (inp) => {
      const digits = latin(inp.value).replace(/[^\d]/g, "");
      inp.value = digits ? Number(digits).toLocaleString("en-US") : "";
    };
    $$("input[data-money]").forEach((inp) => { fmt(inp); inp.addEventListener("input", () => fmt(inp)); });
  }

  /* ---------- جستجوی سریع بیمار ---------- */
  function initPatientSearch() {
    const forms = $$("[data-patient-search]");
    if (!forms.length) return;
    const latin = (s) => s.replace(/[۰-۹]/g, (d) => "۰۱۲۳۴۵۶۷۸۹".indexOf(d)).replace(/[٠-٩]/g, (d) => "٠١٢٣٤٥٦٧٨٩".indexOf(d));

    // متن را با برجسته کردن بخش منطبق، بدون innerHTML می‌سازد
    function mark(el, text, term) {
      const i = term ? latin(text).toLowerCase().indexOf(latin(term).toLowerCase()) : -1;
      if (i < 0) { el.textContent = text; return; }
      const m = document.createElement("mark");
      m.textContent = text.slice(i, i + term.length);
      el.append(text.slice(0, i), m, text.slice(i + term.length));
    }

    forms.forEach((form) => {
      const input = $(".psearch-input", form);
      const pop = $(".psearch-pop", form);
      let items = [], active = -1, timer = 0, ctrl = null, lastTerm = "";

      const close = () => { pop.hidden = true; input.setAttribute("aria-expanded", "false"); active = -1; };
      const open = () => { pop.hidden = false; input.setAttribute("aria-expanded", "true"); };
      const setActive = (i) => {
        active = i;
        $$(".psearch-item", pop).forEach((el, k) => el.classList.toggle("is-active", k === i));
        if (i >= 0) $$(".psearch-item", pop)[i]?.scrollIntoView({ block: "nearest" });
      };

      function render(list, term) {
        items = list;
        pop.textContent = "";
        if (!list.length) {
          const empty = document.createElement("div");
          empty.className = "psearch-empty";
          empty.textContent = `بیماری با «${term}» پیدا نشد.`;
          pop.appendChild(empty);
        }
        list.forEach((p, i) => {
          const a = document.createElement("a");
          a.className = "psearch-item";
          a.href = p.url;
          a.setAttribute("role", "option");
          const av = document.createElement("span");
          av.className = "psearch-avatar";
          av.textContent = (p.name || "؟").trim().charAt(0);
          const body = document.createElement("span");
          body.className = "psearch-body";
          const nm = document.createElement("b");
          mark(nm, p.name, term);
          const mob = document.createElement("span");
          mob.className = "psearch-mobile ltr";
          mark(mob, p.mobile, /\d/.test(latin(term)) ? latin(term).replace(/\D/g, "") : "");
          const meta = document.createElement("small");
          meta.textContent = p.next ? `نوبت بعدی: ${p.next}` : p.last ? `آخرین مراجعه ${p.last} · ${p.visits} نوبت` : `${p.visits} نوبت`;
          if (p.next) meta.classList.add("has-next");
          body.append(nm, mob, meta);
          a.append(av, body);
          a.addEventListener("mouseenter", () => setActive(i));
          pop.appendChild(a);
        });
        const all = document.createElement("a");
        all.className = "psearch-all";
        all.href = `/admin/patients?q=${encodeURIComponent(term)}`;
        all.textContent = "همه‌ی نتایج در فهرست بیماران";
        pop.appendChild(all);
        open();
        setActive(list.length ? 0 : -1);
      }

      async function search() {
        const term = input.value.trim();
        if (term.length < 2) { close(); lastTerm = ""; return; }
        if (term === lastTerm && !pop.hidden) return;
        lastTerm = term;
        ctrl?.abort();
        ctrl = new AbortController();
        form.classList.add("is-loading");
        try {
          const res = await fetch(`/admin/search/patients?q=${encodeURIComponent(term)}`, { signal: ctrl.signal, headers: { Accept: "application/json" } });
          if (!res.ok) throw new Error();
          if (input.value.trim() === term) render(await res.json(), term);
        } catch (e) {
          if (e.name !== "AbortError") close();
        } finally {
          form.classList.remove("is-loading");
        }
      }

      input.addEventListener("input", () => { clearTimeout(timer); timer = setTimeout(search, 180); });
      input.addEventListener("focus", () => { if (items.length && input.value.trim().length >= 2) open(); });
      input.addEventListener("keydown", (e) => {
        if (e.key === "ArrowDown" && !pop.hidden) { e.preventDefault(); setActive(Math.min(active + 1, items.length - 1)); }
        else if (e.key === "ArrowUp" && !pop.hidden) { e.preventDefault(); setActive(Math.max(active - 1, 0)); }
        else if (e.key === "Escape") { close(); input.blur(); }
      });
      // Enter: بیمار انتخاب‌شده، وگرنه فهرست بیماران با همین عبارت
      form.addEventListener("submit", (e) => {
        if (input.value.trim().length < 2) { e.preventDefault(); input.focus(); return; }
        const pick = items[active];
        if (!pop.hidden && pick && input.value.trim() === lastTerm) { e.preventDefault(); location.href = pick.url; }
      });
      document.addEventListener("click", (e) => { if (!form.contains(e.target)) close(); });
    });

    // میان‌بر «/» برای رفتن به جستجو
    document.addEventListener("keydown", (e) => {
      if (e.key !== "/" || e.ctrlKey || e.metaKey || e.altKey) return;
      const t = e.target;
      if (t.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(t.tagName)) return;
      const input = $(".psearch-hero .psearch-input") || $(".psearch-bar .psearch-input");
      if (!input) return;
      e.preventDefault();
      $(".topbar-search")?.classList.add("is-open");
      input.focus();
    });

    // موبایل: دکمه‌ی ذره‌بین نوار بالا کادر جستجو را باز و بسته می‌کند
    $$("[data-search-toggle]").forEach((b) => b.addEventListener("click", () => {
      const box = document.getElementById(b.getAttribute("aria-controls"));
      if (!box) return;
      box.classList.toggle("is-open");
      if (box.classList.contains("is-open")) $(".psearch-input", box)?.focus();
    }));
  }

  document.addEventListener("DOMContentLoaded", () => {
    initSidebar();
    initBoard();
    initWeek();
    initPhoto();
    initConfirm();
    initModals();
    initAutoSubmit();
    initUserForm();
    initNewAppointment();
    initInventory();
    initPatientSearch();
    const flash = $("[data-flash]");
    if (flash) toast(flash.dataset.flash, flash.dataset.flashError === "1");
  });
})();
