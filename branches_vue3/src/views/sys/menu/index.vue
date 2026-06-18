<template>
  <div class="app-container">
    <el-row :gutter="20" class="header">
      <el-col :span="7">
        <el-input placeholder="请输入菜单名称..." v-model="queryForm.query" clearable @keyup.enter="handleSearch" />
      </el-col>
      <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
      <el-button type="success" :icon="DocumentAdd" @click="handleDialogValue()">新增</el-button>
      <el-popconfirm title="您确定批量删除这些记录吗？" @confirm="handleBatchDelete">
        <template #reference>
          <el-button type="danger" :disabled="delBtnStatus" :icon="Delete">批量删除</el-button>
        </template>
      </el-popconfirm>
    </el-row>

    <el-table
        :data="filteredTableData"
        style="width: 100%; margin-bottom: 20px"
        row-key="id"
        border
        stripe
        default-expand-all
        :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
        @selection-change="handleSelectionChange"
    >
      <el-table-column type="selection" width="55" />
      <el-table-column prop="name" label="菜单名称" width="180" />
      <el-table-column prop="icon" label="图标" width="70" align="center">
        <template v-slot="scope">
          <el-icon>
            <svg-icon :icon="scope.row.icon" />
          </el-icon>
        </template>
      </el-table-column>
      <el-table-column prop="order_num" label="排序" width="70" align="center" />
      <el-table-column prop="perms" label="权限标识" width="200" />
      <el-table-column prop="path" label="路由地址" width="180" />
      <el-table-column prop="component" label="组件路径" width="200" />
      <el-table-column prop="menu_type" label="菜单类型" width="120" align="center">
        <template v-slot="scope">
          <el-tag size="small" v-if="scope.row.menu_type === 'M'" type="danger" effect="dark">目录</el-tag>
          <el-tag size="small" v-else-if="scope.row.menu_type === 'C'" type="success" effect="dark">菜单</el-tag>
          <el-tag size="small" v-else type="info" effect="dark">按钮</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="create_time" label="创建时间" align="center" width="120" />
      <el-table-column prop="action" label="操作" width="300" fixed="right" align="center">
        <template v-slot="scope">
          <el-button type="primary" :icon="Edit" @click="handleDialogValue(scope.row.id)" />
          <el-popconfirm title="您确定要删除这条记录吗？" @confirm="handleDeleteSingle(scope.row.id)">
            <template #reference>
              <el-button type="danger" :icon="Delete" />
            </template>
          </el-popconfirm>
        </template>
      </el-table-column>
    </el-table>

    <!-- 分页（可选，树形表格通常不分页，但若数据量大可启用） -->
    <el-pagination
        v-if="false"
        v-model:current-page="queryForm.page"
        v-model:page-size="queryForm.page_size"
        :total="total"
        layout="total, prev, pager, next"
    />
  </div>

  <Dialog v-model="dialogVisible" :tableData="tableData" :dialogVisible="dialogVisible" :id="id"
          :dialogTitle="dialogTitle" @initMenuList="initMenuList" />
</template>

<script setup>
import { ref, reactive, computed } from 'vue'
import { Search, Delete, DocumentAdd, Edit } from '@element-plus/icons-vue'
import requestUtil from '@/util/request'
import { ElMessage } from 'element-plus'
import Dialog from './components/dialog.vue'

const tableData = ref([])          // 原始树形数据
const total = ref(0)
const multipleSelection = ref([])
const delBtnStatus = ref(true)

// 搜索表单（前端过滤，也可改为后端搜索）
const queryForm = reactive({
  query: '',
  page: 1,
  page_size: 999   // 树形结构一般不分页，给个大值
})

// 前端本地过滤（保持树结构）
const filterTree = (nodes, keyword) => {
  if (!keyword) return nodes
  return nodes.reduce((acc, node) => {
    const children = node.children ? filterTree(node.children, keyword) : []
    if (node.name.includes(keyword) || children.length) {
      acc.push({ ...node, children })
    }
    return acc
  }, [])
}

const filteredTableData = computed(() => {
  return filterTree(tableData.value, queryForm.query)
})

// 初始化菜单树
const initMenuList = async () => {
  try {
    const res = await requestUtil.get('/api/menu/menus/tree/')
    tableData.value = res.data
    // 刷新右侧菜单（如果有全局方法）
    if (window.refreshRightMenu) window.refreshRightMenu()
  } catch (error) {
    ElMessage.error('获取菜单列表失败')
    console.error(error)
  }
}

// 搜索
const handleSearch = () => {
  // 前端过滤，无需重新请求
}

// 多选
const handleSelectionChange = (selection) => {
  multipleSelection.value = selection
  delBtnStatus.value = selection.length === 0
}

// 单个删除
const handleDeleteSingle = async (id) => {
  try {
    await requestUtil.del(`/api/menu/menus/${id}/`)
    ElMessage.success('删除成功')
    initMenuList()
  } catch (error) {
    const msg = error.response?.data?.detail || '删除失败'
    ElMessage.error(msg)
  }
}

// 批量删除
const handleBatchDelete = async () => {
  const ids = multipleSelection.value.map(item => item.id)
  if (ids.length === 0) return
  try {
    await requestUtil.del('/api/menu/menus/batch-delete/', { ids })
    ElMessage.success('批量删除成功')
    multipleSelection.value = []
    initMenuList()
  } catch (error) {
    const msg = error.response?.data?.detail || '批量删除失败'
    ElMessage.error(msg)
  }
}

// 新增/编辑弹窗
const id = ref(null)
const dialogVisible = ref(false)
const dialogTitle = ref('')

const handleDialogValue = (menuId) => {
  if (menuId) {
    id.value = menuId
    dialogTitle.value = '菜单修改'
  } else {
    id.value = null
    dialogTitle.value = '菜单添加'
  }
  dialogVisible.value = true
}

initMenuList()
</script>

<style lang="scss" scoped>
.header {
  padding-bottom: 16px;
  box-sizing: border-box;
}
.el-pagination {
  float: right;
  padding: 20px;
  box-sizing: border-box;
}
::v-deep th.el-table__cell {
  word-break: break-word;
  background-color: #f8f8f9 !important;
  color: #515a6e;
  height: 40px;
  font-size: 13px;
}
.el-tag--small {
  margin-left: 5px;
}
</style>