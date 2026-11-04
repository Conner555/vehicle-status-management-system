<script setup>
import { ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import api from '../services/api'

// 父组件传入是否打开、订单 ID
const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false
  },
  orderId: {
    type: Number,
    default: null
  }
})

const emit = defineEmits(['update:modelValue'])

const loading = ref(false)
const order = ref(null)

function closeDrawer() {
  emit('update:modelValue', false)
}

function formatMoney(value) {
  if (value === null || value === undefined) {
    return '-'
  }
  return `¥${Number(value).toFixed(2)}`
}

function formatDate(value) {
  if (!value) return '-'

  // API 的日期一般是 UTC 时间字符串。
  // 当前先保留原始值，避免错误的时区转换。
  return String(value).replace('T', ' ')
}

function statusType(status) {
  const map = {
    Completed: 'success',
    Passed: 'success',
    Succeeded: 'success',
    InUse: 'warning',
    PendingInspection: 'warning',
    InProgress: 'warning',
    Confirmed: 'primary',
    Cancelled: 'danger',
    Failed: 'danger',
    Expired: 'info'
  }

  return map[status] || 'info'
}
//退款记录函数
function getRefundReason(reason) {
  const map = {
    Cancellation: '取消订单退款',
    EarlyReturn: '提前归还退款'
  }

  return map[reason] || reason || '-'
}

// 根据订单明细 ID 查找关联车辆
function getRefundVehicle(orderItemId) {
  if (orderItemId == null) {
    return '整笔订单'
  }

  const item = order.value?.items?.find(
    x => x.itemId === orderItemId
  )

  if (!item) {
    return `明细 #${orderItemId}`
  }

  return `${item.modelName} / ${item.plateNumber}`
}


async function loadDetail(id) {
  if (!id) return

  loading.value = true
  order.value = null

  try {
    const response = await api.get(
      `/admin/rental-orders/${id}`
    )

    // 防止快速切换订单时旧响应覆盖新订单
    if (props.orderId === id && props.modelValue) {
      order.value = response.data
    }
  } catch (error) {
    if (props.orderId === id && props.modelValue) {
      ElMessage.error(
        error.response?.data?.message ||
        '订单详情加载失败'
      )
    }
  } finally {
    loading.value = false
  }
}

// 每次打开或切换订单 ID 时加载
watch(
  () => [props.modelValue, props.orderId],
  ([visible, id]) => {
    if (visible && id) {
      loadDetail(id)
    } else {
      order.value = null
    }
  },
  { immediate: true }
)
</script>

