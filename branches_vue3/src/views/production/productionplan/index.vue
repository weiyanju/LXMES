<template>
  <div class="app-container">
    <!-- 搜索栏 -->
    <el-row :gutter="20" class="header">
      <el-col :span="6">
        <el-input
            placeholder="计划名称/产品类型"
            v-model="queryParams.search"
            clearable
            @clear="handleSearch"
            @keyup.enter="handleSearch"
        />
      </el-col>
      <el-col :span="4">
        <el-select
            v-model="queryParams.status"
            placeholder="计划状态"
            clearable
            @change="handleSearch"
        >
          <el-option label="全部" value="" />
          <el-option label="待开始" value="pending" />
          <el-option label="进行中" value="in_progress" />
          <el-option label="已完成" value="completed" />
        </el-select>
      </el-col>
      <el-button type="primary" :icon="Search" @click="handleSearch" :loading="loading">搜索</el-button>
      <el-button type="success" :icon="DocumentAdd" @click="handleAdd">新增</el-button>
      <el-popconfirm title="确定删除所选计划吗？" @confirm="handleBatchDelete">
        <template #reference>
          <el-button type="danger" :disabled="selectedRows.length === 0" :icon="Delete">批量删除</el-button>
        </template>
      </el-popconfirm>
    </el-row>

    <!-- 表格 -->
    <el-table :data="tableData" stripe border @selection-change="handleSelectionChange" v-loading="loading">
      <el-table-column type="selection" width="55" />
      <el-table-column prop="id" label="ID" width="80" />
      <el-table-column prop="plan_name" label="计划名称" min-width="150" />
      <el-table-column prop="product_type" label="产品类型" width="120" />
      <el-table-column prop="quantity" label="计划数量" width="100" />
      <el-table-column prop="demand_date" label="需求日期" width="120" />
      <el-table-column prop="status" label="状态" width="100" align="center">
        <template #default="{ row }">
          <el-tag :type="getStatusType(row.status)">
            {{ getStatusLabel(row.status) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="200" fixed="right" align="center">
        <template #default="{ row }">
          <el-button type="primary" link :icon="Edit" @click="handleEdit(row.id)">编辑</el-button>
          <el-button type="info" link :icon="View" @click="handleView(row.id)">详情</el-button>
          <el-popconfirm title="确定删除该计划吗？" @confirm="handleDelete(row.id)">
            <template #reference>
              <el-button type="danger" link :icon="Delete">删除</el-button>
            </template>
          </el-popconfirm>
        </template>
      </el-table-column>
    </el-table>

    <!-- 分页 -->
    <el-pagination
        v-model:current-page="queryParams.page"
        v-model:page-size="queryParams.pageSize"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        :total="total"
        @size-change="fetchData"
        @current-change="fetchData"
    />

    <!-- 新增/编辑对话框 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="600px">
      <el-form ref="formRef" :model="formData" :rules="rules" label-width="100px">
        <el-form-item label="计划名称" prop="plan_name">
          <el-input v-model="formData.plan_name" />
        </el-form-item>
        <el-form-item label="产品类型" prop="product_type">
          <el-input v-model="formData.product_type" />
        </el-form-item>
        <el-form-item label="计划数量" prop="quantity">
          <el-input-number v-model="formData.quantity" :min="1" />
        </el-form-item>
        <el-form-item label="开始条码" prop="start_barcode">
          <el-input v-model="formData.start_barcode" />
        </el-form-item>
        <el-form-item label="结束条码" prop="end_barcode">
          <el-input v-model="formData.end_barcode" />
        </el-form-item>
        <el-form-item label="开始日期" prop="start_date">
          <el-date-picker v-model="formData.start_date" type="date" value-format="YYYY-MM-DD" />
        </el-form-item>
        <el-form-item label="结束日期" prop="end_date">
          <el-date-picker v-model="formData.end_date" type="date" value-format="YYYY-MM-DD" />
        </el-form-item>
        <el-form-item label="需求日期" prop="demand_date">
          <el-date-picker v-model="formData.demand_date" type="date" value-format="YYYY-MM-DD" />
        </el-form-item>
        <el-form-item label="来源" prop="source">
          <el-select v-model="formData.source">
            <el-option label="客户订单" :value="1" />
            <el-option label="库存备货" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态" prop="status">
          <el-select v-model="formData.status">
            <el-option label="待开始" value="pending" />
            <el-option label="进行中" value="in_progress" />
            <el-option label="已完成" value="completed" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitForm" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>

    <!-- 详情对话框 -->
    <el-dialog v-model="detailVisible" title="计划详情" width="600px">
      <el-descriptions :column="2" border>
        <el-descriptions-item label="计划名称">{{ detailData.plan_name }}</el-descriptions-item>
        <el-descriptions-item label="产品类型">{{ detailData.product_type || '-' }}</el-descriptions-item>
        <el-descriptions-item label="计划数量">{{ detailData.quantity }}</el-descriptions-item>
        <el-descriptions-item label="开始条码">{{ detailData.start_barcode || '-' }}</el-descriptions-item>
        <el-descriptions-item label="结束条码">{{ detailData.end_barcode || '-' }}</el-descriptions-item>
        <el-descriptions-item label="开始日期">{{ detailData.start_date || '-' }}</el-descriptions-item>
        <el-descriptions-item label="结束日期">{{ detailData.end_date || '-' }}</el-descriptions-item>
        <el-descriptions-item label="需求日期">{{ detailData.demand_date || '-' }}</el-descriptions-item>
        <el-descriptions-item label="来源">{{ detailData.source === 1 ? '客户订单' : '库存备货' }}</el-descriptions-item>
        <el-descriptions-item label="状态">{{ getStatusLabel(detailData.status) }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="2">{{ detailData.remark || '-' }}</el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, DocumentAdd, Delete, Edit, View } from '@element-plus/icons-vue'
import requestUtil from '@/util/request'

const loading = ref(false)
const submitting = ref(false)

const queryParams = reactive({
  page: 1,
  pageSize: 10,
  search: '',
  status: ''
})
const tableData = ref([])
const total = ref(0)
const selectedRows = ref([])

const dialogVisible = ref(false)
const dialogTitle = ref('')
const formData = ref({
  id: null,
  plan_name: '',
  product_type: '',
  quantity: 1,
  start_barcode: '',
  end_barcode: '',
  start_date: '',
  end_date: '',
  demand_date: '',
  source: 1,
  status: 'pending',
  remark: ''
})
const formRef = ref(null)
const rules = {
  plan_name: [{ required: true, message: '请输入计划名称', trigger: 'blur' }],
  quantity: [{ required: true, message: '请输入计划数量', trigger: 'blur' }]
}

const detailVisible = ref(false)
const detailData = ref({})

// 获取列表数据
const fetchData = async () => {
  loading.value = true
  try {
    // 注意：直接传递 queryParams，不包装 { params }
    const res = await requestUtil.get('/api/production-plans/', queryParams)
    tableData.value = res.data.results
    total.value = res.data.count
  } catch (error) {
    ElMessage.error('获取数据失败')
    console.error(error)
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  queryParams.page = 1
  fetchData()
}

const getStatusLabel = (status) => {
  const map = { pending: '待开始', in_progress: '进行中', completed: '已完成' }
  return map[status] || status
}
const getStatusType = (status) => {
  const map = { pending: 'info', in_progress: 'warning', completed: 'success' }
  return map[status] || ''
}

const handleAdd = () => {
  formData.value = {
    id: null,
    plan_name: '',
    product_type: '',
    quantity: 1,
    start_barcode: '',
    end_barcode: '',
    start_date: '',
    end_date: '',
    demand_date: '',
    source: 1,
    status: 'pending',
    remark: ''
  }
  dialogTitle.value = '新增生产计划'
  dialogVisible.value = true
}

const handleEdit = async (id) => {
  try {
    const res = await requestUtil.get(`/api/production-plans/${id}/`)
    formData.value = res.data
    dialogTitle.value = '编辑生产计划'
    dialogVisible.value = true
  } catch (error) {
    ElMessage.error('获取详情失败')
  }
}

const handleView = async (id) => {
  try {
    const res = await requestUtil.get(`/api/production-plans/${id}/`)
    detailData.value = res.data
    detailVisible.value = true
  } catch (error) {
    ElMessage.error('获取详情失败')
  }
}

// 处理 API 错误，提取 DRF 字段错误
const handleApiError = (error, defaultMsg) => {
  const response = error.response
  if (response && response.status === 400) {
    const data = response.data
    const messages = []
    Object.keys(data).forEach(key => {
      const val = data[key]
      if (Array.isArray(val)) messages.push(...val)
      else messages.push(val)
    })
    ElMessage.error(messages.join(' ') || defaultMsg)
  } else {
    ElMessage.error(defaultMsg)
  }
}

const submitForm = async () => {
  if (!formRef.value) return
  await formRef.value.validate()
  submitting.value = true
  try {
    if (formData.value.id) {
      await requestUtil.patch(`/api/production-plans/${formData.value.id}/`, formData.value)
      ElMessage.success('更新成功')
    } else {
      await requestUtil.post('/api/production-plans/', formData.value)
      ElMessage.success('新增成功')
    }
    dialogVisible.value = false
    fetchData()
  } catch (error) {
    handleApiError(error, '保存失败')
  } finally {
    submitting.value = false
  }
}

const handleDelete = async (id) => {
  try {
    await requestUtil.del(`/api/production-plans/${id}/`)
    ElMessage.success('删除成功')
    fetchData()
  } catch (error) {
    ElMessage.error('删除失败')
  }
}

const handleBatchDelete = async () => {
  const ids = selectedRows.value.map(row => row.id)
  if (ids.length === 0) return
  try {
    // 假设后端提供了批量删除接口，若未提供，需后端添加 batch-delete action
    await requestUtil.del('/api/production-plans/batch-delete/', { ids })
    ElMessage.success('批量删除成功')
    fetchData()
  } catch (error) {
    const msg = error.response?.data?.detail || '批量删除失败'
    ElMessage.error(msg)
  }
}

const handleSelectionChange = (selection) => {
  selectedRows.value = selection
}

onMounted(() => {
  fetchData()
})
</script>

<style scoped>
.header {
  margin-bottom: 20px;
}
</style>