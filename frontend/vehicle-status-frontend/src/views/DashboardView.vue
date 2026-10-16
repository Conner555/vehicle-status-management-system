
<script setup>
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import * as echarts from 'echarts'
import api from '../services/api'

const router = useRouter()

const loading = ref(false)
const summary = ref({
  totalVehicles: 0,
  activeVehicles: 0,
  inactiveVehicles: 0,
  totalRecords: 0
})

const chartRef = ref(null)
let chartInstance = null

// 获取真实数据库统计数据
async function loadDashboard() {
  loading.value = true

  try {
    const response = await api.get('/dashboard/summary')
    summary.value = response.data

    await nextTick()
    renderChart()
  }
  catch (error) {
    console.error('Dashboard error:', error)

    if (error.response?.status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('username')
      localStorage.removeItem('role')
      router.replace('/login')
    } else {
      ElMessage.error('Failed to load dashboard.')
    }
  }
  finally {
    loading.value = false
  }
}

// 绘制车辆状态分布饼图
function renderChart() {
  if (!chartRef.value) return

  if (!chartInstance) {
    chartInstance = echarts.init(chartRef.value)
  }

  chartInstance.setOption({
    tooltip: {
      trigger: 'item',
      formatter: '{b}: {c} ({d}%)'
    },

    legend: {
      bottom: 0
    },

    series: [
      {
        name: 'Vehicle Status',
        type: 'pie',
        radius: ['40%', '65%'],
        center: ['50%', '45%'],
        label: {
          formatter: '{b}: {c}'
        },
        data: [
          {
            name: 'Active',
            value: summary.value.activeVehicles
          },
          {
            name: 'Inactive',
            value: summary.value.inactiveVehicles
          }
        ]
      }
    ]
  })
}

function handleResize() {
  chartInstance?.resize()
}

onMounted(() => {
  loadDashboard()
  window.addEventListener('resize', handleResize)
})

onUnmounted(() => {
  window.removeEventListener('resize', handleResize)
  chartInstance?.dispose()
  chartInstance = null
})
</script>

<template>
  <div class="dashboard-page" v-loading="loading">

    <div class="dashboard-header">
      <h2>Vehicle Dashboard</h2>

      <div>
        <el-button @click="router.push('/vehicles')">
          Vehicles
        </el-button>
        <el-button
          type="primary"
          :loading="loading"
          @click="loadDashboard"
        >
          Refresh
        </el-button>
      </div>
    </div>

    <el-row :gutter="20">

      <el-col :xs="24" :sm="12" :lg="6">
        <el-card class="stat-card">
          <el-statistic
            title="Total Vehicles"
            :value="summary.totalVehicles"
          />
        </el-card>
      </el-col>

      <el-col :xs="24" :sm="12" :lg="6">
        <el-card class="stat-card">
          <el-statistic
            title="Active Vehicles"
            :value="summary.activeVehicles"
          />
        </el-card>
      </el-col>

      <el-col :xs="24" :sm="12" :lg="6">
        <el-card class="stat-card">
          <el-statistic
            title="Inactive Vehicles"
            :value="summary.inactiveVehicles"
          />
        </el-card>
      </el-col>

      <el-col :xs="24" :sm="12" :lg="6">
        <el-card class="stat-card">
          <el-statistic
            title="Status Records"
            :value="summary.totalRecords"
          />
        </el-card>
      </el-col>

    </el-row>

    <el-card class="chart-card">
      <template #header>
        <strong>Vehicle Status Distribution</strong>
      </template>

      <div ref="chartRef" class="pie-chart"></div>
    </el-card>

  </div>
</template>

<style scoped>
.dashboard-page {
  padding: 24px;
}

.dashboard-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}

.stat-card {
  margin-bottom: 20px;
}

.chart-card {
  margin-top: 12px;
}

.pie-chart {
  width: 100%;
  height: 380px;
}
</style>
