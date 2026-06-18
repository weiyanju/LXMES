<template>
  <div class="app-container">

    <el-row :gutter="20" class="header">
      <el-col :span="7">
        <el-input placeholder="请输入用户名..." v-model="queryForm.query" clearable @keyup.enter="handleSearch"></el-input>
      </el-col>
      <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
      <el-button type="success" :icon="DocumentAdd" @click="handleDialogValue()">新增</el-button>
      <el-popconfirm title="您确定批量删除这些记录吗？" @confirm="handleBatchDelete">
        <template #reference>
          <el-button type="danger" :disabled="delBtnStatus" :icon="Delete">批量删除</el-button>
        </template>
      </el-popconfirm>
    </el-row>

    <el-table :data="tableData" stripe style="width: 100%" @selection-change="handleSelectionChange">
      <el-table-column type="selection" width="55"/>
      <el-table-column prop="avatar" label="头像" width="80" align="center">
        <template v-slot="scope">
          <img :src="getMediaUrl('userAvatar/' + scope.row.avatar)" width="50" height="50"/>
        </template>
      </el-table-column>
      <el-table-column prop="username" label="用户名" width="100" align="center"/>
      <el-table-column prop="roles" label="拥有角色" width="200" align="center">
        <template v-slot="scope">
          <el-tag size="small" type="warning" v-for="item in scope.row.roles" :key="item.id">
            {{ item.name }}
          </el-tag>
        </template>
      </el-table-column>
      <!-- 所属部门列：注意数据字段名变化，从 roleList 变为 roles，从 departments 保持不变 -->
      <el-table-column label="所属部门" width="200" align="center">
        <template v-slot="scope">
          <el-tag v-for="deptId in scope.row.departments" :key="deptId" size="small" type="info" style="margin:2px;">
            {{ getDepartmentName(deptId) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="email" label="邮箱" width="200" align="center"/>
      <el-table-column prop="phonenumber" label="手机号" width="120" align="center"/>
      <el-table-column prop="status" label="状态？" width="200" align="center">
        <template v-slot="{row}">
          <el-switch v-model="row.status" @change="statusChangeHandle(row)" active-text="正常"
                     inactive-text="禁用" :active-value="1" :inactive-value="0"></el-switch>
        </template>
      </el-table-column>
      <el-table-column prop="create_time" label="创建时间" width="200" align="center"/>
      <el-table-column prop="login_date" label="最后登录时间" width="200" align="center"/>
      <el-table-column prop="remark" label="备注"/>
      <el-table-column prop="action" label="操作" width="400" fixed="right" align="center">
        <template v-slot="scope">
          <el-button type="primary" :icon="Tools" @click="handleRoleDialogValue(scope.row.id, scope.row.roles)">分配角色</el-button>

          <el-popconfirm v-if="scope.row.username!=='python222'" title="您确定要对这个用户重置密码吗？"
                         @confirm="handleResetPassword(scope.row.id)">
            <template #reference>
              <el-button type="warning" :icon="RefreshRight">重置密码</el-button>
            </template>
          </el-popconfirm>

          <el-button type="primary" v-if="scope.row.username!=='python222'" :icon="Edit"
                     @click="handleDialogValue(scope.row.id)"></el-button>
          <el-popconfirm v-if="scope.row.username!=='python222'" title="您确定要删除这条记录吗？"
                         @confirm="handleDeleteSingle(scope.row.id)">
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
            :page-sizes="[10, 20, 30, 40]"
            layout="total, sizes, prev, pager, next, jumper"
            :total="total"
            @size-change="handleSizeChange"
            @current-change="handleCurrentChange"
    />

    <Dialog v-model="dialogVisible" :dialogVisible="dialogVisible" :id="id" :dialogTitle="dialogTitle"
            @initUserList="initUserList"></Dialog>
    <RoleDialog v-model="roleDialogVisible" :sysRoleList="selectedRoleList" :roleDialogVisible="roleDialogVisible" :id="id"
                @initUserList="initUserList"></RoleDialog>
  </div>
</template>

<script setup>
  import { ref, reactive } from 'vue'
  import requestUtil, { getServerUrl,getMediaUrl } from '@/util/request'
  import { Search, Delete, DocumentAdd, Edit, Tools, RefreshRight } from '@element-plus/icons-vue'
  import { ElMessage } from 'element-plus'
  import Dialog from './components/dialog.vue'
  import RoleDialog from './components/roleDialog.vue'

  const tableData = ref([])
  const total = ref(0)

  // 分页参数改为 DRF 标准：page, page_size
  const queryForm = reactive({
    query: '',
    page: 1,
    page_size: 10
  })

  const dialogVisible = ref(false)
  const dialogTitle = ref('')
  const id = ref(-1)
  const selectedRoleList = ref([])      // 当前选中用户的角色列表，用于传递给角色分配弹窗
  const roleDialogVisible = ref(false)
  const delBtnStatus = ref(true)
  const multipleSelection = ref([])

  const handleSelectionChange = (selection) => {
    multipleSelection.value = selection
    delBtnStatus.value = selection.length === 0
  }

  const handleDialogValue = (userId) => {
    if (userId) {
      id.value = userId
      dialogTitle.value = '用户修改'
    } else {
      id.value = -1
      dialogTitle.value = '用户添加'
    }
    dialogVisible.value = true
  }

  // 需要先获取所有部门列表（与 dialog.vue 类似）
  const departmentMap = ref({})

  const fetchAllDepartments = async () => {
    const res = await requestUtil.get('/api/department/departments/', { page_size: 100 })
    res.data.results.forEach(dept => {
      departmentMap.value[dept.id] = dept.name
    })
  }

  const getDepartmentName = (id) => departmentMap.value[id] || '未知部门'

  // 在 initUserList 之前或同时调用 fetchAllDepartments
  // 获取用户列表
  const initUserList = async () => {
    try {
      const res = await requestUtil.get('/api/user/users/', queryForm )
      tableData.value = res.data.results
      total.value = res.data.count
    } catch (error) {
      ElMessage.error('获取用户列表失败')
      console.error(error)
    }
  }

  // 搜索（重置页码）
  const handleSearch = () => {
    queryForm.page = 1
    initUserList()
  }

  const handleSizeChange = (pageSize) => {
    queryForm.page_size = pageSize
    queryForm.page = 1
    initUserList()
  }

  const handleCurrentChange = (page) => {
    queryForm.page = page
    initUserList()
  }

  // 单个删除
  const handleDeleteSingle = async (userId) => {
    try {
      await requestUtil.del(`/api/user/users/${userId}/`)
      ElMessage.success('删除成功')
      initUserList()
    } catch (error) {
      ElMessage.error('删除失败')
    }
  }

  // 批量删除
  const handleBatchDelete = async () => {
    const ids = multipleSelection.value.map(item => item.id)
    if (ids.length === 0) return
    try {
      await requestUtil.del('/api/user/users/batch-delete/', { ids })
      ElMessage.success('批量删除成功')
      multipleSelection.value = []
      initUserList()
    } catch (error) {
      const msg = error.response?.data?.detail || '批量删除失败'
      ElMessage.error(msg)
    }
  }

  // 重置密码
  const handleResetPassword = async (userId) => {
    try {
      await requestUtil.post(`/api/user/users/${userId}/reset-password/`)
      ElMessage.success('密码已重置为123456')
      initUserList()
    } catch (error) {
      ElMessage.error('重置密码失败')
    }
  }

  // 状态切换
  const statusChangeHandle = async (row) => {
    try {
      await requestUtil.patch(`/api/user/users/${row.id}/toggle-status/`, { status: row.status })
      ElMessage.success('状态更新成功')
    } catch (error) {
      ElMessage.error('状态更新失败')
      // 恢复原状态
      row.status = row.status === 1 ? 0 : 1
    }
  }

  // 打开角色分配弹窗，并传递当前用户的角色列表
  const handleRoleDialogValue = (userId, roleList) => {
    id.value = userId
    selectedRoleList.value = roleList || []
    roleDialogVisible.value = true
  }

  // 初始化加载
  fetchAllDepartments()
  initUserList()
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