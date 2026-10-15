/* تقویم شمسی: تبدیل تاریخ، رندر ماه و انتخاب‌گر تاریخ — بدون وابستگی */
(function () {
  "use strict";

  const div = (a, b) => ~~(a / b);
  const mod = (a, b) => a - ~~(a / b) * b;

  function jalCal(jy) {
    const breaks = [-61, 9, 38, 199, 426, 686, 756, 818, 1111, 1181, 1210, 1635, 2060, 2097, 2192, 2262, 2324, 2394, 2456, 3178];
    const gy = jy + 621;
    let leapJ = -14, jp = breaks[0], jump = 0;
    for (let i = 1; i < breaks.length; i++) {
      const jm = breaks[i];
      jump = jm - jp;
      if (jy < jm) break;
      leapJ += div(jump, 33) * 8 + div(mod(jump, 33), 4);
      jp = jm;
    }
    let n = jy - jp;
    leapJ += div(n, 33) * 8 + div(mod(n, 33) + 3, 4);
    if (mod(jump, 33) === 4 && jump - n === 4) leapJ += 1;
    const leapG = div(gy, 4) - div((div(gy, 100) + 1) * 3, 4) - 150;
    const march = 20 + leapJ - leapG;
    if (jump - n < 6) n = n - jump + div(jump + 4, 33) * 33;
    let leap = mod(mod(n + 1, 33) - 1, 4);
    if (leap === -1) leap = 4;
    return { leap, gy, march };
  }

  function g2d(gy, gm, gd) {
    let d = div((gy + div(gm - 8, 6) + 100100) * 1461, 4) + div(153 * mod(gm + 9, 12) + 2, 5) + gd - 34840408;
    d = d - div(div(gy + 100100 + div(gm - 8, 6), 100) * 3, 4) + 752;
    return d;
  }

  function d2g(jdn) {
    let j = 4 * jdn + 139361631;
    j = j + div(div(4 * jdn + 183187720, 146097) * 3, 4) * 4 - 3908;
    const i = div(mod(j, 1461), 4) * 5 + 308;
    const gd = div(mod(i, 153), 5) + 1;
    const gm = mod(div(i, 153), 12) + 1;
    const gy = div(j, 1461) - 100100 + div(8 - gm, 6);
    return { gy, gm, gd };
  }

  function j2d(jy, jm, jd) {
    const r = jalCal(jy);
    return g2d(r.gy, 3, r.march) + (jm - 1) * 31 - div(jm, 7) * (jm - 7) + jd - 1;
  }

  function d2j(jdn) {
    const gy = d2g(jdn).gy;
    let jy = gy - 621;
    const r = jalCal(jy);
    const jdn1f = g2d(gy, 3, r.march);
    let k = jdn - jdn1f, jm, jd;
    if (k >= 0) {
      if (k <= 185) {
        jm = 1 + div(k, 31);
        jd = mod(k, 31) + 1;
        return { jy, jm, jd };
      }
      k -= 186;
    } else {
      jy -= 1;
      k += 179;
      if (r.leap === 1) k += 1;
    }
    jm = 7 + div(k, 30);
    jd = mod(k, 30) + 1;
    return { jy, jm, jd };
  }

  const pad = (n) => String(n).padStart(2, "0");
  const parseIso = (iso) => iso.split("-").map(Number);
  const isoOf = (g) => `${g.gy}-${pad(g.gm)}-${pad(g.gd)}`;

  const months = ["فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"];
  const days = ["شنبه", "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه"];
  const daysShort = ["ش", "ی", "د", "س", "چ", "پ", "ج"];

  const J = {
    months, days, daysShort,
    toJ(iso) { const [y, m, d] = parseIso(iso); return d2j(g2d(y, m, d)); },
    toG(jy, jm, jd) { return isoOf(d2g(j2d(jy, jm, jd))); },
    monthLength(jy, jm) { return jm <= 6 ? 31 : jm <= 11 ? 30 : (jalCal(jy).leap === 0 ? 30 : 29); },
    weekIndex(iso) { const [y, m, d] = parseIso(iso); return (new Date(y, m - 1, d).getDay() + 1) % 7; },
    addDays(iso, n) { const [y, m, d] = parseIso(iso); return isoOf(d2g(g2d(y, m, d) + n)); },
    todayIso() { const t = new Date(); return `${t.getFullYear()}-${pad(t.getMonth() + 1)}-${pad(t.getDate())}`; },
    format(iso) { const j = J.toJ(iso); return `${j.jy}/${pad(j.jm)}/${pad(j.jd)}`; },
    long(iso) { const j = J.toJ(iso); return `${days[J.weekIndex(iso)]} ${j.jd} ${months[j.jm - 1]} ${j.jy}`; },
    dayMonth(iso) { const j = J.toJ(iso); return `${j.jd} ${months[j.jm - 1]}`; },
    shiftMonth(jy, jm, delta) { let m = jm + delta, y = jy; while (m > 12) { m -= 12; y++; } while (m < 1) { m += 12; y--; } return { jy: y, jm: m }; },
    monthRange(jy, jm) { return { from: J.toG(jy, jm, 1), to: J.toG(jy, jm, J.monthLength(jy, jm)) }; },
  };

  /**
   * رندر یک ماه شمسی.
   * opts: { jy, jm, today, cell(iso, el) — برای افزودن کلاس/محتوا، onPick(iso) }
   */
  J.renderGrid = function (grid, opts) {
    grid.innerHTML = "";
    for (const d of daysShort) {
      const h = document.createElement("div");
      h.className = "jcal-dow";
      h.textContent = d;
      grid.appendChild(h);
    }
    const first = J.toG(opts.jy, opts.jm, 1);
    const lead = J.weekIndex(first);
    for (let i = 0; i < lead; i++) {
      const e = document.createElement("div");
      e.className = "jcal-day jcal-empty";
      grid.appendChild(e);
    }
    const len = J.monthLength(opts.jy, opts.jm);
    const today = opts.today || J.todayIso();
    for (let d = 1; d <= len; d++) {
      const iso = J.toG(opts.jy, opts.jm, d);
      const btn = document.createElement("button");
      btn.type = "button";
      btn.className = "jcal-day";
      btn.dataset.date = iso;
      btn.innerHTML = `<span>${d}</span>`;
      btn.setAttribute("aria-label", J.long(iso));
      if (iso === today) btn.classList.add("is-today");
      if (J.weekIndex(iso) === 6) btn.classList.add("is-friday");
      const pickable = opts.cell ? opts.cell(iso, btn) !== false : true;
      if (!pickable) btn.disabled = true;
      else if (opts.onPick) btn.addEventListener("click", () => opts.onPick(iso, btn));
      grid.appendChild(btn);
    }
  };

  /** انتخاب‌گر تاریخ برای فرم‌های پنل: <div class="jdate" data-jdate> + input hidden */
  J.picker = function (root) {
    const hidden = root.querySelector("input[type=hidden]");
    const label = root.querySelector(".jdate-label");
    const pop = root.querySelector(".jdate-pop");
    const btn = root.querySelector(".jdate-btn");
    const allowEmpty = root.hasAttribute("data-allow-empty");
    let view;

    function setLabel() {
      label.textContent = hidden.value ? J.long(hidden.value) : (label.dataset.placeholder || "انتخاب تاریخ");
    }
    function draw() {
      pop.querySelector(".jcal-title").textContent = `${months[view.jm - 1]} ${view.jy}`;
      J.renderGrid(pop.querySelector(".jcal-grid"), {
        jy: view.jy, jm: view.jm,
        cell(iso, el) {
          el.classList.add("is-pickable");
          if (iso === hidden.value) el.classList.add("is-selected");
        },
        onPick(iso) { choose(iso); },
      });
    }
    function choose(iso) {
      hidden.value = iso;
      setLabel();
      root.classList.remove("open");
      hidden.dispatchEvent(new Event("change", { bubbles: true }));
      if (root.hasAttribute("data-submit")) root.closest("form").requestSubmit();
    }
    function open() {
      const base = J.toJ(hidden.value || J.todayIso());
      view = { jy: base.jy, jm: base.jm };
      draw();
      root.classList.add("open");
    }

    btn.addEventListener("click", () => root.classList.contains("open") ? root.classList.remove("open") : open());
    pop.querySelector("[data-prev]").addEventListener("click", () => { view = J.shiftMonth(view.jy, view.jm, -1); draw(); });
    pop.querySelector("[data-next]").addEventListener("click", () => { view = J.shiftMonth(view.jy, view.jm, 1); draw(); });
    pop.querySelector("[data-today]").addEventListener("click", () => choose(J.todayIso()));
    const clear = pop.querySelector("[data-clear]");
    if (clear) {
      if (!allowEmpty) clear.remove();
      else clear.addEventListener("click", () => { hidden.value = ""; setLabel(); root.classList.remove("open"); if (root.hasAttribute("data-submit")) root.closest("form").requestSubmit(); });
    }
    document.addEventListener("click", (e) => { if (!root.contains(e.target)) root.classList.remove("open"); });
    document.addEventListener("keydown", (e) => { if (e.key === "Escape") root.classList.remove("open"); });
    setLabel();
  };

  window.J = J;
  document.addEventListener("DOMContentLoaded", () => document.querySelectorAll("[data-jdate]").forEach(J.picker));
})();
