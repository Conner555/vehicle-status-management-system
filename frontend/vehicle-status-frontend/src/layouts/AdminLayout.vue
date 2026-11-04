<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessageBox } from 'element-plus'

const route = useRoute()
const router = useRouter()

const username = computed(
  () => localStorage.getItem('username') || 'Admin'
)

const currentPath = computed(() => route.path)

const menuItems = [
  {
    path: '/admin/dashboard',
    title: '运营总览'
  },
  {
    path: '/admin/orders',
    title: '租赁订单'
  },
  {
    path: '/admin/inspections',
    title: '验车管理'
  },
  {
    path: '/vehicles',
    title: '车辆管理'
  }
]

async function logout() {
  try {
    await ElMessageBox.confirm(
      '确定退出当前账号吗？',
      '退出登录',
      {
        confirmButtonText: '退出',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
  } catch {
    return
  }

  localStorage.removeItem('token')
  localStorage.removeItem('username')
  localStorage.removeItem('role')

  router.replace('/login')
}
</script>

<template>
  <el-container class="admin-layout">
    <!-- 左侧导航 -->
    <el-aside width="220px" class="sidebar">
      <div class="logo">
        <span class="logo-mark">F</span>
        <span>FleetFlow</span>
      </div>

      <div class="menu-label">运营管理</div>

      <el-menu
        router
        :default-active="currentPath"
        background-color="#172439"
        text-color="#cbd5e1"
        active-text-color="#ffffff"
        class="side-menu"
      >
        <el-menu-item
          v-for="item in menuItems"
          :key="item.path"
          :index="item.path"
        >
          <span>{{ item.title }}</span>
        </el-menu-item>
      </el-menu>
    </el-aside>

    <!-- 右侧内容 -->
    <el-container class="content-container">
      <el-header class="top-header">
        <div class="header-title">
          <strong>车辆租赁与运营管理系统</strong>
          <span>FleetFlow Administration</span>
        </div>

        <div class="user-area">
          <el-tag type="primary" effect="plain">
            Admin
          </el-tag>

          <span class="username">{{ username }}</span>

          <el-button text @click="logout">
            退出登录
          </el-button>
        </div>
      </el-header>

      <el-main class="main-content">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<style scoped>
.admin-layout {
  height: 100vh;
  min-height: 600px;
}

.sidebar {
  background-color: #172439;
  display: flex;
  flex-direction: column;
}

.logo {
  height: 72px;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 0 20px;
  color: white;
  font-size: 21px;
  font-weight: 700;
}

.logo-mark {
  width: 34px;
  height: 34px;
  border-radius: 9px;
  background: #3179b9;
  display: flex;
  align-items: center;
  justify-content: center;
}

.menu-label {
  color: #8ca1b8;
  font-size: 12px;
  padding: 20px 22px 8px;
}

.side-menu {
  border-right: none;
}

.side-menu :deep(.el-menu-item.is-active) {
  background: #304765;
  border-radius: 6px;
  margin: 0 10px;
}

.content-container {
  min-width: 0;
}

.top-header {
  height: 72px;
  background: white;
  border-bottom: 1px solid #e6eaf0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.header-title {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.header-title span {
  font-size: 12px;
  color: #909399;
}

.user-area {
  display: flex;
  align-items: center;
  gap: 14px;
}

.username {
  font-size: 14px;
  color: #374151;
}

.main-content {
  background: #f5f7fa;
  overflow: auto;
  min-width: 0;
  padding: 24px;
}

@media (max-width: 700px) {
  .top-header {
    padding: 0 12px;
  }

  .header-title span,
  .username {
    display: none;
  }

  .main-content {
    padding: 12px;
  }
}
</style>
