
<script setup>
import { ref, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'

import {
  getMyInspections,
  completeInspection,
  getInspectionHistory
} from '../services/inspectionApi'

const route = useRoute()

const tasks = ref([])
const loading = ref(false)
const submitting = ref(false)

const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

const dialogVisible = ref(false)
const selectedTask = ref(null)

const resultForm = ref({
  result: 'Passed',
  resultNotes: ''
})

//历史记录
const activeTab = ref('active')

const historyList = ref([])
const historyLoading = ref(false)
const historyError = ref('')

const historyPage = ref(1)
const historyPageSize = ref(10)
const historyTotal = ref(0)

const historyResult = ref('')

// 历史详情弹窗
const historyDetailVisible = ref(false)
const selectedHistory = ref(null)



async function loadHistory() {
  historyLoading.value = true
  historyError.value = ''

  try {
    const params = {
      page: historyPage.value,
      pageSize: historyPageSize.value
    }

    if (historyResult.value) {
      params.result = historyResult.value
    }

    const response = await getInspectionHistory(params)

    historyList.value = response.data.items ?? []
    historyTotal.value = response.data.totalCount ?? 0

  } catch (error) {
    historyList.value = []
    historyTotal.value = 0

    historyError.value =
      error.response?.data?.message ||
      '历史验车记录查询失败'

    ElMessage.error(historyError.value)

  } finally {
    historyLoading.value = false
  }
}

function changeTab(value) {
  if (value === 'history') {
    loadHistory()
  } else {
    loadTasks()
  }
}

function searchHistory() {
  historyPage.value = 1
  loadHistory()
}

function resetHistory() {
  historyResult.value = ''
  historyPage.value = 1
  loadHistory()
}

function changeHistoryPage(value) {
  historyPage.value = value
  loadHistory()
}

function changeHistoryPageSize(value) {
  historyPageSize.value = value
  historyPage.value = 1
  loadHistory()
}

function openHistoryDetail(row) {
  selectedHistory.value = row
  historyDetailVisible.value = true
}


//查看验车信息
async function loadTasks() {
  loading.value = true

  try {
    const response = await getMyInspections({
      page: page.value,
      pageSize: pageSize.value
    })

    tasks.value = response.data.items ?? []
    totalCount.value = response.data.totalCount ?? 0
  } catch (error) {
    tasks.value = []
    totalCount.value = 0

    ElMessage.error(
      error.response?.data?.message ||
      '加载我的验车任务失败'
    )
  } finally {
    loading.value = false
  }
}

function openCompleteDialog(row) {
  selectedTask.value = row

  resultForm.value = {
    result: 'Passed',
    resultNotes: ''
  }

  dialogVisible.value = true
}


async function submitInspection() {
  // 1. 防止无任务或重复提交
  if (!selectedTask.value || submitting.value) {
    return
  }

  const inspectionId = selectedTask.value.inspectionId
  const result = resultForm.value.result
  const notes = resultForm.value.resultNotes.trim()

  // 2. 校验验车结果
  if (!['Passed', 'Failed'].includes(result)) {
    ElMessage.warning('请选择正确的验车结果')
    return
  }

  if (result === 'Failed' && !notes) {
    ElMessage.warning('验车未通过时必须填写原因')
    return
  }

  if (notes.length > 500) {
    ElMessage.warning('验车备注不能超过500字')
    return
  }

  // 3. 二次确认
  try {
    await ElMessageBox.confirm(
      `确认提交验车 #${inspectionId} 的 ${result} 结果吗？`,
      '确认提交',
      {
        type: 'warning',
        confirmButtonText: '确认',
        cancelButtonText: '取消'
      }
    )
  } catch {
    return
  }

  // 4. 调用后端接口
  submitting.value = true

  let completed = false

  try {
    await completeInspection(inspectionId, {
      result: result,
      resultNotes: notes || null
    })

    completed = true
    dialogVisible.value = false

    ElMessage.success(
      `验车 #${inspectionId} 已提交：${result}`
    )

  } catch (error) {
    const status = error.response?.status
    const message = error.response?.data?.message

    if (status === 400) {
      ElMessage.error(message || '验车参数不正确')
    } else if (status === 401) {
      ElMessage.error('登录已失效')
    } else if (status === 403) {
      ElMessage.error('该任务不属于当前 Operator')
    } else if (status === 404) {
      ElMessage.error('验车任务不存在')
    } else if (status === 409) {
      ElMessage.warning(
        message || '验车任务状态发生冲突'
      )
    } else {
      ElMessage.error(
        message || '提交请求失败，请检查后台状态'
      )
    }

  } finally {
    submitting.value = false
  }

  // 5. 成功后重新获取数据库中的任务
  if (completed) {
    if (tasks.value.length === 1 && page.value > 1) {
      page.value -= 1
    }

    await loadTasks()
  }
}


function changePage(value) {
  page.value = value
  loadTasks()
}

function formatDate(value) {
  return value
    ? String(value).replace('T', ' ').slice(0, 19)
    : '-'
}

onMounted(() => {
  if (route.query.tab === 'history') {
    activeTab.value = 'history'
    loadHistory()
  } else {
    loadTasks()
  }
})

// 从 Dashboard 点击“历史验车记录”时，兼容同一组件的路由复用。
watch(() => route.query.tab, (tab) => {
  if (tab === 'history' && activeTab.value !== 'history') {
    activeTab.value = 'history'
    loadHistory()
  }
})
</script>

<template>
  <div class="my-inspections">
    <div class="page-header">
      <div>
        <h2>我的验车任务</h2>
        <p>查看自己已领取的进行中任务和历史验车结果</p>
      </div>

      <el-button
        type="primary"
        :loading="activeTab === 'history' ? historyLoading : loading"
        @click="activeTab === 'history' ? loadHistory() : loadTasks()"
      >
        刷新
      </el-button>
    </div>

    <el-card shadow="never">
      <el-tabs v-model="activeTab" @tab-change="changeTab">
        <el-tab-pane label="进行中任务" name="active">
          <el-table
            v-loading="loading"
            :data="tasks"
            stripe
            border
            empty-text="暂无进行中的验车任务"
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
              min-width="175"
            >
              <template #default="{ row }">
                {{ formatDate(row.returnedAt) }}
              </template>
            </el-table-column>

            <el-table-column
              label="计划验车 (UTC)"
              min-width="175"
            >
              <template #default="{ row }">
                {{ formatDate(row.scheduledInspectionAt) }}
              </template>
            </el-table-column>

            <el-table-column
              label="截止时间 (UTC)"
              min-width="175"
            >
              <template #default="{ row }">
                {{ formatDate(row.inspectionDeadlineAt) }}
              </template>
            </el-table-column>

            <el-table-column
              prop="status"
              label="任务状态"
              width="125"
            >
              <template #default="{ row }">
                <el-tag type="warning">
                  {{ row.status }}
                </el-tag>
              </template>
            </el-table-column>

            <el-table-column
              label="操作"
              width="130"
              fixed="right"
            >
              <template #default="{ row }">
                <el-button
                  type="primary"
                  size="small"
                  @click="openCompleteDialog(row)"
                >
                  提交验车
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination">
            <el-pagination
              :current-page="page"
              :page-size="pageSize"
              :total="totalCount"
              layout="total, prev, pager, next"
              @current-change="changePage"
            />
          </div>
        </el-tab-pane>
        <el-tab-pane label="历史验车记录" name="history">
          <div class="history-toolbar">
            <el-select
              v-model="historyResult"
              placeholder="验车结果"
              style="width: 180px"
              @change="searchHistory"
            >
              <el-option label="全部结果" value="" />
              <el-option label="通过 Passed" value="Passed" />
              <el-option label="未通过 Failed" value="Failed" />
            </el-select>
            <el-button type="primary" :loading="historyLoading" @click="searchHistory">查询</el-button>
            <el-button @click="resetHistory">重置</el-button>
          </div>

          <el-alert
            v-if="historyError"
            :title="historyError"
            type="error"
            :closable="false"
            class="history-error"
          />

          <el-table
            v-loading="historyLoading"
            :data="historyList"
            border
            stripe
            empty-text="暂无历史验车记录"
          >
            <el-table-column prop="inspectionId" label="验车 ID" width="100" />
            <el-table-column prop="orderId" label="订单 ID" width="100" />
            <el-table-column prop="plateNumber" label="车牌号" min-width="120" />
            <el-table-column prop="modelName" label="车型" min-width="140" />
            <el-table-column label="验车结果" width="130">
              <template #default="{ row }">
                <el-tag :type="row.status === 'Passed' ? 'success' : 'danger'">
                  {{ row.status === 'Passed' ? '通过' : '未通过' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="完成时间 (UTC)" min-width="175">
              <template #default="{ row }">
                {{ formatDate(row.inspectedAt) }}
              </template>
            </el-table-column>
            <el-table-column prop="resultNotes" label="验车备注" min-width="180" show-overflow-tooltip>
              <template #default="{ row }">{{ row.resultNotes || '-' }}</template>
            </el-table-column>
            <el-table-column label="操作" width="120" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" @click="openHistoryDetail(row)">查看详情</el-button>
              </template>
            </el-table-column>
          </el-table>

          <div class="pagination">
            <el-pagination
              :current-page="historyPage"
              :page-size="historyPageSize"
              :page-sizes="[5, 10, 20]"
              :total="historyTotal"
              layout="total, sizes, prev, pager, next"
              @current-change="changeHistoryPage"
              @size-change="changeHistoryPageSize"
            />
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <el-dialog
      v-model="dialogVisible"
      title="提交车辆验车结果"
      width="500px"
      :close-on-click-modal="!submitting"
      :close-on-press-escape="!submitting"
      :show-close="!submitting"
    >
      <el-form label-width="100px">
        <el-form-item label="车辆">
          {{ selectedTask?.plateNumber }}
        </el-form-item>

        <el-form-item label="验车结果">
          <el-radio-group v-model="resultForm.result">
            <el-radio value="Passed">通过</el-radio>
            <el-radio value="Failed">未通过</el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item label="验车备注">
          <el-input
            v-model="resultForm.resultNotes"
            type="textarea"
            :rows="4"
            maxlength="500"
            show-word-limit
            placeholder="记录车辆检查情况"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button
          :disabled="submitting"
          @click="dialogVisible = false"
        >
          取消
        </el-button>

        <el-button
          type="primary"
          :loading="submitting"
          @click="submitInspection"
        >
          确认提交
        </el-button>
      </template>
    </el-dialog>

    <!-- 历史验车详情：只读，不调用 CompleteAsync -->
    <el-drawer v-model="historyDetailVisible" title="历史验车详情" size="45%">
      <el-descriptions v-if="selectedHistory" :column="1" border>
        <el-descriptions-item label="验车 ID">{{ selectedHistory.inspectionId }}</el-descriptions-item>
        <el-descriptions-item label="订单 ID">{{ selectedHistory.orderId }}</el-descriptions-item>
        <el-descriptions-item label="订单明细 ID">{{ selectedHistory.orderItemId }}</el-descriptions-item>
        <el-descriptions-item label="车辆">
          {{ selectedHistory.plateNumber }} / {{ selectedHistory.modelName }}
        </el-descriptions-item>
        <el-descriptions-item label="验车结果">
          <el-tag :type="selectedHistory.status === 'Passed' ? 'success' : 'danger'">
            {{ selectedHistory.status === 'Passed' ? '通过' : '未通过' }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="归还时间 (UTC)">
          {{ formatDate(selectedHistory.returnedAt) }}
        </el-descriptions-item>
        <el-descriptions-item label="计划验车时间 (UTC)">
          {{ formatDate(selectedHistory.scheduledInspectionAt) }}
        </el-descriptions-item>
        <el-descriptions-item label="验车截止时间 (UTC)">
          {{ formatDate(selectedHistory.inspectionDeadlineAt) }}
        </el-descriptions-item>
        <el-descriptions-item label="实际验车时间 (UTC)">
          {{ formatDate(selectedHistory.inspectedAt) }}
        </el-descriptions-item>
        <el-descriptions-item label="验车备注">
          {{ selectedHistory.resultNotes || '无备注' }}
        </el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="historyDetailVisible = false">关闭</el-button>
      </template>
    </el-drawer>
  </div>
</template>

<style scoped>
.my-inspections {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.page-header h2 {
  margin: 0 0 8px;
}

.page-header p {
  margin: 0;
  color: #909399;
  font-size: 14px;
}

.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

.history-toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  margin: 14px 0 18px;
}

.history-error {
  margin-bottom: 12px;
}
</style>
