
<script setup>
import { useRouter } from 'vue-router'

const router = useRouter()

const menus = [
  {
    path: '/operator/dashboard',
    label: '工作台'
  },
  {
    path: '/operator/inspections',
    label: '待验车任务'
  },
  {
    path: '/operator/my-inspections',
    label: '我的验车任务'
  },
  {
    path: '/operator/reminders',
    label: '验车提醒'
  }
]

function logout() {
  localStorage.removeItem('token')
  localStorage.removeItem('role')
  localStorage.removeItem('username')

  router.replace('/login')
}
</script>

<template>
  <el-container class="operator-layout">
    <el-aside width="220px" class="sidebar">
      <div class="logo">
        FleetFlow
      </div>

      <div class="section-title">
        Operator 工作台
      </div>

      <el-menu
        router
        background-color="#172439"
        text-color="#cbd5e1"
        active-text-color="#ffffff"
        :default-active="$route.path"
      >
        <el-menu-item
          v-for="menu in menus"
          :key="menu.path"
          :index="menu.path"
        >
          {{ menu.label }}
        </el-menu-item>
      </el-menu>
    </el-aside>

    <el-container>
      <el-header class="header">
        <strong>车辆验车与运营工作台</strong>

        <div class="user-info">
          <el-tag type="success">
            Operator
          </el-tag>

          <span>
            {{ localStorageUsername }}
          </span>

          <el-button text @click="logout">
            退出登录
          </el-button>
        </div>
      </el-header>

      <el-main class="content">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script>
export default {
  computed: {
    localStorageUsername() {
      return localStorage.getItem('username') || 'Operator'
    }
  }
}
</script>

<style scoped>
.operator-layout {
  min-height: 100vh;
}

.sidebar {
  background: #172439;
}

.logo {
  height: 70px;
  padding: 22px;
  box-sizing: border-box;
  color: white;
  font-size: 21px;
  font-weight: 700;
}

.section-title {
  padding: 18px 20px 10px;
  color: #94a3b8;
  font-size: 12px;
}

.sidebar :deep(.el-menu) {
  border-right: none;
}

.header {
  height: 70px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: white;
  border-bottom: 1px solid #e5e7eb;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 14px;
}

.content {
  background: #f5f7fa;
  min-width: 0;
  padding: 24px;
}
</style>
