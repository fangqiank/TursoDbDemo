/* ============================================================
   Product 实体元数据 —— 前端运行时据此渲染（致敬 AgomTUI 的 metadata 驱动）。
   想加字段/改阈值，改这里即可，不必动 app.js。
   ============================================================ */
window.METADATA = {
  screen: 'products',
  title: '商品列表 / PRODUCTS',
  apiBase: '/api/products',
  primaryKey: 'id',

  // 数据表列
  columns: [
    { key: 'id',          label: 'ID',          width: 60, align: 'right' },
    { key: 'name',        label: 'NAME' },
    { key: 'description', label: 'DESCRIPTION' },
    { key: 'price',       label: 'PRICE',  align: 'right', type: 'currency' },
    { key: 'stock',       label: 'STOCK',  align: 'right', type: 'stock' },
    { key: 'updatedAt',   label: 'UPDATED', type: 'datetime' },
  ],

  // 详情面板字段
  inspector: [
    { key: 'id',          label: 'ID' },
    { key: 'name',        label: '名称 NAME' },
    { key: 'description', label: '描述 DESCRIPTION' },
    { key: 'price',       label: '价格 PRICE',  type: 'currency' },
    { key: 'stock',       label: '库存 STOCK',  type: 'stock' },
    { key: 'createdAt',   label: '创建时间 CREATED', type: 'datetime' },
    { key: 'updatedAt',   label: '更新时间 UPDATED', type: 'datetime' },
  ],

  // 新建/编辑表单字段
  form: [
    { name: 'name',        label: '名称 Name',        type: 'text',     required: true, max: 200 },
    { name: 'description', label: '描述 Description', type: 'textarea',                 max: 2000 },
    { name: 'price',       label: '价格 Price',       type: 'number',   required: true, step: 0.01, min: 0 },
    { name: 'stock',       label: '库存 Stock',       type: 'number',   min: 0, default: 0 },
  ],

  // KPI / 库存分级阈值
  kpi: {
    stockOk: 50,   // ≥ 视为充足
    stockLow: 10,  // < 视为紧急
  },
};