<template>
  <el-drawer
    :model-value="modelValue"
    :title="`租赁订单详情 #${orderId ?? ''}`"
    size="65%"
    @close="closeDrawer"
  >
    <div v-loading="loading" class="detail-content">
      <template v-if="order">
        <!-- 订单基本信息 -->
        <h3>订单基本信息</h3>

        <el-descriptions
          :column="2"
          border
          class="section"
        >
          <el-descriptions-item label="订单编号">
            {{ order.orderId }}
          </el-descriptions-item>

          <el-descriptions-item label="客户">
            {{ order.customerName }}
          </el-descriptions-item>

          <el-descriptions-item label="订单金额">
            {{ formatMoney(order.totalAmount) }}
          </el-descriptions-item>

          <el-descriptions-item label="订单状态">
            <el-tag :type="statusType(order.orderStatus)">
              {{ order.orderStatus }}
            </el-tag>
          </el-descriptions-item>

          <el-descriptions-item label="创建时间">
            {{ formatDate(order.createdAt) }}
          </el-descriptions-item>

          <el-descriptions-item label="过期时间">
            {{ formatDate(order.expiresAt) }}
          </el-descriptions-item>
        </el-descriptions>

        <!-- 车辆明细 -->
        <h3>车辆租赁明细</h3>

        <el-table
          :data="order.items ?? []"
          border
          stripe
          class="section"
          style="width: 100%"
        >
          <el-table-column
            prop="itemId"
            label="明细 ID"
            width="90"
          />

          <el-table-column
            prop="plateNumber"
            label="车牌号"
            min-width="110"
          />

          <el-table-column
            prop="modelName"
            label="车型"
            min-width="130"
          />

          <el-table-column
            prop="rentalDays"
            label="租赁天数"
            width="100"
          />

          <el-table-column
            label="日租金"
            width="110"
          >
            <template #default="{ row }">
              {{ formatMoney(row.dailyRate) }}
            </template>
          </el-table-column>

          <el-table-column
            label="小计"
            width="110"
          >
            <template #default="{ row }">
              {{ formatMoney(row.subtotal) }}
            </template>
          </el-table-column>

          <el-table-column
            label="明细状态"
            width="140"
          >
            <template #default="{ row }">
              <el-tag :type="statusType(row.itemStatus)">
                {{ row.itemStatus }}
              </el-tag>
            </template>
          </el-table-column>
        </el-table>

        <!-- 每辆车的详细信息 -->
        <h3>车辆履约与验车信息</h3>

        <el-collapse class="section">
          <el-collapse-item
            v-for="item in order.items ?? []"
            :key="item.itemId"
            :name="item.itemId"
            :title="`${item.modelName} / ${item.plateNumber}`"
          >
            <el-descriptions :column="2" border>
              <el-descriptions-item label="车辆 ID">
                {{ item.vehicleId }}
              </el-descriptions-item>

              <el-descriptions-item label="运营状态">
                <el-tag :type="statusType(item.operationStatus)">
                  {{ item.operationStatus }}
                </el-tag>
              </el-descriptions-item>

              <el-descriptions-item label="租赁开始">
                {{ formatDate(item.startAt) }}
              </el-descriptions-item>

              <el-descriptions-item label="租赁结束">
                {{ formatDate(item.endAt) }}
              </el-descriptions-item>

              <el-descriptions-item label="实际归还">
                {{ formatDate(item.actualReturnedAt) }}
              </el-descriptions-item>

              <el-descriptions-item label="租金小计">
                {{ formatMoney(item.subtotal) }}
              </el-descriptions-item>
            </el-descriptions>

            <h4>验车信息</h4>

            <el-descriptions
              v-if="item.inspection"
              :column="2"
              border
            >
              <el-descriptions-item label="验车 ID">
                {{ item.inspection.inspectionId }}
              </el-descriptions-item>

              <el-descriptions-item label="验车状态">
                <el-tag
                  :type="statusType(item.inspection.status)"
                >
                  {{ item.inspection.status }}
                </el-tag>
              </el-descriptions-item>

              <el-descriptions-item label="归还时间">
                {{ formatDate(item.inspection.returnedAt) }}
              </el-descriptions-item>

              <el-descriptions-item label="原验车时间字段">
                {{ formatDate(item.inspection.inspectionDueAt) }}
              </el-descriptions-item>

              <el-descriptions-item label="完成验车">
                {{ formatDate(item.inspection.inspectedAt) }}
              </el-descriptions-item>

              <el-descriptions-item label="验车备注">
                {{ item.inspection.resultNotes || '-' }}
              </el-descriptions-item>
            </el-descriptions>

            <el-empty
              v-else
              :image-size="50"
              description="暂无验车记录"
            />

            <h4>维修记录</h4>

            <el-descriptions
              v-if="item.maintenance"
              :column="2"
              border
            >
              <el-descriptions-item label="维修单 ID">
                {{ item.maintenance.maintenanceId }}
              </el-descriptions-item>

              <el-descriptions-item label="维修状态">
                {{ item.maintenance.status }}
              </el-descriptions-item>

              <el-descriptions-item label="优先级">
                {{ item.maintenance.priority }}
              </el-descriptions-item>

              <el-descriptions-item label="维修费用">
                {{ formatMoney(item.maintenance.repairCost) }}
              </el-descriptions-item>

              <el-descriptions-item label="维修备注">
                {{ item.maintenance.repairNotes || '-' }}
              </el-descriptions-item>

              <el-descriptions-item label="验证备注">
                {{ item.maintenance.verificationNotes || '-' }}
              </el-descriptions-item>
            </el-descriptions>

            <el-empty
              v-else
              :image-size="50"
              description="暂无维修记录"
            />
          </el-collapse-item>
        </el-collapse>

        <!-- 支付记录 -->
        <h3>支付记录</h3>

        <el-table
          :data="order.payments ?? []"
          border
          stripe
          class="section"
        >
          <el-table-column
            prop="paymentId"
            label="支付 ID"
            width="100"
          />

          <el-table-column
            prop="paymentNo"
            label="支付流水号"
            min-width="240"
            show-overflow-tooltip
          />

          <el-table-column
            label="金额"
            width="130"
          >
            <template #default="{ row }">
              {{ formatMoney(row.amount) }}
            </template>
          </el-table-column>

          <el-table-column
            prop="method"
            label="支付方式"
            width="130"
          />

          <el-table-column
            label="支付状态"
            width="140"
          >
            <template #default="{ row }">
              <el-tag :type="statusType(row.paymentStatus)">
                {{ row.paymentStatus }}
              </el-tag>
            </template>
          </el-table-column>

          <el-table-column
            label="支付时间"
            min-width="190"
          >
            <template #default="{ row }">
              {{ formatDate(row.paidAt) }}
            </template>
          </el-table-column>
        </el-table>

        <!-- 退款记录 -->
        <h3>退款记录</h3>

        <el-table
        :data="order.refunds ?? []"
        border
        stripe
        class="section"
        style="width: 100%"
        empty-text="暂无退款记录"
        >
        <el-table-column
            prop="refundId"
            label="退款 ID"
            width="90"
        />

        <el-table-column
            label="退款类型"
            width="150"
        >
            <template #default="{ row }">
            <el-tag
                :type="
                row.reason === 'Cancellation'
                    ? 'danger'
                    : 'warning'
                "
            >
                {{ getRefundReason(row.reason) }}
            </el-tag>
            </template>
        </el-table-column>

        <el-table-column
            label="关联车辆"
            min-width="180"
        >
            <template #default="{ row }">
            {{ getRefundVehicle(row.orderItemId) }}
            </template>
        </el-table-column>

        <el-table-column
            prop="refundNo"
            label="退款流水号"
            min-width="240"
            show-overflow-tooltip
        />

        <el-table-column
            label="退款金额"
            width="140"
        >
            <template #default="{ row }">
            <span class="refund-amount">
                {{ formatMoney(row.amount) }}
            </span>
            </template>
        </el-table-column>

        <el-table-column
            label="退款状态"
            width="120"
        >
            <template #default="{ row }">
            <el-tag :type="statusType(row.status)">
                {{ row.status }}
            </el-tag>
            </template>
        </el-table-column>

        <el-table-column
            label="退款时间"
            min-width="190"
        >
            <template #default="{ row }">
            {{ formatDate(row.refundedAt) }}
            </template>
        </el-table-column>
        </el-table>
      </template>

      <el-empty
        v-else-if="!loading"
        description="暂无订单详情"
      />
    </div>
  </el-drawer>
</template>

<style scoped>
.detail-content {
  min-height: 200px;
  padding-bottom: 30px;
}

.refund-amount {
  color: #d9534f;
  font-weight: 600;
}

.detail-content h3 {
  margin: 22px 0 14px;
  font-size: 16px;
}

.detail-content h4 {
  margin: 20px 0 10px;
  font-size: 14px;
}

.section {
  margin-bottom: 22px;
}
</style>
