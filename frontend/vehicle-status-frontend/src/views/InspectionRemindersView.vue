
<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'

import {
  getInspectionReminders
} from '../services/inspectionApi'

const router = useRouter()

const loading = ref(false)
const reminders = ref([])

const status = ref('Open')
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

const detailVisible = ref(false)
const selectedReminder = ref(null)
const detailLoading = ref(false)

const statusOptions = [
  { label: 'Open 待处理', value: 'Open' },
  { label: 'Escalated 已升级', value: 'Escalated' },
  { label: 'Resolved 已解决', value: 'Resolved' }
]

function formatDate(value) {
  if (!value) return '-'
  return String(value).replace('T', ' ').slice(0, 19)
}

function statusType(value) {
  const map = {
    Open: 'warning',
    Escalated: 'danger',
    Resolved: 'success'
  }
  return map[value] || 'info'
}

function statusLabel(value) {
  const map = {
    Open: '待处理提醒',
    Escalated: '已升级',
    Resolved: '已解决'
  }
  return map[value] || value
}

async function loadReminders() {
  loading.value = true

  try {
    const response = await getInspectionReminders({
      status: status.value,
      page: page.value,
      pageSize: pageSize.value
    })

    reminders.value = response.data.items ?? []
    totalCount.value = response.data.totalCount ?? 0

  } catch (error) {
    reminders.value = []
    totalCount.value = 0

    ElMessage.error(
      error.response?.data?.message ||
      '验车提醒加载失败'
    )
  } finally {
    loading.value = false
  }
}

function changeStatus(value) {
  status.value = value
  page.value = 1
  loadReminders()
}

function changePage(value) {
  page.value = value
  loadReminders()
}

function changePageSize(value) {
  pageSize.value = value
  page.value = 1
  loadReminders()
}

function openDetail(row) {
  selectedReminder.value = row
  detailVisible.value = true
}

function goToInspection(row) {
  detailVisible.value = false

  // 根据当前验车任务的实际状态跳转
  if (row.inspectionStatus === 'Pending') {
    router.push('/operator/inspections')
  } else if (row.inspectionStatus === 'InProgress') {
    router.push('/operator/my-inspections')
  } else {
    ElMessage.info('该验车任务已结束')
  }
}

onMounted(loadReminders)
</script>

<template>
  <div class="reminder-page">
    <div class="page-header">
      <div>
        <h2>验车提醒管理</h2>
        <p>查看计划提醒、升级提醒及历史事件</p>
      </div>

      <el-button
        type="primary"
        :loading="loading"
        @click="loadReminders"
      >
        刷新数据
      </el-button>
    </div>

    <el-card shadow="never">
      <el-tabs
        v-model="status"
        @tab-change="changeStatus"
      >
        <el-tab-pane
          v-for="item in statusOptions"
          :key="item.value"
          :label="item.label"
          :name="item.value"
        />
      </el-tabs>

      <el-table
        v-loading="loading"
        :data="reminders"
        border
        stripe
        empty-text="当前状态下暂无提醒"
      >
        <el-table-column
          prop="id"
          label="提醒 ID"
          width="95"
        />

        <el-table-column
          prop="inspectionId"
          label="验车 ID"
          width="95"
        />

        <el-table-column
          prop="plateNumber"
          label="车牌号"
          min-width="115"
        />

        <el-table-column
          label="计划时间 (UTC)"
          min-width="175"
        >
          <template #default="{ row }">
            {{ formatDate(row.scheduledAt) }}
          </template>
        </el-table-column>

        <el-table-column
          label="12小时截止 (UTC)"
          min-width="175"
        >
          <template #default="{ row }">
            {{ formatDate(row.deadlineAt) }}
          </template>
        </el-table-column>

        <el-table-column
          label="检测时间 (UTC)"
          min-width="175"
        >
          <template #default="{ row }">
            {{ formatDate(row.detectedAt) }}
          </template>
        </el-table-column>

        <el-table-column
          label="事件状态"
          width="140"
        >
          <template #default="{ row }">
            <el-tag :type="statusType(row.status)">
              {{ statusLabel(row.status) }}
            </el-tag>
          </template>
        </el-table-column>

        <el-table-column
          label="验车状态"
          width="125"
        >
          <template #default="{ row }">
            {{ row.inspectionStatus }}
          </template>
        </el-table-column>

        <el-table-column
          label="操作"
          width="120"
          fixed="right"
        >
          <template #default="{ row }">
            <el-button
              link
              type="primary"
              @click="openDetail(row)"
            >
              查看详情
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination">
        <el-pagination
          :current-page="page"
          :page-size="pageSize"
          :page-sizes="[5, 10, 20]"
          :total="totalCount"
          layout="total, sizes, prev, pager, next"
          @current-change="changePage"
          @size-change="changePageSize"
        />
      </div>
    </el-card>

    <!-- 提醒详情 -->
    <el-drawer
      v-model="detailVisible"
      title="验车提醒详情"
      size="45%"
    >
      <el-descriptions
        v-if="selectedReminder"
        :column="1"
        border
      >
        <el-descriptions-item label="提醒 ID">
          {{ selectedReminder.id }}
        </el-descriptions-item>

        <el-descriptions-item label="验车 ID">
          {{ selectedReminder.inspectionId }}
        </el-descriptions-item>

        <el-descriptions-item label="订单明细 ID">
          {{ selectedReminder.orderItemId }}
        </el-descriptions-item>

        <el-descriptions-item label="车辆 ID">
          {{ selectedReminder.vehicleId }}
        </el-descriptions-item>

        <el-descriptions-item label="车牌号">
          {{ selectedReminder.plateNumber }}
        </el-descriptions-item>

        <el-descriptions-item label="计划时间 (UTC)">
          {{ formatDate(selectedReminder.scheduledAt) }}
        </el-descriptions-item>

        <el-descriptions-item label="截止时间 (UTC)">
          {{ formatDate(selectedReminder.deadlineAt) }}
        </el-descriptions-item>

        <el-descriptions-item label="提醒创建时间 (UTC)">
          {{ formatDate(selectedReminder.detectedAt) }}
        </el-descriptions-item>

        <el-descriptions-item label="提醒状态">
          <el-tag :type="statusType(selectedReminder.status)">
            {{ statusLabel(selectedReminder.status) }}
          </el-tag>
        </el-descriptions-item>

        <el-descriptions-item label="任务状态">
          {{ selectedReminder.inspectionStatus }}
        </el-descriptions-item>

        <el-descriptions-item label="负责 Operator">
          {{ selectedReminder.operatorId ?? '尚未分配' }}
        </el-descriptions-item>

        <el-descriptions-item label="解决时间 (UTC)">
          {{ formatDate(selectedReminder.resolvedAt) }}
        </el-descriptions-item>
      </el-descriptions>

      <template #footer>
        <el-button @click="detailVisible = false">
          关闭
        </el-button>

        <el-button
          v-if="
            selectedReminder &&
            ['Pending', 'InProgress'].includes(
              selectedReminder.inspectionStatus
            )
          "
          type="primary"
          @click="goToInspection(selectedReminder)"
        >
          前往验车任务
        </el-button>
      </template>
    </el-drawer>
  </div>
</template>

<style scoped>
.reminder-page {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.page-header h2 {
  margin: 0 0 8px;
}

.page-header p {
  color: #909399;
  margin: 0;
  font-size: 14px;
}

.pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
}
</style>
