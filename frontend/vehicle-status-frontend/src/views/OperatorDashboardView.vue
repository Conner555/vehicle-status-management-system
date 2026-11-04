<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import {
  getPendingInspections,
  getInspectionReminders,
  getMyInspections,
  getInspectionHistory
} from '../services/inspectionApi'

const router = useRouter()
const loading = ref(false)
const pendingCount = ref(null)
const openCount = ref(null)
const myCount = ref(null)
const historyCount = ref(null)
const pendingItems = ref([])
const myItems = ref([])
const overdueItems = ref([])
const overdueVisible = ref(false)
const lastUpdated = ref('')
const errors = ref([])
let refreshTimer = null

const cards = computed(() => [
  { key: 'open', title: 'Open 验车提醒', count: openCount.value, route: '/operator/reminders', label: '查看提醒' },
  { key: 'pending', title: '待领取验车任务', count: pendingCount.value, route: '/operator/inspections', label: '查看待验车列表' },
  { key: 'mine', title: '我的进行中任务', count: myCount.value, route: '/operator/my-inspections', label: '查看我的任务' },
  { key: 'history', title: '历史验车记录', count: historyCount.value, route: '/operator/my-inspections', label: '查看历史记录' }
])

function parseUtc(value) {
  if (!value) return null
  const text = String(value).trim().replace(' ', 'T')
  const normalized = /(?:Z|[+-]\d{2}:?\d{2})$/i.test(text) ? text : `${text}Z`
  const date = new Date(normalized)
  return Number.isNaN(date.getTime()) ? null : date
}

function formatUtc(value) {
  const date = parseUtc(value)
  if (!date) return '-'
  return date.toISOString().replace('T', ' ').slice(0, 19)
}

function isOverdue(row) {
  const deadline = parseUtc(row.inspectionDeadlineAt ?? row.deadlineAt)
  return deadline !== null && deadline.getTime() <= Date.now()
}

function buildOverdueItems(pendingRows, mineRows, escalatedRows) {
  const candidates = [
    ...pendingRows,
    ...mineRows,
    ...escalatedRows
  ]
  const map = new Map()
  for (const row of candidates) {
    const id = row.inspectionId ?? row.id
    const status = row.inspectionStatus ?? row.status
    if (id == null || !['Pending', 'InProgress'].includes(status) || !isOverdue(row)) continue
    const key = String(id)
    const existing = map.get(key)
    // 优先使用具有较多有效字段的记录
    const merged = { ...(existing || {}), ...Object.fromEntries(Object.entries(row).filter(([, value]) => value != null)) }
    map.set(key, { ...merged, inspectionId: id, inspectionStatus: status })
  }
  return [...map.values()].sort((a, b) =>
    (parseUtc(a.inspectionDeadlineAt ?? a.deadlineAt)?.getTime() ?? 0) -
    (parseUtc(b.inspectionDeadlineAt ?? b.deadlineAt)?.getTime() ?? 0))
}

function showNewOverdue(list) {
  if (list.length === 0) return
  // 每次进入页面的同一任务只自动提醒一次；仍可手动重新打开弹窗。
  // 若跨页面后重新进入，会重新提醒尚未处理的任务。
  if (list.some(item => !notifiedInThisVisit.has(String(item.inspectionId)))) {
    for (const item of list) notifiedInThisVisit.add(String(item.inspectionId))
    overdueVisible.value = true
  }
}

const notifiedInThisVisit = new Set()

