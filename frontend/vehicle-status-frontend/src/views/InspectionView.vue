
<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'

import {
  getPendingInspections,
  claimInspection
} from '../services/inspectionApi'

// 查询状态
const loading = ref(false)
const claimingId = ref(null)

const tasks = ref([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)

// null：全部；true：旧计划时间已到；false：未到
const dueFilter = ref(null)

// 显示后端原始 UTC 时间
function formatDate(value) {
  if (!value) return '-'
  return String(value).replace('T', ' ').slice(0, 19)
}

// 加载待领取任务
async function loadTasks() {
  loading.value = true

  try {
    const params = {
      page: currentPage.value,
      pageSize: pageSize.value
    }

    if (dueFilter.value !== null) {
      params.isDue = dueFilter.value
    }

    const response = await getPendingInspections(params)

    tasks.value = response.data.items ?? []
    totalCount.value = response.data.totalCount ?? 0

  } catch (error) {
    tasks.value = []
    totalCount.value = 0

    ElMessage.error(
      error.response?.data?.message ||
      '获取验车任务失败'
    )
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  currentPage.value = 1
  loadTasks()
}

function resetSearch() {
  dueFilter.value = null
  currentPage.value = 1
  loadTasks()
}

function handlePageChange(page) {
  currentPage.value = page
  loadTasks()
}

function handleSizeChange(size) {
  pageSize.value = size
  currentPage.value = 1
  loadTasks()
}

// 领取验车任务
async function handleClaim(row) {
  const inspectionId = row.inspectionId

  try {
    await ElMessageBox.confirm(
      `确认领取车辆 ${row.plateNumber} 的验车任务吗？`,
      '领取验车任务',
      {
        type: 'warning',
        confirmButtonText: '确认领取',
        cancelButtonText: '取消'
      }
    )
  } catch {
    return
  }

  claimingId.value = inspectionId

  try {
    await claimInspection(inspectionId)

    ElMessage.success(
      `验车任务 #${inspectionId} 领取成功`
    )

    // 领取成功后，任务离开 Pending 列表
    // 如果当前页已没有记录，返回上一页
    if (
      tasks.value.length === 1 &&
      currentPage.value > 1
    ) {
      currentPage.value -= 1
    }

    await loadTasks()

  } catch (error) {
    const status = error.response?.status

    if (status === 409) {
      ElMessage.warning(
        '任务状态已变化，可能已被其他 Operator 领取'
      )

      await loadTasks()
    } else if (status === 404) {
      ElMessage.error('验车任务不存在')
      await loadTasks()
    } else if (status === 401) {
      ElMessage.error('登录已失效，请重新登录')
    } else if (status === 403) {
      ElMessage.error('当前账号没有操作权限')
    } else {
      ElMessage.error(
        error.response?.data?.message ||
        '领取失败，请稍后重试'
      )
    }
  } finally {
    claimingId.value = null
  }
}

onMounted(loadTasks)
</script>

<template>
  <div class="inspection-page">
    <div class="page-header">
      <div>
        <h2>待验车任务</h2>
        <p>查看用户归还的车辆并领取验车任务</p>
      </div>

      <el-button
        type="primary"
        :loading="loading"
        @click="loadTasks"
      >
        刷新任务
      </el-button>
    </div>

    <!-- 筛选区域 -->
    <el-card shadow="never">
      <el-form inline>
        <el-form-item label="计划时间">
          <el-select
            v-model="dueFilter"
            style="width: 190px"
          >
            <el-option label="全部待领取" :value="null" />
            <el-option label="旧计划时间已到" :value="true" />
            <el-option label="旧计划时间未到" :value="false" />
          </el-select>
        </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            @click="handleSearch"
          >
            查询
          </el-button>

          <el-button @click="resetSearch">
            重置
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 任务表格 -->
    <el-card shadow="never">
      <el-table
        v-loading="loading"
        :data="tasks"
        stripe
        border
        style="width: 100%"
        empty-text="暂无待领取验车任务"
      >
        <el-table-column
          prop="inspectionId"
          label="验车 ID"
          width="100"
        />

        <el-table-column
          prop="orderId"
          label="订单 ID"
          width="100"
        />

        <el-table-column
          prop="plateNumber"
          label="车牌号"
          min-width="120"
        />

        <el-table-column
          prop="modelName"
          label="车型"
          min-width="140"
        />

        <el-table-column
          label="归还时间 (UTC)"
          min-width="170"
        >
          <template #default="{ row }">
            {{ formatDate(row.returnedAt) }}
          </template>
        </el-table-column>

        <el-table-column
          label="原计划时间 (UTC)"
          min-width="170"
        >
          <template #default="{ row }">
            {{ formatDate(row.inspectionDueAt) }}
          </template>
        </el-table-column>

        <el-table-column
          label="计划时间判断"
          width="140"
        >
          <template #default="{ row }">
            <el-tag :type="row.isDue ? 'warning' : 'info'">
              {{ row.isDue ? '已到' : '未到' }}
            </el-tag>
          </template>
        </el-table-column>

        <el-table-column
          prop="status"
          label="任务状态"
          width="110"
        />

        <el-table-column
          label="操作"
          width="130"
          fixed="right"
        >
          <template #default="{ row }">
            <el-button
              type="primary"
              size="small"
              :loading="claimingId === row.inspectionId"
              :disabled="claimingId !== null"
              @click="handleClaim(row)"
            >
              领取任务
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination">
        <el-pagination
          :current-page="currentPage"
          :page-size="pageSize"
          :page-sizes="[5, 10, 20]"
          :total="totalCount"
          layout="total, sizes, prev, pager, next"
          @current-change="handlePageChange"
          @size-change="handleSizeChange"
        />
      </div>
    </el-card>
  </div>
</template>

<style scoped>
.inspection-page {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
}

.page-header h2 {
  margin: 0 0 8px;
}

.page-header p {
  color: #909399;
  font-size: 14px;
  margin: 0;
}

.pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
}
</style>
