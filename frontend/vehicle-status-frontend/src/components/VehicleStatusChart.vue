
<script setup>
import {
  ref,
  watch,
  onMounted,
  onUnmounted
} from 'vue'

import * as echarts from 'echarts'

// 1. 接收父组件传入的历史记录
const props = defineProps({
  records: {
    type: Array,
    default: () => []
  }
})

// 2. 图表 DOM 引用
const chartRef = ref(null)

// 3. ECharts 实例
let chartInstance = null

// 4. 当前选择的指标
const selectedMetric = ref('temperature')

// 5. 指标配置
const metricConfig = {
  temperature: {
    name: 'Temperature',
    unit: '°C'
  },
  speed: {
    name: 'Speed',
    unit: 'km/h'
  },
  battery: {
    name: 'Battery',
    unit: '%'
  }
}

// 6. 日期显示格式
function formatTime(time) {
  if (!time) return ''

  // 保留原始日期的年月日、时分秒
  // 不主动进行时区转换
  return String(time)
    .replace('T', ' ')
    .split('.')[0]
}

// 7. 生成图表
function renderChart() {
  if (!chartInstance) return

  const sortedRecords = [...props.records].sort(
    (a, b) => {
      const timeA = String(a.recordTime ?? '')
      const timeB = String(b.recordTime ?? '')

      const result = timeA.localeCompare(timeB)

      return result !== 0
        ? result
        : (a.id ?? 0) - (b.id ?? 0)
    }
  )

  const metric = selectedMetric.value

  const config = metricConfig[metric]

  const chartData = sortedRecords.map(record => [
  record.recordTime,
  Number(record[metric])
])

  const times = sortedRecords.map(record =>
    formatTime(record.recordTime)
  )

  const values = sortedRecords.map(record =>
    Number(record[metric])
  )

    const option = {
    tooltip: {
        trigger: 'axis',
        formatter(params) {
        if (!params || params.length === 0) return ''

        const item = params[0]
        const time = formatTime(item.value[0])
        const value = item.value[1]

        return `
            <div>
            <div><strong>${config.name}</strong></div>
            <div>Time: ${time}</div>
            <div>Value: ${value} ${config.unit}</div>
            </div>
        `
        }
    },

    grid: {
        left: 70,
        right: 30,
        top: 40,
        bottom: 80,
        containLabel: true
    },

    xAxis: {
        type: 'time',
        name: 'Time',
        nameLocation: 'middle',
        nameGap: 55,
        axisLabel: {
        formatter(value) {
            const date = new Date(value)
            const month = String(date.getMonth() + 1).padStart(2, '0')
            const day = String(date.getDate()).padStart(2, '0')
            const hour = String(date.getHours()).padStart(2, '0')
            const minute = String(date.getMinutes()).padStart(2, '0')
            return `${month}-${day}\n${hour}:${minute}`
        }
        }
    },

    yAxis: {
        type: 'value',
        name: `${config.name} (${config.unit})`,
        nameLocation: 'middle',
        nameGap: 50,
        scale: true
    },

    series: [
        {
        name: config.name,
        type: 'line',
        data: chartData,
        smooth: false,
        showSymbol: true,
        symbol: 'circle',
        symbolSize: 8,
        lineStyle: {
            width: 2
        },
        label: {
            show: true,
            position: 'top',
            formatter(params) {
            return params.value[1]
            }
        }
        }
    ]
    }

  chartInstance.setOption(option, true)
}

// 8. 监听父组件历史数据变化
watch(
  () => props.records,
  () => {
    renderChart()
  },
  { deep: true }
)

// 9. 监听指标切换
watch(selectedMetric, () => {
  renderChart()
})

// 10. 组件挂载后初始化图表
onMounted(() => {
  if (!chartRef.value) return

  chartInstance = echarts.init(chartRef.value)

  renderChart()

  window.addEventListener('resize', handleResize)
})

// 11. 窗口大小变化
function handleResize() {
  chartInstance?.resize()
}

// 12. 组件卸载时释放资源
onUnmounted(() => {
  window.removeEventListener('resize', handleResize)

  chartInstance?.dispose()
  chartInstance = null
})
</script>

<template>
  <el-card class="chart-card">

    <template #header>
      <div class="chart-header">
        <strong>Vehicle Status Trends</strong>

        <el-select
          v-model="selectedMetric"
          style="width: 170px"
        >
          <el-option
            label="Temperature"
            value="temperature"
          />

          <el-option
            label="Speed"
            value="speed"
          />

          <el-option
            label="Battery"
            value="battery"
          />
        </el-select>
      </div>
    </template>

    <div
      v-if="records.length === 0"
      class="empty-chart"
    >
      No status records available
    </div>

    <div
      ref="chartRef"
      class="chart-container"
      v-show="records.length > 0"
    ></div>

  </el-card>
</template>

<style scoped>
.chart-card {
  margin-top: 20px;
}

.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.chart-container {
  width: 100%;
  height: 360px;
}

.empty-chart {
  text-align: center;
  padding: 50px 0;
  color: #909399;
}
</style>
