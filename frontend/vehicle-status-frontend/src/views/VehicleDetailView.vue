
<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import api from '../services/api'
import { fi } from 'element-plus/es/locales.mjs'

import VehicleStatusChart from 
    '../components/VehicleStatusChart.vue'

// 1. 获取路由对象
const route = useRoute()
const router = useRouter()

// 2. 定义响应式变量
const vehicle = ref(null)
const loading = ref(false)
const statusRecords = ref([])
const historyLoading = ref(false)

//3.新增管理员增加删除历史记录

const currentRole = ref(
  localStorage.getItem('role') || ''
)

const dialogVisible = ref(false)
const saving = ref(false)

const statusForm = ref({
  temperature: null,
  speed: null,
  battery: null
})

// 3. 查询车辆详情
async function loadVehicleDetail() {
  loading.value = true

  try {
    const id = route.params.id

    const response = await api.get(
      `/vehicles/${id}`
    )

    vehicle.value = response.data
  }
  catch (error) {
    if (error.response?.status === 404) {
      ElMessage.error('Vehicle not found.')
    }
    else if (error.response?.status === 401) {
      ElMessage.error('Please login again.')
      router.replace('/login')
    }
    else {
      ElMessage.error('Failed to load vehicle.')
    }
  }
  finally {
    loading.value = false
  }
}

//加载历史数据内容
async function loadStatusHistory() {
  historyLoading.value = true

  try {
    const vehicleId = route.params.id

    const response = await api.get(
      `/vehicles/${vehicleId}/status`
    )

    statusRecords.value = response.data

    console.log('Status History:', response.data)
  }
  catch (error) {
    console.error('Load status history failed:', error)

    statusRecords.value = []

    ElMessage.error('Failed to load status history.')
  }
  finally {
    historyLoading.value = false
  }
}

//新增车辆状态信息

async function addStatusRecord() {
  const form = statusForm.value

  if (
    form.temperature === null ||
    form.speed === null ||
    form.battery === null
  ) {
    ElMessage.warning('Please complete all fields.')
    return
  }

  if (
    form.temperature < -100 || form.temperature > 300 ||
    form.speed < 0 || form.speed > 500 ||
    form.battery < 0 || form.battery > 100
  ) {
    ElMessage.warning('Invalid status values.')
    return
  }

  saving.value = true

  try {
    await api.post(
      `/vehicles/${route.params.id}/status`,
      {
        temperature: form.temperature,
        speed: form.speed,
        battery: form.battery
      }
    )

    dialogVisible.value = false

    statusForm.value = {
      temperature: null,
      speed: null,
      battery: null
    }

    ElMessage.success('Status record added successfully.')

    await loadStatusHistory()
  }
  catch (error) {
    console.error('Add status record failed:', error)

    if (error.response?.status === 403) {
      ElMessage.error('Administrator permission required.')
    }
    else if (error.response?.status === 400) {
      ElMessage.error('Invalid status data.')
    }
    else {
      ElMessage.error('Failed to add status record.')
    }
  }
  finally {
    saving.value = false
  }
}


// 4. 返回车辆列表
function goBack() {
  router.push('/vehicles')
}

// 5. 页面加载时查询

onMounted(async () => {
  await loadVehicleDetail()

  if (vehicle.value) {
    await loadStatusHistory()
  }
})

</script>

<template>
  <div v-loading="loading" class="detail-page">

    <el-button @click="goBack">
      Back to Vehicles
    </el-button>

    <h2>Vehicle Details</h2>

    <el-card v-if="vehicle">
      <el-descriptions
        title="Vehicle Information"
        :column="1"
        border
      >
        <el-descriptions-item label="ID">
          {{ vehicle.id }}
        </el-descriptions-item>

        <el-descriptions-item label="Plate Number">
          {{ vehicle.plateNumber }}
        </el-descriptions-item>

        <el-descriptions-item label="Model">
          {{ vehicle.model }}
        </el-descriptions-item>

        <el-descriptions-item label="Status">
          {{ vehicle.status }}
        </el-descriptions-item>

        <el-descriptions-item label="Created At">
          {{ vehicle.createTime }}
        </el-descriptions-item>
      </el-descriptions>
    </el-card>

    <el-empty
      v-else-if="!loading"
      description="Vehicle not found"
    />


    <el-card
    v-if="vehicle"
    class="history-card"
    >


    <template #header>
    <div class="history-header">
        <strong>Vehicle Status History</strong>

        <el-button
        v-if="currentRole === 'Admin'"
        type="primary"
        @click="dialogVisible = true"
        >
        Add Status Record
        </el-button>
    </div>
    </template>

<!-- //historyvehiclesrecords车辆记录显示 -->
    <el-table
        v-loading="historyLoading"
        :data="statusRecords"
        style="width: 100%"
    >
        <el-table-column
        prop="recordTime"
        label="Record Time"
        min-width="200"
        />

        <el-table-column
        prop="temperature"
        label="Temperature (°C)"
        />

        <el-table-column
        prop="speed"
        label="Speed (km/h)"
        />

        <el-table-column
        prop="battery"
        label="Battery (%)"
        />

        <template #empty>
        <el-empty
            description="No status records found"
        />
        </template>
    </el-table>
    </el-card>

<!-- //addVehiclerecords车辆增加新记录 -->

    <el-dialog
    v-model="dialogVisible"
    title="Add Vehicle Status Record"
    width="480px"
    >
    <el-form label-width="130px">

        <el-form-item label="Temperature">
        <el-input-number
            v-model="statusForm.temperature"
            :min="-100"
            :max="300"
            :precision="2"
            style="width: 100%"
        />
        </el-form-item>

        <el-form-item label="Speed">
        <el-input-number
            v-model="statusForm.speed"
            :min="0"
            :max="500"
            :precision="2"
            style="width: 100%"
        />
        </el-form-item>

        <el-form-item label="Battery">
        <el-input-number
            v-model="statusForm.battery"
            :min="0"
            :max="100"
            :precision="2"
            style="width: 100%"
        />
        </el-form-item>

    </el-form>

    <template #footer>
        <el-button @click="dialogVisible = false">
        Cancel
        </el-button>

        <el-button
        type="primary"
        :loading="saving"
        @click="addStatusRecord"
        >
        Save
        </el-button>
    </template>
    </el-dialog>

    <VehicleStatusChart
    v-if="vehicle"
    :records="statusRecords"
    />


  </div>
</template>

<style scoped>

.detail-page {
  padding: 24px;
}

.history-card {
  margin-top: 20px;
}

</style>
