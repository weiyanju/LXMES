<template>
    <div class="app-container">
        <!-- 搜索栏 -->
        <el-row :gutter="20" class="header">
            <el-col :span="6">
                <el-input
                        placeholder="工单编号/产品类型"
                        v-model="queryParams.search"
                        clearable
                        @clear="fetchData"
                />
            </el-col>
            <el-col :span="4">
                <el-select
                        v-model="queryParams.status"
                        placeholder="工单状态"
                        clearable
                        @change="fetchData"
                >
                    <el-option label="全部" value="" />
                    <el-option label="待开始" value="pending" />
                    <el-option label="进行中" value="in_progress" />
                    <el-option label="已完成" value="completed" />
                    <el-option label="已取消" value="cancelled" />
                </el-select>
            </el-col>
            <el-button type="primary" :icon="Search" @click="fetchData">搜索</el-button>
            <el-button type="success" :icon="DocumentAdd" @click="handleAdd">新增</el-button>
            <el-popconfirm title="确定删除所选工单吗？" @confirm="handleBatchDelete">
                <template #reference>
                    <el-button type="danger" :disabled="selectedRows.length === 0" :icon="Delete">批量删除</el-button>
                </template>
            </el-popconfirm>
        </el-row>

        <!-- 表格 -->
        <el-table :data="tableData" stripe border @selection-change="handleSelectionChange">
            <el-table-column type="selection" width="55" />
            <el-table-column prop="id" label="ID" width="80" />
            <el-table-column prop="order_number" label="工单编号" min-width="150" />
            <el-table-column prop="product_type" label="产品类型" width="120" />
            <el-table-column prop="quantity" label="数量" width="100" />
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
                    <el-popconfirm title="确定删除该工单吗？" @confirm="handleDelete(row.id)">
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
                <el-form-item label="工单编号" prop="order_number">
                    <el-input v-model="formData.order_number" />
                </el-form-item>
                <el-form-item label="产品类型" prop="product_type">
                    <el-input v-model="formData.product_type" />
                </el-form-item>
                <el-form-item label="数量" prop="quantity">
                    <el-input-number v-model="formData.quantity" :min="1" />
                </el-form-item>
                <el-form-item label="开始条码" prop="start_barcode">
                    <el-input v-model="formData.start_barcode" />
                </el-form-item>
                <el-form-item label="结束条码" prop="end_barcode">
                    <el-input v-model="formData.end_barcode" />
                </el-form-item>
                <el-form-item label="需求日期" prop="demand_date">
                    <el-date-picker v-model="formData.demand_date" type="date" value-format="YYYY-MM-DD" />
                </el-form-item>
                <el-form-item label="状态" prop="status">
                    <el-select v-model="formData.status">
                        <el-option label="待开始" value="pending" />
                        <el-option label="进行中" value="in_progress" />
                        <el-option label="已完成" value="completed" />
                        <el-option label="已取消" value="cancelled" />
                    </el-select>
                </el-form-item>
                <el-form-item label="备注" prop="remark">
                    <el-input v-model="formData.remark" type="textarea" :rows="3" />
                </el-form-item>
            </el-form>
            <template #footer>
                <el-button @click="dialogVisible = false">取消</el-button>
                <el-button type="primary" @click="submitForm">确定</el-button>
            </template>
        </el-dialog>

        <!-- 详情对话框 -->
        <el-dialog v-model="detailVisible" title="工单详情" width="600px">
            <el-descriptions :column="2" border>
                <el-descriptions-item label="工单编号">{{ detailData.order_number }}</el-descriptions-item>
                <el-descriptions-item label="产品类型">{{ detailData.product_type }}</el-descriptions-item>
                <el-descriptions-item label="数量">{{ detailData.quantity }}</el-descriptions-item>
                <el-descriptions-item label="开始条码">{{ detailData.start_barcode || '-' }}</el-descriptions-item>
                <el-descriptions-item label="结束条码">{{ detailData.end_barcode || '-' }}</el-descriptions-item>
                <el-descriptions-item label="需求日期">{{ detailData.demand_date || '-' }}</el-descriptions-item>
                <el-descriptions-item label="开始时间">{{ detailData.start_time || '-' }}</el-descriptions-item>
                <el-descriptions-item label="结束时间">{{ detailData.end_time || '-' }}</el-descriptions-item>
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
        order_number: '',
        product_type: '',
        quantity: 1,
        start_barcode: '',
        end_barcode: '',
        demand_date: '',
        status: 'pending',
        remark: ''
    })
    const formRef = ref(null)
    const rules = {
        order_number: [{ required: true, message: '请输入工单编号', trigger: 'blur' }],
        quantity: [{ required: true, message: '请输入数量', trigger: 'blur' }]
    }

    const detailVisible = ref(false)
    const detailData = ref({})

    const fetchData = async () => {
        try {
            const res = await requestUtil.get('/api/work-orders/', { params: queryParams })
            tableData.value = res.data.results
            total.value = res.data.count
        } catch (error) {
            ElMessage.error('获取数据失败')
        }
    }

    const getStatusLabel = (status) => {
        const map = { pending: '待开始', in_progress: '进行中', completed: '已完成', cancelled: '已取消' }
        return map[status] || status
    }
    const getStatusType = (status) => {
        const map = { pending: 'info', in_progress: 'warning', completed: 'success', cancelled: 'danger' }
        return map[status] || ''
    }

    const handleAdd = () => {
        formData.value = {
            id: null,
            order_number: '',
            product_type: '',
            quantity: 1,
            start_barcode: '',
            end_barcode: '',
            demand_date: '',
            status: 'pending',
            remark: ''
        }
        dialogTitle.value = '新增工单'
        dialogVisible.value = true
    }

    const handleEdit = async (id) => {
        const res = await requestUtil.get(`/api/work-orders/${id}/`)
        formData.value = res.data
        dialogTitle.value = '编辑工单'
        dialogVisible.value = true
    }

    const handleView = async (id) => {
        const res = await requestUtil.get(`/api/work-orders/${id}/`)
        detailData.value = res.data
        detailVisible.value = true
    }

    const submitForm = async () => {
        await formRef.value.validate()
        try {
            if (formData.value.id) {
                await requestUtil.put(`/api/work-orders/${formData.value.id}/`, formData.value)
                ElMessage.success('更新成功')
            } else {
                await requestUtil.post('/api/work-orders/', formData.value)
                ElMessage.success('新增成功')
            }
            dialogVisible.value = false
            fetchData()
        } catch (error) {
            ElMessage.error('保存失败')
        }
    }

    const handleDelete = async (id) => {
        await requestUtil.del(`/api/work-orders/${id}/`)
        ElMessage.success('删除成功')
        fetchData()
    }

    const handleBatchDelete = async () => {
        const ids = selectedRows.value.map(row => row.id)
        try {
            await Promise.all(ids.map(id => requestUtil.del(`/api/work-orders/${id}/`)))
            ElMessage.success('批量删除成功')
            fetchData()
        } catch (error) {
            ElMessage.error('批量删除失败')
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