async function loadDashboard() {
  if (loading.value) return
  loading.value = true
  errors.value = []
  try {
    const responses = await Promise.allSettled([
      getInspectionReminders({ status: 'Open', page: 1, pageSize: 1 }),
      getPendingInspections({ page: 1, pageSize: 5 }),
      getMyInspections({ page: 1, pageSize: 5 }),
      getInspectionHistory({ page: 1, pageSize: 1 }),
      getInspectionReminders({ status: 'Escalated', page: 1, pageSize: 100 })
    ])
    const extract = (index, label) => {
      const result = responses[index]
      if (result?.status === 'fulfilled') return result.value.data
      errors.value.push(label)
      console.error(`[Operator Dashboard] ${label} 请求失败`, result?.reason)
      return null
    }

    const open = extract(0, 'Open 提醒')
    const pending = extract(1, '待领取任务')
    const mine = extract(2, '我的进行中任务')
    const history = extract(3, '历史验车记录')
    const escalated = extract(4, '升级提醒')

    openCount.value = open?.totalCount ?? null
    pendingCount.value = pending?.totalCount ?? null
    myCount.value = mine?.totalCount ?? null
    historyCount.value = history?.totalCount ?? null
    pendingItems.value = pending?.items ?? []
    myItems.value = mine?.items ?? []

    // 仅已有数据的记录可在本地判定超时。历史 Resolved 不触发弹窗。
    const overdue = buildOverdueItems(
      pending?.items ?? [], mine?.items ?? [], escalated?.items ?? []
    )
    overdueItems.value = overdue
    showNewOverdue(overdue)

    lastUpdated.value = new Date().toLocaleTimeString()
    if (errors.value.length) {
      ElMessage.warning(`部分统计加载失败：${errors.value.join('、')}`)
    }
  } catch (error) {
    console.error('[Operator Dashboard] 加载异常', error)
    ElMessage.error('工作台数据加载失败')
  } finally {
    loading.value = false
  }
}

function go(route) {
  router.push(route)
}

function goHistory() {
  // 传入查询参数，MyInspectionsView 需读取 query.tab 并激活 history 标签
  router.push({ path: '/operator/my-inspections', query: { tab: 'history' } })
}

function refreshOverdueStatus() {
  // 刷新时也重新检查现有数据的时间是否已经到期
  const latest = buildOverdueItems(pendingItems.value, myItems.value, overdueItems.value)
  overdueItems.value = latest
  showNewOverdue(latest)
}

onMounted(() => {
  loadDashboard()
  refreshTimer = setInterval(() => {
    refreshOverdueStatus()
    loadDashboard()
  }, 60_000)
})
onUnmounted(() => {
  if (refreshTimer !== null) clearInterval(refreshTimer)
})
</script>

