/* ============================================================
   TursoDbDemo TUI 运行时 —— metadata 驱动的终端操作台
   对接 /api/products 的 CRUD；布局/配色致敬 AgomTUI 仪表盘。
   ============================================================ */
(() => {
  'use strict';
  const M = window.METADATA;
  const $ = (s, r = document) => r.querySelector(s);

  const state = { rows: [], selected: null, filterKW: '', view: 'products' };

  /* ---------- 值格式化 ---------- */
  function fmt(v, type) {
    if (v === null || v === undefined || v === '') return '—';
    switch (type) {
      case 'currency': return '¥' + Number(v).toFixed(2);
      case 'datetime': { const d = new Date(v); return isNaN(d) ? String(v) : d.toLocaleString('zh-CN', { hour12: false }); }
      default: return String(v);
    }
  }
  function stockClass(v) {
    const n = Number(v);
    if (isNaN(n)) return '';
    if (n < (M.kpi?.stockLow ?? 10)) return 'tui-text-red';
    if (n < (M.kpi?.stockOk ?? 50)) return 'tui-text-yellow';
    return 'tui-text-green';
  }

  /* ---------- 表头 / 表体 ---------- */
  function renderHead() {
    $('tr[data-grid-head]').innerHTML = M.columns.map(c =>
      `<th class="${c.align === 'right' ? 'tui-align-right' : ''}"${c.width ? ` style="width:${c.width}px"` : ''}>${c.label}</th>`
    ).join('');
  }
  function renderRows() {
    const tb = $('[data-grid-body]');
    const empty = $('[data-empty]');
    let rows = state.rows;
    if (state.filterKW) {
      const kw = state.filterKW.toLowerCase();
      rows = rows.filter(r =>
        String(r.name || '').toLowerCase().includes(kw) ||
        String(r.description || '').toLowerCase().includes(kw)
      );
    }
    if (!rows.length) { tb.innerHTML = ''; empty.hidden = false; }
    else {
      empty.hidden = true;
      tb.innerHTML = rows.map(r => {
        const sel = state.selected && String(r[M.primaryKey]) === String(state.selected[M.primaryKey]) ? ' is-selected' : '';
        const cells = M.columns.map(c => {
          let content = escapeHtml(fmt(r[c.key], c.type));
          if (c.type === 'stock') content = `<span class="${stockClass(r[c.key])}">${content}</span>`;
          return `<td class="${c.align === 'right' ? 'tui-align-right' : ''}">${content}</td>`;
        }).join('');
        return `<tr class="${sel.trim()}" data-id="${escapeAttr(String(r[M.primaryKey]))}">${cells}</tr>`;
      }).join('');
    }
    $('[data-pager]').textContent = `显示 ${rows.length} / 共 ${state.rows.length} 行`;
  }

  /* ---------- KPI 快照 ---------- */
  function renderKPI() {
    const rows = state.rows;
    const stock = rows.reduce((s, r) => s + (Number(r.stock) || 0), 0);
    const value = rows.reduce((s, r) => s + (Number(r.price) || 0) * (Number(r.stock) || 0), 0);
    const ok = (M.kpi?.stockOk ?? 50), low = (M.kpi?.stockLow ?? 10);
    let nOk = 0, nMid = 0, nLow = 0;
    rows.forEach(r => { const n = Number(r.stock) || 0; if (n >= ok) nOk++; else if (n >= low) nMid++; else nLow++; });
    $('[data-kpi-count]').textContent = rows.length;
    $('[data-kpi-stock]').textContent = stock;
    $('[data-kpi-value]').textContent = '¥' + value.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    $('[data-kpi-ok]').textContent = nOk;
    $('[data-kpi-mid]').textContent = nMid;
    $('[data-kpi-low]').textContent = nLow;
  }

  /* ---------- 选中 ---------- */
  function selectRow(id) {
    state.selected = state.rows.find(r => String(r[M.primaryKey]) === String(id)) || null;
    renderRows();
  }

  /* ---------- 数据加载 ---------- */
  async function load() {
    setStatus('加载中…');
    try {
      const res = await fetch(M.apiBase, { headers: { Accept: 'application/json' } });
      if (!res.ok) throw new Error('HTTP ' + res.status);
      const data = await res.json();
      state.rows = Array.isArray(data) ? data : [];
      showRaw(data);
      if (state.selected) state.selected = state.rows.find(r => String(r[M.primaryKey]) === String(state.selected[M.primaryKey])) || null;
      renderKPI();
      renderRows();
      setRefreshed();
      setStatus('就绪', 'ok');
    } catch (e) {
      setStatus('加载失败: ' + e.message, 'error');
      $('[data-conn]').textContent = 'OFFLINE';
    }
  }

  /* ---------- 表单 ---------- */
  function fieldHtml(f, value) {
    const v = value !== undefined ? escapeAttr(String(value ?? '')) : escapeAttr(String(f.default ?? ''));
    const req = f.required ? ' <span class="tui-req">*</span>' : '';
    const err = `<div class="tui-field-error" data-err="${f.name}"></div>`;
    if (f.type === 'textarea') {
      return `<div class="tui-field"><label>${f.label}${req}</label><textarea class="tui-textarea" name="${f.name}" maxlength="${f.max ?? 500}">${v}</textarea>${err}</div>`;
    }
    const type = f.type === 'number' ? 'number' : 'text';
    return `<div class="tui-field"><label>${f.label}${req}</label><input class="tui-input" type="${type}" name="${f.name}" value="${v}" ${f.step ? `step="${f.step}"` : ''} ${f.min !== undefined ? `min="${f.min}"` : ''} ${f.max ? `maxlength="${f.max}"` : ''}>${err}</div>`;
  }
  function readForm(formEl) {
    const data = {}, errors = {};
    M.form.forEach(f => {
      const el = formEl.querySelector(`[name="${f.name}"]`);
      let val = el ? el.value : '';
      if (f.type === 'number') {
        if (val === '') { if (f.required) errors[f.name] = '必填'; else val = null; }
        else { const n = Number(val); if (isNaN(n)) errors[f.name] = '需为数字'; else if (f.min !== undefined && n < f.min) errors[f.name] = `最小 ${f.min}`; val = n; }
      } else {
        if (f.required && !val.trim()) errors[f.name] = '必填';
        else if (f.max && val.length > f.max) errors[f.name] = `最长 ${f.max}`;
      }
      data[f.name] = val;
    });
    return { data, errors };
  }
  function showFormErrors(formEl, errors) {
    formEl.querySelectorAll('[data-err]').forEach(e => (e.textContent = ''));
    Object.entries(errors).forEach(([k, v]) => { const e = formEl.querySelector(`[data-err="${k}"]`); if (e) e.textContent = v; });
    const fe = formEl.querySelector('.tui-field-error:not(:empty)');
    if (fe) fe.closest('.tui-field').querySelector('input,textarea')?.focus();
  }
  function openForm({ title, values, submitLabel, onSubmit }) {
    showModal(title, `<form class="tui-form-fields" data-form>${M.form.map(f => fieldHtml(f, values ? values[f.name] : undefined)).join('')}<div class="tui-form-actions"><button type="button" class="tui-btn" data-modal-close>取消</button><button type="submit" class="tui-btn tui-btn-primary">${submitLabel}</button></div></form>`);
    $('[data-form]').addEventListener('submit', async (e) => {
      e.preventDefault();
      const { data, errors } = readForm(e.target);
      if (Object.keys(errors).length) { showFormErrors(e.target, errors); return; }
      await onSubmit(data);
    });
  }

  /* ---------- 详情 modal ---------- */
  function openDetail() {
    if (!state.selected) { setStatus('请先选中一行', 'error'); return; }
    const r = state.selected;
    const dl = M.inspector.map(f =>
      `<dt>${escapeHtml(f.label)}</dt><dd>${escapeHtml(fmt(r[f.key], f.type))}</dd>`
    ).join('');
    showModal('商品详情 / DETAIL #' + r[M.primaryKey],
      `<dl class="tui-detail-grid">${dl}</dl>
       <div class="tui-form-actions"><button class="tui-btn" data-modal-close>关闭</button><button class="tui-btn tui-btn-primary" data-act="edit">编辑</button></div>`);
  }

  /* ---------- Modal ---------- */
  function showModal(title, bodyHtml) {
    $('[data-modal-title]').textContent = title;
    $('[data-modal-body]').innerHTML = bodyHtml;
    $('[data-modal]').hidden = false;
  }
  function closeModal() { $('[data-modal]').hidden = true; }

  /* ---------- CRUD ---------- */
  async function doCreate(data) {
    const res = await fetch(M.apiBase, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) });
    if (!res.ok) { setStatus('创建失败: HTTP ' + res.status, 'error'); return; }
    const created = await res.json().catch(() => null);
    closeModal(); await load();
    if (created) selectRow(created[M.primaryKey]);
    setStatus('已创建', 'ok');
  }
  async function doEdit(data) {
    if (!state.selected) return;
    const id = state.selected[M.primaryKey];
    const res = await fetch(`${M.apiBase}/${id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) });
    if (!res.ok) { setStatus('更新失败: HTTP ' + res.status, 'error'); return; }
    closeModal(); await load(); selectRow(id); setStatus('已更新', 'ok');
  }
  async function doDelete() {
    if (!state.selected) return;
    const id = state.selected[M.primaryKey];
    const res = await fetch(`${M.apiBase}/${id}`, { method: 'DELETE' });
    if (!res.ok && res.status !== 204) { setStatus('删除失败: HTTP ' + res.status, 'error'); return; }
    closeModal(); state.selected = null; await load(); setStatus('已删除', 'ok');
  }

  /* ---------- 动作分发 ---------- */
  function handleAct(act) {
    switch (act) {
      case 'refresh': load(); break;
      case 'raw': $('[data-raw]').hidden = false; break;
      case 'filter': toggleFilter(); break;
      case 'create': openForm({ title: '新建商品 / CREATE', submitLabel: '创建', onSubmit: doCreate }); break;
      case 'detail': openDetail(); break;
      case 'edit':
        if (!state.selected) { setStatus('请先选中一行', 'error'); return; }
        openForm({ title: '编辑商品 / EDIT #' + state.selected[M.primaryKey], values: state.selected, submitLabel: '保存', onSubmit: doEdit });
        break;
      case 'delete':
        if (!state.selected) { setStatus('请先选中一行', 'error'); return; }
        confirmDelete();
        break;
    }
  }
  function confirmDelete() {
    const r = state.selected;
    showModal('确认删除 / CONFIRM DELETE',
      `<div class="tui-confirm-msg">! 危险操作：删除后不可恢复</div>
       <dl class="tui-detail-grid"><dt>ID</dt><dd>${escapeHtml(String(r.id))}</dd><dt>名称</dt><dd>${escapeHtml(r.name)}</dd></dl>
       <div class="tui-form-actions"><button class="tui-btn" data-modal-close>取消</button><button class="tui-btn tui-btn-danger" data-confirm-delete>确认删除</button></div>`);
    const b = $('[data-confirm-delete]');
    if (b) b.addEventListener('click', doDelete);
  }

  /* ---------- 视图切换 ---------- */
  function setView(view) {
    state.view = view;
    document.querySelectorAll('[data-nav]').forEach(n => n.classList.toggle('is-active', n.dataset.nav === view));
    const titleMap = { overview: '概览 / OVERVIEW', products: M.title, raw: '原始数据 / RAW' };
    $('[data-main-title]').textContent = titleMap[view] || M.title;
    $('[data-screen]').textContent = view;
    if (view === 'raw') $('[data-raw]').hidden = false;
    if (view === 'overview') setStatus('概览：参考上方 KPI 快照', 'ok');
  }

  /* ---------- 筛选 ---------- */
  function toggleFilter() { const bar = $('[data-filter]'); bar.hidden = !bar.hidden; if (!bar.hidden) $('[data-filter-input]').focus(); }
  function applyFilter() {
    state.filterKW = $('[data-filter-input]').value.trim();
    renderRows();
    setStatus(state.filterKW ? `已筛选: ${state.filterKW}` : '就绪');
  }

  /* ---------- 状态 / 工具 ---------- */
  function setStatus(msg, kind) {
    const el = $('[data-status]');
    el.textContent = msg;
    el.style.color = kind === 'error' ? 'var(--tui-red)' : kind === 'ok' ? 'var(--tui-green)' : 'var(--tui-text)';
  }
  function setRefreshed() { $('[data-refreshed]').textContent = '刷新: ' + new Date().toLocaleTimeString('zh-CN', { hour12: false }); }
  function showRaw(data) { $('[data-raw-body]').textContent = typeof data === 'string' ? data : JSON.stringify(data, null, 2); }
  function escapeHtml(s) { return String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c])); }
  function escapeAttr(s) { return String(s).replace(/"/g, '&quot;'); }

  /* ---------- 事件绑定 ---------- */
  function bind() {
    document.addEventListener('click', (e) => {
      if (e.target.closest('[data-modal-close]')) { closeModal(); return; }
      if (e.target.matches('.tui-modal')) { closeModal(); return; }
      if (e.target.closest('[data-raw-close]')) { $('[data-raw]').hidden = true; return; }
      const actEl = e.target.closest('[data-act]');
      if (actEl) { handleAct(actEl.dataset.act); return; }
      const navEl = e.target.closest('[data-nav]');
      if (navEl) { setView(navEl.dataset.nav); return; }
      const rowEl = e.target.closest('tr[data-id]');
      if (rowEl) { selectRow(rowEl.dataset.id); return; }
    });

    $('[data-filter-apply]').addEventListener('click', applyFilter);
    $('[data-filter-clear]').addEventListener('click', () => { $('[data-filter-input]').value = ''; applyFilter(); $('[data-filter]').hidden = true; });
    $('[data-filter-input]').addEventListener('keydown', (e) => { if (e.key === 'Enter') applyFilter(); });

    document.addEventListener('keydown', (e) => {
      const tag = (e.target.tagName || '').toLowerCase();
      const typing = tag === 'input' || tag === 'textarea';
      if (e.key === 'Escape') {
        if (!$('[data-modal]').hidden) { closeModal(); return; }
        if (!$('[data-filter]').hidden) { $('[data-filter]').hidden = true; return; }
        if (!$('[data-raw]').hidden) { $('[data-raw]').hidden = true; return; }
      }
      if (typing) return;
      if (e.key === 'F5') { e.preventDefault(); load(); }
      else if (e.key === 'F7') { e.preventDefault(); toggleFilter(); }
      else if (e.key === 'n' || e.key === 'N') handleAct('create');
      else if (e.key === 'e' || e.key === 'E') handleAct('edit');
      else if (e.key === 'Delete') handleAct('delete');
      else if (e.key === 'Enter' && state.selected) openDetail();
    });

    setInterval(() => { $('[data-clock]').textContent = new Date().toLocaleString('zh-CN', { hour12: false }); }, 1000);
  }

  /* ---------- 启动 ---------- */
  function init() {
    renderHead();
    bind();
    setView('products');
    load();
  }
  document.addEventListener('DOMContentLoaded', init);
})();
