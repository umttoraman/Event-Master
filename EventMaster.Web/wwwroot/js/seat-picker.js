(() => {
  const picker = document.querySelector('[data-seat-picker="1"]');
  if (!picker) return;

  const form = picker.closest("form");
  if (!form) return;

  const eventSelect = form.querySelector('select[name="EventId"]');
  const hiddenInputsHost = document.getElementById("seatHiddenInputs");
  const selectedLabelEl = document.getElementById("selectedSeatLabel");
  const totalPriceEl = document.getElementById("totalPrice");

  const state = {
    seats: [],
    selectedSeatIds: new Set(),
  };

  const UNIT_PRICE = 100; // demo; server is authoritative
  const normalizeId = (id) => String(id || "").trim().toLowerCase();

  function syncHiddenInputs() {
    if (!hiddenInputsHost) return;
    hiddenInputsHost.innerHTML = "";
    for (const id of state.selectedSeatIds) {
      const input = document.createElement("input");
      input.type = "hidden";
      input.name = "RoomSeatIds";
      input.value = id;
      hiddenInputsHost.appendChild(input);
    }
  }

  function updateSummary() {
    const selected = Array.from(state.selectedSeatIds);
    if (selectedLabelEl) selectedLabelEl.textContent = selected.length ? `${selected.length} koltuk` : "—";
    if (totalPriceEl) totalPriceEl.value = String(selected.length * UNIT_PRICE);
    syncHiddenInputs();
  }

  function renderError(message) {
    picker.innerHTML = `<div class="alert alert-warning mb-0">${message}</div>`;
  }

  function seatClass(status) {
    if (status === "sold") return "seat--sold";
    if (status === "heldByMe") return "seat--held";
    if (status === "heldByOther") return "seat--blocked";
    return "seat--available";
  }

  function seatDisabled(status) {
    return status === "sold" || status === "heldByOther";
  }

  function render() {
    if (!Array.isArray(state.seats) || state.seats.length === 0) {
      picker.innerHTML = `<div class="text-muted small">Koltuklar yükleniyor...</div>`;
      return;
    }

    // Group by Row; fall back to "—" when missing
    const rows = new Map();
    for (const s of state.seats) {
      const key = s.row || "—";
      if (!rows.has(key)) rows.set(key, []);
      rows.get(key).push(s);
    }

    const orderedRowKeys = Array.from(rows.keys()).sort((a, b) => String(a).localeCompare(String(b)));

    const html = orderedRowKeys
      .map((rowKey) => {
        const seats = rows.get(rowKey) || [];
        seats.sort((a, b) => (a.number ?? 9999) - (b.number ?? 9999));
        const buttons = seats
          .map((s) => {
            const seatId = normalizeId(s.roomSeatId);
            const isSelected = state.selectedSeatIds.has(seatId);
            const cls = seatClass(s.status);
            const disabled = seatDisabled(s.status) ? "disabled" : "";
            const selected = isSelected ? "seat--selected" : "";
            const title = `${s.label} (${s.status})`;
            return `<button type="button" class="seat ${cls} ${selected}" data-seat-id="${escapeHtml(
              s.roomSeatId
            )}" data-seat-label="${escapeHtml(s.label)}" ${disabled} title="${escapeHtml(title)}">${escapeHtml(s.label)}</button>`;
          })
          .join("");

        return `<div class="seat-row">
  <div class="seat-row__label">${escapeHtml(rowKey)}</div>
  <div class="seat-row__seats">${buttons}</div>
</div>`;
      })
      .join("");

    picker.innerHTML = `<div class="seat-rows">${html}</div>`;
  }

  function escapeHtml(str) {
    return String(str)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  async function loadSeats(eventId) {
    picker.innerHTML = `<div class="text-muted small">Koltuklar yükleniyor...</div>`;

    const res = await fetch(`/Seat/Event/${eventId}`, { headers: { Accept: "application/json" } });
    if (!res.ok) {
      renderError("Koltuklar alınamadı.");
      return;
    }
    const payload = await res.json();
    if (!payload || payload.success !== true) {
      renderError(payload?.error || "Koltuklar alınamadı.");
      return;
    }
    state.seats = payload.value || [];
    // Drop selections that no longer exist in this event map
    const existing = new Set(state.seats.map((s) => normalizeId(s.roomSeatId)));
    state.selectedSeatIds = new Set(Array.from(state.selectedSeatIds).filter((id) => existing.has(id)));
    updateSummary();
    render();
  }

  async function holdSeat(eventId, roomSeatId) {
    const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const res = await fetch(`/Seat/Hold`, {
      method: "POST",
      headers: {
        "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8",
        Accept: "application/json",
        ...(token ? { RequestVerificationToken: token } : {}),
      },
      body: new URLSearchParams({ eventId, roomSeatId }),
    });
    const payload = await res.json().catch(() => null);
    if (!res.ok || !payload || payload.success !== true) {
      throw new Error(payload?.error || "Koltuk tutulamadı.");
    }
  }

  async function releaseSeat(eventId, roomSeatId) {
    const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const res = await fetch(`/Seat/Release`, {
      method: "POST",
      headers: {
        "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8",
        Accept: "application/json",
        ...(token ? { RequestVerificationToken: token } : {}),
      },
      body: new URLSearchParams({ eventId, roomSeatId }),
    });
    const payload = await res.json().catch(() => null);
    if (!res.ok || !payload || payload.success !== true) {
      throw new Error(payload?.error || "Koltuk bırakılamadı.");
    }
  }

  picker.addEventListener("click", async (e) => {
    const btn = e.target?.closest?.("[data-seat-id]");
    if (!btn) return;

    const eventId = eventSelect?.value;
    const seatIdRaw = btn.getAttribute("data-seat-id");
    if (!eventId || !seatIdRaw) return;

    const seatId = normalizeId(seatIdRaw);

    try {
      picker.classList.add("seat-picker--busy");
      if (state.selectedSeatIds.has(seatId)) {
        await releaseSeat(eventId, seatId);
        state.selectedSeatIds.delete(seatId);
      } else {
        await holdSeat(eventId, seatId);
        state.selectedSeatIds.add(seatId);
      }
      updateSummary();
      await loadSeats(eventId); // refresh statuses (sold/held)
    } catch (err) {
      alert(err?.message || "Koltuk tutulamadı.");
      await loadSeats(eventId);
    } finally {
      picker.classList.remove("seat-picker--busy");
    }
  });

  eventSelect?.addEventListener("change", () => {
    const eventId = eventSelect.value;
    state.selectedSeatIds.clear();
    updateSummary();
    if (eventId) loadSeats(eventId);
  });

  if (eventSelect?.value) {
    loadSeats(eventSelect.value);
  } else {
    renderError("Etkinlik seçiniz.");
  }
})();