<template>
  <div class="operator-dashboard">
    <div class="page-header">
      <div>
        <h2>Operator 验车工作台</h2>
        <p>查看待办验车任务、计划提醒及个人历史记录</p>
      </div>
      <el-button type="primary" :loading="loading" @click="loadDashboard">刷新数据</el-button>
    </div>

    <el-alert
      v-if="errors.length"
      type="warning"
      :closable="false"
      :title="`部分接口加载失败：${errors.join('、')}`"
      class="alert"
    />

    <el-alert
      v-if="overdueItems.length"
      type="error"
      :closable="false"
      class="alert"
    >
      <template #title>
        存在 {{ overdueItems.length }} 项已超过 12 小时且尚未验车的任务
        <el-button link type="danger" @click="overdueVisible = true">查看超时详情</el-button>
      </template>
    </el-alert>

    <el-row :gutter="16" class="stat-row">
      <el-col v-for="card in cards" :key="card.key" :xs="24" :sm="12" :lg="6">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-title">{{ card.title }}</div>
          <div class="stat-value">{{ card.count ?? '-' }}</div>
          <el-button link type="primary" @click="card.key === 'history' ? goHistory() : go(card.route)">
            {{ card.label }}
          </el-button>
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="never" class="list-card">
      <template #header>
        <div class="card-header">
          <strong>最近待领取车辆</strong>
          <el-button link type="primary" @click="go('/operator/inspections')">查看全部</el-button>
        </div>
      </template>
      <el-table v-loading="loading" :data="pendingItems" border stripe empty-text="暂无待验车任务">
        <el-table-column prop="inspectionId" label="验车 ID" width="110" />
        <el-table-column prop="plateNumber" label="车牌号" min-width="130" />
        <el-table-column prop="modelName" label="车型" min-width="140" />
        <el-table-column prop="orderId" label="关联订单" width="110" />
        <el-table-column label="归还时间 (UTC)" min-width="170">
          <template #default="{ row }">{{ formatUtc(row.returnedAt) }}</template>
        </el-table-column>
        <el-table-column label="计划验车 (UTC)" min-width="170">
          <template #default="{ row }">{{ formatUtc(row.scheduledInspectionAt) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="110">
          <template #default="{ row }"><el-tag type="warning">{{ row.status }}</el-tag></template>
        </el-table-column>
        <el-table-column label="操作" width="120">
          <template #default><el-button type="primary" link @click="go('/operator/inspections')">前往领取</el-button></template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-card shadow="never" class="list-card">
      <template #header>
        <div class="card-header">
          <strong>我的进行中验车任务</strong>
          <el-button link type="primary" @click="go('/operator/my-inspections')">查看全部</el-button>
        </div>
      </template>
      <el-table v-loading="loading" :data="myItems" border stripe empty-text="当前没有进行中的验车任务">
        <el-table-column prop="inspectionId" label="验车 ID" width="110" />
        <el-table-column prop="plateNumber" label="车牌号" min-width="130" />
        <el-table-column prop="modelName" label="车型" min-width="140" />
        <el-table-column prop="orderId" label="关联订单" width="110" />
        <el-table-column label="计划验车时间 (UTC)" min-width="175">
          <template #default="{ row }">{{ formatUtc(row.scheduledInspectionAt) }}</template>
        </el-table-column>
        <el-table-column label="截止时间 (UTC)" min-width="175">
          <template #default="{ row }">{{ formatUtc(row.inspectionDeadlineAt) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="120">
          <template #default="{ row }"><el-tag type="warning">{{ row.status }}</el-tag></template>
        </el-table-column>
        <el-table-column label="操作" width="120">
          <template #default><el-button type="primary" link @click="go('/operator/my-inspections')">前往处理</el-button></template>
        </el-table-column>
      </el-table>
    </el-card>

    <div class="last-update">最近刷新：{{ lastUpdated || '-' }}</div>

    <el-dialog
      v-model="overdueVisible"
      title="验车超时提醒（超过 12 小时）"
      width="680px"
      append-to-body
    >
      <el-alert title="以下任务已超过验车截止时间，但尚未完成验车。请及时处理。" type="error" :closable="false" class="alert" />
      <el-table :data="overdueItems" border stripe max-height="350">
        <el-table-column prop="inspectionId" label="验车 ID" width="100" />
        <el-table-column prop="plateNumber" label="车牌号" min-width="120" />
        <el-table-column label="截止时间 (UTC)" min-width="175">
          <template #default="{ row }">{{ formatUtc(row.inspectionDeadlineAt ?? row.deadlineAt) }}</template>
        </el-table-column>
        <el-table-column prop="inspectionStatus" label="验车状态" min-width="120" />
      </el-table>
      <template #footer>
        <el-button @click="overdueVisible = false">关闭</el-button>
        <el-button type="primary" @click="overdueVisible = false; go('/operator/inspections')">查看待验车任务</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.operator-dashboard { display: flex; flex-direction: column; gap: 18px; }
.page-header, .card-header { display: flex; justify-content: space-between; align-items: center; gap: 16px; }
.page-header h2 { margin: 0 0 6px; }
.page-header p { margin: 0; color: #909399; font-size: 13px; }
.stat-row { row-gap: 16px; }
.stat-card { min-height: 125px; }
.stat-title { color: #909399; font-size: 13px; }
.stat-value { font-weight: 700; font-size: 27px; margin: 12px 0 6px; }
.list-card { margin-top: 2px; }
.last-update { color: #909399; text-align: right; font-size: 12px; }
.alert { margin-bottom: 2px; }
</style>
