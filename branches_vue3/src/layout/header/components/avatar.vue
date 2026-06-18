<template>
  <el-dropdown>
    <span class="el-dropdown-link">
      <el-avatar shape="square" :size="40" :src="squareUrl" />
      &nbsp;&nbsp;{{ currentUser?.username || '未登录' }}
      <el-icon class="el-icon--right">
        <arrow-down />
      </el-icon>
    </span>
    <template #dropdown>
      <el-dropdown-menu>
        <el-dropdown-item>
          <router-link :to="{name:'个人中心'}">个人中心</router-link>
        </el-dropdown-item>
        <el-dropdown-item @click="logout">安全退出</el-dropdown-item>
      </el-dropdown-menu>
    </template>
  </el-dropdown>
</template>

<script setup>
import { ArrowDown } from '@element-plus/icons-vue'
import requestUtil,{getServerUrl,getMediaUrl} from '@/util/request'
import router from '@/router'
import  store from '@/store'

const currentUserStr = sessionStorage.getItem("currentUser")
const currentUser = currentUserStr ? JSON.parse(currentUserStr) : {}

const squareUrl=getMediaUrl('userAvatar/'+currentUser.avatar)

const logout = () => {
  // 1. 清除所有 sessionStorage（这一步已做）
  window.sessionStorage.clear()

  // 2. 移除所有动态添加的路由（除了公共路由）
  const currentRoutes = router.getRoutes()
  currentRoutes.forEach(route => {
    // 保留公共路由：'/'、'/login'、'/index'、'/userCenter'
    if (route.path !== '/' && route.path !== '/login' && route.path !== '/index' && route.path !== '/userCenter') {
      router.removeRoute(route.name)
    }
  })

  // 3. 重置 tab 状态
  store.commit('RESET_TAB')

  // 4. 跳转到登录页
  router.replace("/login")
}
</script>

<style lang="scss" scoped>
.el-dropdown-link {
  cursor: pointer;
  color: var(--el-color-primary);
  display: flex;
  align-items: center;
}
</style>
