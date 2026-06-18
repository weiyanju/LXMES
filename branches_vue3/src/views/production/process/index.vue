<template>
    <div class="app-container">
        <el-row :gutter="20" class="header">
            <el-col :span="6">
                <el-input placeholder="工序名称/编码" v-model="queryParams.search" clearable @clear="fetchData" />
            </el-col>
            <el-button type="primary" :icon="Search" @click="fetchData">搜索</el-button>
            <el-button type="success" :icon="DocumentAdd" @click="handleAdd">新增</el-button>
            <el-popconfirm title="确定删除所选工序吗？" @confirm="handleBatchDelete">
                <template #reference>
                    <el-button type="danger" :disabled="selectedRows.length === 0" :icon="Delete">批量删除</el-button>
                </template>
            </el-popconfirm>
        </el-row>

        <el-table :data="tableData" stripe border @selection-change="handleSelectionChange">
            <el-table-column type="selection" width="55" />
            <el-table-column prop="id" label="ID" width="80" />
            <el-table-column prop="process_code" label="工序编码" width="120" />
            <el-table-column prop="process_name" label="工序名称" min-width="150" />
            <el-table-column prop="description" label="描述" min-width="200" show-overflow-tooltip />
            <el-table-column label="操作" width="200" fixed="right" align="center">
                <template #default="{ row }">
                    <el-button type="primary" link :icon="Edit" @click="handleEdit(row.id)">编辑</el-button>
                    <el-button type="info" link :icon="View" @click="handleView(row.id)">详情</el-button>
                    <el-popconfirm title="确定删除该工序吗？" @confirm="handleDelete(row.id)">
                        <template #reference>
                            <el-button type="danger" link :icon="Delete">删除</el-button>
                        </template>
                    </el-popconfirm>
                </template>
            </el-table-column>
        </el-table>

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
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form ref="formRef" :model="formData" :rules="rules" label-width="100px">
                <el-form-item label="工序编码" prop="process_code">
                    <el-input v-model="formData.process_code" />
                </el-form-item>
                <el-form-item label="工序名称" prop="process_name">
                    <el-input v-model="formData.process_name" />
                </el-form-item>
                <el-form-item label="描述" prop="description">
                    <el-input v-model="formData.description" type="textarea" :rows="3" />
                </el-form-item>
            </el-form>
            <template #footer>
                <el-button @click="dialogVisible = false">取消</el-button>
                <el-button type="primary" @click="submitForm">确定</el-button>
            </template>
        </el-dialog>

        <!-- 详情对话框 -->
        <el-dialog v-model="detailVisible" title="工序详情" width="500px">
            <el-descriptions :column="1" border>
                <el-descriptions-item label="工序编码">{{ detailData.process_code }}</el-descriptions-item>
                <el-descriptions-item label="工序名称">{{ detailData.process_name }}</el-descriptions-item>
                <el-descriptions-item label="描述">{{ detailData.description || '-' }}</el-descriptions-item>
                <el-descriptions-item label="创建时间">{{ detailData.created_at }}</el-descriptions-item>
                <el-descriptions-item label="更新时间">{{ detailData.updated_at }}</el-descriptions-item>
            </el-descriptions>
        </el-dialog>
    </div>
</template>

<script setup>
    import { ref, reactive, onMounted } from 'vue'
    import { ElMessage } from 'element-plus'
    import { Search, DocumentAdd, Delete, Edit, View } from '@element-plus/icons-vue'
    import requestUtil from '@/util/request'

    const queryParams = reactive({ page: 1, pageSize: 10, search: '' })
    const tableData = ref([])
    const total = ref(0)
    const selectedRows = ref([])

    const dialogVisible = ref(false)
    const dialogTitle = ref('')
    const formData = ref({ id: null, process_code: '', process_name: '', description: '' })
    const formRef = ref(null)
    const rules = {
        process_code: [{ required: true, message: '请输入工序编码', trigger: 'blur' }],
        process_name: [{ required: true, message: '请输入工序名称', trigger: 'blur' }]
    }

    const detailVisible = ref(false)
    const detailData = ref({})

    const fetchData = async () => {
        try {
            const res = await requestUtil.get('/api/processes/', { params: queryParams })
            tableData.value = res.data.results
            total.value = res.data.count
        } catch { ElMessage.error('获取数据失败') }
    }
    const handleAdd = () => {
        formData.value = { id: null, process_code: '', process_name: '', description: '' }
        dialogTitle.value = '新增工序'
        dialogVisible.value = true
    }
    const handleEdit = async (id) => {
        const res = await requestUtil.get(`/api/processes/${id}/`)
        formData.value = res.data
        dialogTitle.value = '编辑工序'
        dialogVisible.value = true
    }
    const handleView = async (id) => {
        const res = await requestUtil.get(`/api/processes/${id}/`)
        detailData.value = res.data
        detailVisible.value = true
    }
    const submitForm = async () => {
        await formRef.value.validate()
        try {
            if (formData.value.id) {
                await requestUtil.put(`/api/processes/${formData.value.id}/`, formData.value)
                ElMessage.success('更新成功')
            } else {
                await requestUtil.post('/api/processes/', formData.value)
                ElMessage.success('新增成功')
            }
            dialogVisible.value = false
            fetchData()
        } catch { ElMessage.error('保存失败') }
    }
    const handleDelete = async (id) => {
        await requestUtil.del(`/api/processes/${id}/`)
        ElMessage.success('删除成功')
        fetchData()
    }
    const handleBatchDelete = async () => {
        const ids = selectedRows.value.map(row => row.id)
        try {
            await Promise.all(ids.map(id => requestUtil.del(`/api/processes/${id}/`)))
            ElMessage.success('批量删除成功')
            fetchData()
        } catch { ElMessage.error('批量删除失败') }
    }
    const handleSelectionChange = (selection) => { selectedRows.value = selection }

    onMounted(() => fetchData())
</script>
<style scoped>.header { margin-bottom: 20px; }</style>