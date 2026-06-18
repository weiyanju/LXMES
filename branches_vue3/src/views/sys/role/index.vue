<template>
  <div class="app-container">

    <el-row :gutter="20" class="header">
      <el-col :span="7">
        <el-input placeholder="请输入角色名..." v-model="queryForm.query" clearable @keyup.enter="handleSearch"></el-input>
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
        :data="tableData"
        stripe
        style="width: 100%"
        @selection-change="handleSelectionChange">
      <el-table-column type="selection" width="55"/>
      <el-table-column prop="name" label="角色名" width="100" align="center"/>
      <el-table-column prop="code" label="权限字符" width="200" align="center"/>
      <el-table-column prop="create_time" label="创建时间" width="200" align="center"/>
      <el-table-column prop="remark" label="备注"/>
      <el-table-column prop="action" label="操作" width="400" fixed="right" align="center">
        <template v-slot="scope">
          <el-button type="primary" :icon="Tools" @click="handleMenuDialogValue(scope.row.id)">分配权限</el-button>

          <el-button v-if="scope.row.code!=='admin'" type="primary" :icon="Edit"
                     @click="handleDialogValue(scope.row.id)"/>

          <el-popconfirm v-if="scope.row.code!=='admin'" title="您确定要删除这条记录吗？" @confirm="handleDeleteSingle(scope.row.id)">
            <template #reference>
              <el-button type="danger" :icon="Delete"/>
            </template>
          </el-popconfirm>
        </template>
      </el-table-column>
    </el-table>

    <el-pagination
        v-model:current-page="queryForm.page"
        v-model:page-size="queryForm.page_size"
        :page-sizes="[10, 20, 30, 40, 50]"
        layout="total, sizes, prev, pager, next, jumper"
        :total="total"
        @size-change="handleSizeChange"
        @current-change="handleCurrentChange"
    />
  </div>

  <Dialog v-model="dialogVisible" :dialogVisible="dialogVisible" :id="id" :dialogTitle="dialogTitle"
          @initRoleList="initRoleList"></Dialog>
  <MenuDialog v-model="menuDialogVisible" :menuDialogVisible="menuDialogVisible" :id="id"
              @initRoleList="initRoleList"></MenuDialog>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { Search, Delete, DocumentAdd, Edit, Tools } from '@element-plus/icons-vue'
import requestUtil from "@/util/request"
import { ElMessage } from 'element-plus'
import Dialog from './components/dialog.vue'
import MenuDialog from './components/menuDialog.vue'

const id = ref(null)
const dialogVisible = ref(false)
const dialogTitle = ref('')

const menuDialogVisible = ref(false)

const handleMenuDialogValue = (roleId) => {
  if (roleId) {
    id.value = roleId
  }
  menuDialogVisible.value = true
}

const handleDialogValue = (roleId) => {
  if (roleId) {
    id.value = roleId
    dialogTitle.value = "角色修改"
  } else {
    id.value = null
    dialogTitle.value = "角色添加"
  }
  dialogVisible.value = true
}

// 分页参数改为 DRF 标准
const queryForm = reactive({
  query: '',
  page: 1,
  page_size: 10
})

const total = ref(0)
const tableData = ref([])
const multipleSelection = ref([])
const delBtnStatus = ref(true)

const handleSelectionChange = (selection) => {
  multipleSelection.value = selection
  delBtnStatus.value = selection.length === 0
}

// 单个删除
const handleDeleteSingle = async (roleId) => {
  try {
    await requestUtil.del(`/api/role/roles/${roleId}/`)
    ElMessage.success('删除成功')
    initRoleList()
  } catch (error) {
    ElMessage.error('删除失败')
  }
}

// 批量删除
const handleBatchDelete = async () => {
  const ids = multipleSelection.value.map(item => item.id)
  if (ids.length === 0) return
  try {
    await requestUtil.del('/api/role/roles/batch-delete/', { ids })
    ElMessage.success('批量删除成功')
    multipleSelection.value = []
    initRoleList()
  } catch (error) {
    const msg = error.response?.data?.detail || '批量删除失败'
    ElMessage.error(msg)
  }
}

// 获取角色列表
const initRoleList = async () => {
  try {
    const res = await requestUtil.get('/api/role/roles/', queryForm)
    tableData.value = res.data.results
    total.value = res.data.count
  } catch (error) {
    ElMessage.error('获取角色列表失败')
    console.error(error)
  }
}

// 搜索
const handleSearch = () => {
  queryForm.page = 1
  initRoleList()
}

const handleSizeChange = (pageSize) => {
  queryForm.page_size = pageSize
  queryForm.page = 1
  initRoleList()
}

const handleCurrentChange = (page) => {
  queryForm.page = page
  initRoleList()
}

initRoleList()
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