/* رزرو آنلاین: پزشک ← روز و ساعت ← مشخصات */
(function () {
  "use strict";
  const root = document.getElementById("booking");
  if (!root) return;
  const $ = (s, r = root) => r.querySelector(s);
  const $$ = (s, r = root) => Array.from(r.querySelectorAll(s));
  const esc = (s) => String(s ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  const icon = (n, c = "") => `<svg class="icon ${c}" aria-hidden="true"><use href="/img/icons.svg#i-${n}"/></svg>`;
  const J = window.J;

  const doctors = JSON.parse(root.dataset.doctors || "[]");
  const state = { doctor: null, date: null, time: null, view: null, today: null, last: null, avail: {} };

  /* ---------- مراحل ---------- */
  function go(step) {
    $$(".stage").forEach((s) => (s.hidden = s.dataset.stage !== String(step)));
    $$(".steps li").forEach((li) => {
      const n = +li.dataset.step;
      li.classList.toggle("is-active", n === step);
      li.classList.toggle("is-done", n < step || step === 4);
    });
    const top = root.getBoundingClientRect().top + window.scrollY - 12;
    if (window.scrollY > top || step > 1) window.scrollTo({ top, behavior: matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth" });
    if (step === 3) { fillSummary(); setTimeout(() => $("#b-name").focus({ preventScroll: true }), 50); }
  }
  $$("[data-go]").forEach((b) => b.addEventListener("click", () => go(+b.dataset.go)));

  /* ---------- ۱. پزشکان ---------- */
  function renderDoctors() {
    const list = $("#doctor-list");
    list.innerHTML = doctors.map((d) => `
      <button type="button" class="doctor" data-id="${d.id}" ${d.hasSchedule ? "" : 'aria-disabled="true"'}>
        <span class="doctor-photo">${d.photo ? `<img src="${esc(d.photo)}" alt="" loading="lazy">` : `<span>${esc(d.name.replace("دکتر", "").trim().charAt(0))}</span>`}</span>
        <h3>${esc(d.name)}</h3>
        <span class="headline">${esc(d.headline || "")}</span>
        ${d.services.length ? `<span class="tags">${d.services.slice(0, 4).map((s) => `<span>${esc(s.name)}</span>`).join("")}</span>` : ""}
        ${d.hasSchedule ? "" : '<span class="no-slot">فعلا نوبت آنلاین ندارد</span>'}
      </button>`).join("");
    $$(".doctor", list).forEach((el) => el.addEventListener("click", () => {
      const d = doctors.find((x) => x.id === +el.dataset.id);
      if (!d || !d.hasSchedule) return;
      $$(".doctor", list).forEach((x) => x.classList.toggle("is-selected", x === el));
      if (state.doctor?.id !== d.id) { state.date = null; state.time = null; state.view = null; state.avail = {}; }
      state.doctor = d;
      $('[data-val="doctor"]').textContent = d.name;
      $('[data-val="time"]').textContent = "";
      go(2);
      loadMonth();
      if (!state.date) resetSlots();
    }));
  }

  /* ---------- ۲. تقویم ---------- */
  const cal = $("#cal");
  async function loadMonth() {
    if (!state.view) {
      const base = J.toJ(state.today || J.todayIso());
      state.view = { jy: base.jy, jm: base.jm };
    }
    const { from, to } = J.monthRange(state.view.jy, state.view.jm);
    $(".jcal-title", cal).textContent = `${J.months[state.view.jm - 1]} ${state.view.jy}`;
    cal.classList.add("loading");
    try {
      const res = await fetch(`/api/booking/availability?doctorId=${state.doctor.id}&from=${from}&to=${to}`);
      const data = await res.json();
      state.today = data.today;
      state.last = data.last;
      state.avail = {};
      for (const d of data.days) state.avail[d.date] = d;
    } catch {
      state.avail = {};
    }
    cal.classList.remove("loading");
    drawMonth();
  }

  function drawMonth() {
    J.renderGrid($(".jcal-grid", cal), {
      jy: state.view.jy, jm: state.view.jm, today: state.today,
      cell(iso, el) {
        const a = state.avail[iso];
        if (a && a.free > 0) {
          el.classList.add("is-open");
          el.insertAdjacentHTML("beforeend", `<span class="jcal-count">${a.free} نوبت</span>`);
          if (iso === state.date) el.classList.add("is-selected");
          return true;
        }
        if (a && a.total > 0 && a.free === 0) el.classList.add("is-full");
        return false;
      },
      onPick(iso, el) {
        $$(".jcal-day", cal).forEach((d) => d.classList.remove("is-selected"));
        el.classList.add("is-selected");
        state.date = iso;
        state.time = null;
        $("#to-details").disabled = true;
        loadSlots();
      },
    });
    const first = J.toJ(state.today || J.todayIso());
    const last = state.last ? J.toJ(state.last) : null;
    $('[data-cal="prev"]', cal).disabled = state.view.jy < first.jy || (state.view.jy === first.jy && state.view.jm <= first.jm);
    $('[data-cal="next"]', cal).disabled = !!last && (state.view.jy > last.jy || (state.view.jy === last.jy && state.view.jm >= last.jm));
  }
  $('[data-cal="prev"]', cal).addEventListener("click", () => { state.view = J.shiftMonth(state.view.jy, state.view.jm, -1); loadMonth(); });
  $('[data-cal="next"]', cal).addEventListener("click", () => { state.view = J.shiftMonth(state.view.jy, state.view.jm, 1); loadMonth(); });

  /* ---------- ساعت‌ها ---------- */
  const slotsBox = $("#slots");
  function resetSlots() {
    slotsBox.innerHTML = `<div class="slots-empty">${icon("calendar-days")}یک روز صورتی را از تقویم انتخاب کنید تا ساعت‌های آزادش را ببینید.</div>`;
    $("#to-details").disabled = true;
  }

  async function loadSlots() {
    slotsBox.innerHTML = `<div class="slots-empty"><span class="spinner" style="margin:0 auto 8px;display:block"></span>در حال دریافت ساعت‌ها…</div>`;
    let slots = [];
    try {
      const res = await fetch(`/api/booking/slots?doctorId=${state.doctor.id}&date=${state.date}`);
      slots = await res.json();
    } catch { /* */ }
    const free = slots.filter((s) => s.free);
    if (!free.length) {
      slotsBox.innerHTML = `<div class="slots-empty">${icon("calendar-x")}ساعت‌های این روز پر شد. روز دیگری را انتخاب کنید.</div>`;
      return;
    }
    const groups = [
      { title: "صبح", test: (h) => h < 12 },
      { title: "بعدازظهر", test: (h) => h >= 12 && h < 16 },
      { title: "عصر", test: (h) => h >= 16 },
    ];
    let html = `<div class="slots-head"><h3>${esc(J.long(state.date))}</h3><span class="faint" style="font-size:13px">${free.length} ساعت آزاد</span></div>`;
    for (const g of groups) {
      const items = slots.filter((s) => g.test(+s.start.slice(0, 2)));
      if (!items.length) continue;
      html += `<div class="slot-group"><h4>${g.title}</h4><div class="slot-grid">${items.map((s) =>
        `<button type="button" class="slot" data-time="${s.start}" ${s.free ? "" : 'disabled aria-label="' + s.start + ' رزرو شده"'}>${s.start}</button>`).join("")}</div></div>`;
    }
    slotsBox.innerHTML = html;
    $$(".slot", slotsBox).forEach((b) => b.addEventListener("click", () => {
      $$(".slot", slotsBox).forEach((x) => x.classList.toggle("is-selected", x === b));
      state.time = b.dataset.time;
      $('[data-val="time"]').textContent = `${J.dayMonth(state.date)}، ساعت ${state.time}`;
      $("#to-details").disabled = false;
      if (matchMedia("(max-width: 640px)").matches) $("#to-details").scrollIntoView({ block: "nearest", behavior: "smooth" });
    }));
  }

  /* ---------- ۳. مشخصات ---------- */
  function fillSummary() {
    $('[data-sum="doctor"]').textContent = state.doctor.name;
    $('[data-sum="date"]').textContent = J.long(state.date);
    $('[data-sum="time"]').textContent = state.time;
    const svc = state.doctor.services;
    $("#svc-field").hidden = svc.length === 0;
    const current = $("#svc-list input:checked")?.value;
    $("#svc-list").innerHTML = svc.map((s, i) => `
      <label class="svc"><input type="radio" name="service" value="${s.id}" ${String(s.id) === current || (svc.length === 1 && i === 0) ? "checked" : ""}>
      <span><b>${esc(s.name)}</b>${s.description ? `<small>${esc(s.description)}</small>` : ""}</span></label>`).join("");
  }

  const form = $("#book-form");
  const errBox = $("#form-error");
  function showError(msg) { errBox.textContent = msg; errBox.hidden = false; errBox.scrollIntoView({ block: "nearest" }); }

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    errBox.hidden = true;
    const name = form.name.value.trim();
    const mobile = form.mobile.value.replace(/[۰-۹]/g, (d) => "۰۱۲۳۴۵۶۷۸۹".indexOf(d)).replace(/[٠-٩]/g, (d) => "٠١٢٣٤٥٦٧٨٩".indexOf(d)).replace(/[\s-]/g, "");
    const service = form.querySelector("input[name=service]:checked")?.value;
    if (name.length < 3) { showError("نام و نام خانوادگی را کامل بنویسید."); form.name.focus(); return; }
    if (!/^(\+98|0)9\d{9}$/.test(mobile)) { showError("شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود."); form.mobile.focus(); return; }
    if (state.doctor.services.length && !service) { showError("نوع خدمت را انتخاب کنید."); return; }

    const btn = $("#submit-btn");
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner"></span>در حال ثبت…';
    try {
      const res = await fetch("/api/booking/book", {
        method: "POST",
        headers: { "Content-Type": "application/json", "RequestVerificationToken": document.querySelector("meta[name=csrf]").content },
        body: JSON.stringify({ doctorId: state.doctor.id, date: state.date, start: state.time, serviceId: service ? +service : null, name, mobile, website: form.website.value }),
      });
      const data = await res.json().catch(() => ({}));
      if (res.status === 429) throw new Error("تعداد درخواست‌ها زیاد بود. چند دقیقه بعد دوباره امتحان کنید.");
      if (!res.ok) {
        if (data.field === "slot") {
          state.time = null;
          go(2);
          loadMonth();
          await loadSlots();
          slotsBox.insertAdjacentHTML("afterbegin", `<div class="alert alert-error">${esc(data.error)}</div>`);
          return;
        }
        throw new Error(data.error || "ثبت نوبت انجام نشد. دوباره تلاش کنید.");
      }
      $("#done-code").textContent = data.trackingCode;
      $("#done-meta").textContent = `${data.doctor} · ${data.date} · ساعت ${data.time}${data.service ? " · " + data.service : ""}`;
      go(4);
    } catch (err) {
      showError(err.message);
    } finally {
      btn.disabled = false;
      btn.textContent = "ثبت نوبت";
    }
  });

  $("#again").addEventListener("click", () => {
    form.reset();
    Object.assign(state, { doctor: null, date: null, time: null, view: null, avail: {} });
    $$(".doctor").forEach((x) => x.classList.remove("is-selected"));
    $$(".step-val").forEach((x) => (x.textContent = ""));
    resetSlots();
    go(1);
  });

  renderDoctors();
})();
