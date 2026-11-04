
<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import api from '../services/api'
import OrderDetailDrawer from '../components/OrderDetailDrawer.vue'

// 订单列表
const orders = ref([])
const loading = ref(false)

// 查询参数
const selectedStatus = ref('')
const currentPage = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

const detailVisible = ref(false)
const selectedOrderId = ref(null)

// 订单状态
const statusOptions = [
  { label: '全部状态', value: '' },
  { label: '待支付', value: 'PendingPayment' },
  { label: '已确认', value: 'Confirmed' },
  { label: '使用中', value: 'InUse' },
  { label: '待验车', value: 'PendingInspection' },
  { label: '已完成', value: 'Completed' },
  { label: '已取消', value: 'Cancelled' },
  { label: '已过期', value: 'Expired' }
]

// 获取订单
async function loadOrders() {
  loading.value = true

  try {
    const params = {
      page: currentPage.value,
      pageSize: pageSize.value
    }

    if (selectedStatus.value) {
      params.status = selectedStatus.value
    }

    const response = await api.get(
      '/admin/rental-orders',
      { params }
    )

    orders.value = response.data.items ?? []
    totalCount.value = response.data.totalCount ?? 0

  } catch (error) {
    console.error('Load orders failed:', error)

    ElMessage.error(
      error.response?.data?.message ||
      '订单加载失败'
    )
  } finally {
    loading.value = false
  }
}

// 筛选
function handleSearch() {
  currentPage.value = 1
  loadOrders()
}

// 重置
function resetSearch() {
  selectedStatus.value = ''
  currentPage.value = 1
  loadOrders()
}

// 分页
function handlePageChange(page) {
  currentPage.value = page
  loadOrders()
}

function handleSizeChange(size) {
  pageSize.value = size
  currentPage.value = 1
  loadOrders()
}

// 金额
function formatMoney(value) {
  return `¥${Number(value ?? 0).toFixed(2)}`
}

//查看详细方法
function handleViewDetail(row) {
  selectedOrderId.value = row.orderId
  detailVisible.value = true
}

// 状态颜色
function getStatusType(status) {
  const types = {
    Completed: 'success',
    Confirmed: 'primary',
    InUse: 'warning',
    Cancelled: 'danger',
    Expired: 'info',
    PendingPayment: 'info',
    PendingInspection: 'warning'
  }

  return types[status] || 'info'
}

onMounted(loadOrders)
</script>

<template>
  <div class="orders-page">

    <!-- 页面标题 -->
    <div class="page-header">
      <div>
        <h2>租赁订单管理</h2>
        <p>查看和管理系统中的车辆租赁订单</p>
      </div>

      <el-button
        type="primary"
        :loading="loading"
        @click="loadOrders"
      >
        刷新数据
      </el-button>
    </div>

    <!-- 查询区域 -->
    <el-card shadow="never" class="filter-card">
      <el-form inline>
        <el-form-item label="订单状态">
          <el-select
            v-model="selectedStatus"
            style="width: 180px"
          >
            <el-option
              v-for="item in statusOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
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

    <!-- 订单列表 -->
    <el-card shadow="never">
      <el-table
        v-loading="loading"
        :data="orders"
        stripe
        border
        style="width: 100%"
      >
        <el-table-column
          prop="orderId"
          label="订单编号"
          width="110"
        />

        <el-table-column
          prop="customerName"
          label="客户"
          min-width="130"
        />

        <el-table-column
          prop="vehicleCount"
          label="车辆数量"
          width="110"
        />

        <el-table-column
          label="订单金额"
          width="140"
        >
          <template #default="{ row }">
            {{ formatMoney(row.totalAmount) }}
          </template>
        </el-table-column>

        <el-table-column
          label="订单状态"
          width="150"
        >
          <template #default="{ row }">
            <el-tag
              :type="getStatusType(row.orderStatus)"
            >
              {{ row.orderStatus }}
            </el-tag>
          </template>
        </el-table-column>

        <el-table-column
          prop="paymentStatus"
          label="支付状态"
          width="130"
        />

        <el-table-column
          prop="createdAt"
          label="创建时间"
          min-width="190"
        />

        <el-table-column
          label="操作"
          width="120"
          fixed="right"
        >
          <template #default="{ row }">
            <el-button
              link
              type="primary"
              @click="handleViewDetail(row)"
            >
              查看详情
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination">
        <el-pagination
          :current-page="currentPage"
          :page-size="pageSize"
          :page-sizes="[5, 10, 20, 50]"
          :total="totalCount"
          layout="total, sizes, prev, pager, next"
          @current-change="handlePageChange"
          @size-change="handleSizeChange"
        />
      </div>
    </el-card>

    <OrderDetailDrawer
    v-model="detailVisible"
    :order-id="selectedOrderId"
    />
    
  </div>
</template>

<style scoped>
.orders-page {
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

.filter-card :deep(.el-card__body) {
  padding-bottom: 0;
}

.pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
}
</style>
