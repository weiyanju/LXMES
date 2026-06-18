<template>
  <el-menu
      active-text-color="#ffd04b"
      background-color="#2d3a4b"
      class="el-menu-vertical-demo"
      text-color="#fff"
      router
      :default-active="activeMenu"
  >
    <el-menu-item index="/index" @click="openTab({name:'首页',path:'/index'})">
      <el-icon>
        <home-filled/>
      </el-icon>
      <span>首页</span>
    </el-menu-item>

    <el-sub-menu :index="menu.path" v-for="menu in menuList" :key="menu.id">
      <template #title>
        <el-icon>
          <svg-icon :icon="menu.icon"/>
        </el-icon>
        <span>{{ menu.name }}</span>
      </template>
      <el-menu-item :index="item.path" v-for="item in menu.children" :key="item.id" @click="openTab(item)">
        <el-icon>
          <svg-icon :icon="item.icon"/>
        </el-icon>
        <span>{{ item.name }}</span>
      </el-menu-item>
    </el-sub-menu>
  </el-menu>
</template>

<script setup>
import store from '@/store'
import { HomeFilled } from '@element-plus/icons-vue'
import { computed, ref, onMounted } from 'vue'
import requestUtil from "@/util/request";

const menuList = ref([])

// force: true 时忽略缓存，强制重新请求
const loadMenuList = async (force = false) => {
  if (!force) {
    const cached = sessionStorage.getItem("menuList")
    if (cached) {
      menuList.value = JSON.parse(cached)
      return
    }
  }
  try {
    const res = await requestUtil.get("/api/menu/menus/user-tree/")
    menuList.value = res.data
    sessionStorage.setItem("menuList", JSON.stringify(res.data))
  } catch (error) {
    console.error('获取用户菜单树失败', error)
  }
}

onMounted(() => {
  loadMenuList()
})

// 全局刷新方法：供菜单管理操作后调用，强制重新获取
window.refreshRightMenu = () => loadMenuList(true)

const activeMenu = computed(() => {
  return store.state.editableTabsValue
})

const openTab = (item) => {
  store.commit('ADD_TABS', item)
}
</script>

<style lang="scss" scoped>
</style>