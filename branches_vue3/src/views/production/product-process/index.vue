<template>
    <div class="app-container">
        <el-row :gutter="20" class="header">
            <el-col :span="6">
                <el-select v-model="queryParams.product" placeholder="选择产品" clearable @change="fetchData">
                    <el-option v-for="item in productOptions" :key="item.id" :label="item.product_name" :value="item.id" />
                </el-select>
            </el-col>
            <el-button type="primary" :icon="Search" @click="fetchData">查询</el-button>
            <el-button type="success" :icon="DocumentAdd" @click="handleAdd">新增关联</el-button>
        </el-row>

        <el-table :data="tableData" stripe border>
            <el-table-column prop="id" label="ID" width="80" />
            <el-table-column prop="product_name" label="产品名称" width="180" />
            <el-table-column prop="process_name" label="工序名称" min-width="150" />
            <el-table-column prop="sequence" label="工序顺序" width="100" align="center" />
            <el-table-column label="操作" width="150" align="center">
                <template #default="{ row }">
                    <el-button type="primary" link :icon="Edit" @click="handleEdit(row.id)">编辑</el-button>
                    <el-popconfirm title="确定删除该关联吗？" @confirm="handleDelete(row.id)">
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
                :page-sizes="[10, 20, 50]"
                layout="total, sizes, prev, pager, next, jumper"
                :total="total"
                @size-change="fetchData"
                @current-change="fetchData"
        />

        <!-- 新增/编辑对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form ref="formRef" :model="formData" :rules="rules" label-width="100px">
                <el-form-item label="产品" prop="product">
                    <el-select v-model="formData.product" clearable>
                        <el-option v-for="item in productOptions" :key="item.id" :label="item.product_name" :value="item.id" />
                    </el-select>
                </el-form-item>
                <el-form-item label="工序" prop="process">
                    <el-select v-model="formData.process" clearable>
                        <el-option v-for="item in processOptions" :key="item.id" :label="item.process_name" :value="item.id" />
                    </el-select>
                </el-form-item>
                <el-form-item label="工序顺序" prop="sequence">
                    <el-input-number v-model="formData.sequence" :min="1" />
                </el-form-item>
            </el-form>
            <template #footer>
                <el-button @click="dialogVisible = false">取消</el-button>
                <el-button type="primary" @click="submitForm">确定</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script setup>
    import { ref, reactive, onMounted } from 'vue'
    import { ElMessage } from 'element-plus'
    import { Search, DocumentAdd, Edit, Delete } from '@element-plus/icons-vue'
    import requestUtil from '@/util/request'

    const queryParams = reactive({ page: 1, pageSize: 10, product: '' })
    const tableData = ref([])
    const total = ref(0)
    const productOptions = ref([])
    const processOptions = ref([])

    const dialogVisible = ref(false)
    const dialogTitle = ref('')
    const formData = ref({ id: null, product: '', process: '', sequence: 1 })
    const formRef = ref(null)
    const rules = {
        product: [{ required: true, message: '请选择产品', trigger: 'change' }],
        process: [{ required: true, message: '请选择工序', trigger: 'change' }],
        sequence: [{ required: true, message: '请输入工序顺序', trigger: 'blur' }]
    }

    const fetchProducts = async () => {
        const res = await requestUtil.get('/api/products/', { params: { pageSize: 1000 } })
        productOptions.value = res.data.results
    }
    const fetchProcesses = async () => {
        const res = await requestUtil.get('/api/processes/', { params: { pageSize: 1000 } })
        processOptions.value = res.data.results
    }
    const fetchData = async () => {
        try {
            const params = { ...queryParams, page: queryParams.page, pageSize: queryParams.pageSize }
            if (queryParams.product) params.product = queryParams.product
            const res = await requestUtil.get('/api/product-processes/', { params })
            tableData.value = res.data.results
            total.value = res.data.count
        } catch { ElMessage.error('获取数据失败') }
    }
    const handleAdd = () => {
        formData.value = { id: null, product: '', process: '', sequence: 1 }
        dialogTitle.value = '新增产品工序'
        dialogVisible.value = true
    }
    const handleEdit = async (id) => {
        const res = await requestUtil.get(`/api/product-processes/${id}/`)
        formData.value = res.data
        dialogTitle.value = '编辑产品工序'
        dialogVisible.value = true
    }
    const submitForm = async () => {
        await formRef.value.validate()
        try {
            if (formData.value.id) {
                await requestUtil.put(`/api/product-processes/${formData.value.id}/`, formData.value)
                ElMessage.success('更新成功')
            } else {
                await requestUtil.post('/api/product-processes/', formData.value)
                ElMessage.success('新增成功')
            }
            dialogVisible.value = false
            fetchData()
        } catch { ElMessage.error('保存失败') }
    }
    const handleDelete = async (id) => {
        await requestUtil.del(`/api/product-processes/${id}/`)
        ElMessage.success('删除成功')
        fetchData()
    }

    onMounted(() => {
        fetchProducts()
        fetchProcesses()
        fetchData()
    })
</script>
<style scoped>.header { margin-bottom: 20px; }</style>