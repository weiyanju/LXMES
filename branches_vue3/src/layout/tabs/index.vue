<template>
  <el-tabs
          v-model="editableTabsValue"
          type="card"
          class="demo-tabs"
          closable
          @tab-remove="removeTab"
          @tab-click="clickTab"
  >
    <el-tab-pane
            v-for="item in editableTabs"
            :key="item.name"
            :label="item.title"
            :name="item.name"
    >
    </el-tab-pane>
  </el-tabs>
</template>

<script setup>
  import { ref, watch } from 'vue'
  import store from '@/store'
  import { useRouter } from 'vue-router'
  const router = useRouter()

  const editableTabsValue = ref(store.state.editableTabsValue)
  const editableTabs = ref(store.state.editableTabs)

  // 移除标签
  const removeTab = (targetName) => {
    let tabs = editableTabs.value
    let activeName = editableTabsValue.value

    if (activeName === targetName) {
      tabs.forEach((tab, index) => {
        if (tab.name === targetName) {
          const nextTab = tabs[index + 1] || tabs[index - 1]
          if (nextTab) activeName = nextTab.name
        }
      })
    }

    editableTabsValue.value = activeName
    editableTabs.value = tabs.filter(tab => tab.name !== targetName)

    // 同步到 vuex
    store.state.editableTabsValue = activeName
    store.state.editableTabs = editableTabs.value

    router.push({ path: activeName })
  }

  // 点击标签 → 同步菜单高亮
  const clickTab = (target) => {
    const path = target.paneName
    router.push({ path: path })
    store.state.editableTabsValue = path
  }

  // 监听 vuex 变化
  const refreshTabs = () => {
    editableTabs.value = store.state.editableTabs
    editableTabsValue.value = store.state.editableTabsValue
  }

  watch(
          () => store.state,
          () => refreshTabs(),
          { deep: true, immediate: true }
  )
</script>

<style>
  .demo-tabs > .el-tabs__content {
    padding: 32px;
    color: #6b778c;
    font-size: 32px;
    font-weight: 600;
  }
  .el-tabs--card > .el-tabs__header .el-tabs__item.is-active {
    background-color: lightgray;
  }
  .el-main { padding: 0px; }
  .el-tabs__content { padding: 0px !important; }
</style>