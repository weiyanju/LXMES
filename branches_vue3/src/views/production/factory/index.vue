<template>
    <div class="app-container">
        <!-- 搜索栏 -->
        <el-row :gutter="20" class="header">
            <el-col :span="6">
                <el-input
                        placeholder="工厂名称/编码"
                        v-model="queryParams.search"
                        clearable
                        @clear="fetchData"
                />
            </el-col>
            <el-button type="primary" :icon="Search" @click="fetchData">搜索</el-button>
            <el-button type="success" :icon="DocumentAdd" @click="handleAdd">新增</el-button>
            <el-popconfirm title="确定删除所选工厂吗？" @confirm="handleBatchDelete">
                <template #reference>
                    <el-button type="danger" :disabled="selectedRows.length === 0" :icon="Delete">批量删除</el-button>
                </template>
            </el-popconfirm>
        </el-row>

        <!-- 表格 -->
        <el-table :data="tableData" stripe border @selection-change="handleSelectionChange">
            <el-table-column type="selection" width="55" />
            <el-table-column prop="id" label="ID" width="80" />
            <el-table-column prop="factory_code" label="工厂编码" width="120" />
            <el-table-column prop="factory_name" label="工厂名称" min-width="150" />
            <el-table-column prop="address" label="地址" min-width="200" show-overflow-tooltip />
            <el-table-column prop="contact_person" label="联系人" width="100" />
            <el-table-column prop="contact_phone" label="联系电话" width="120" />
            <el-table-column prop="status" label="状态" width="100" align="center">
                <template #default="{ row }">
                    <el-tag :type="row.status === 'active' ? 'success' : 'info'">
                        {{ row.status === 'active' ? '启用' : '禁用' }}
                    </el-tag>
                </template>
            </el-table-column>
            <el-table-column label="操作" width="200" fixed="right" align="center">
                <template #default="{ row }">
                    <el-button type="primary" link :icon="Edit" @click="handleEdit(row.id)">编辑</el-button>
                    <el-button type="info" link :icon="View" @click="handleView(row.id)">详情</el-button>
                    <el-popconfirm title="确定删除该工厂吗？" @confirm="handleDelete(row.id)">
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
                <el-form-item label="工厂编码" prop="factory_code">
                    <el-input v-model="formData.factory_code" />
                </el-form-item>
                <el-form-item label="工厂名称" prop="factory_name">
                    <el-input v-model="formData.factory_name" />
                </el-form-item>
                <el-form-item label="地址" prop="address">
                    <el-input v-model="formData.address" type="textarea" :rows="2" />
                </el-form-item>
                <el-form-item label="联系人" prop="contact_person">
                    <el-input v-model="formData.contact_person" />
                </el-form-item>
                <el-form-item label="联系电话" prop="contact_phone">
                    <el-input v-model="formData.contact_phone" />
                </el-form-item>
                <el-form-item label="状态" prop="status">
                    <el-radio-group v-model="formData.status">
                        <el-radio label="active">启用</el-radio>
                        <el-radio label="inactive">禁用</el-radio>
                    </el-radio-group>
                </el-form-item>
            </el-form>
            <template #footer>
                <el-button @click="dialogVisible = false">取消</el-button>
                <el-button type="primary" @click="submitForm">确定</el-button>
            </template>
        </el-dialog>

        <!-- 详情对话框 -->
        <el-dialog v-model="detailVisible" title="工厂详情" width="600px">
            <el-descriptions :column="2" border>
                <el-descriptions-item label="工厂编码">{{ detailData.factory_code }}</el-descriptions-item>
                <el-descriptions-item label="工厂名称">{{ detailData.factory_name }}</el-descriptions-item>
                <el-descriptions-item label="地址">{{ detailData.address || '-' }}</el-descriptions-item>
                <el-descriptions-item label="联系人">{{ detailData.contact_person || '-' }}</el-descriptions-item>
                <el-descriptions-item label="联系电话">{{ detailData.contact_phone || '-' }}</el-descriptions-item>
                <el-descriptions-item label="状态">{{ detailData.status === 'active' ? '启用' : '禁用' }}</el-descriptions-item>
                <el-descriptions-item label="创建时间">{{ detailData.created_at || '-' }}</el-descriptions-item>
                <el-descriptions-item label="更新时间">{{ detailData.updated_at || '-' }}</el-descriptions-item>
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
        search: ''
    })
    const tableData = ref([])
    const total = ref(0)
    const selectedRows = ref([])

    const dialogVisible = ref(false)
    const dialogTitle = ref('')
    const formData = ref({
        id: null,
        factory_code: '',
        factory_name: '',
        address: '',
        contact_person: '',
        contact_phone: '',
        status: 'active'
    })
    const formRef = ref(null)
    const rules = {
        factory_code: [{ required: true, message: '请输入工厂编码', trigger: 'blur' }],
        factory_name: [{ required: true, message: '请输入工厂名称', trigger: 'blur' }]
    }

    const detailVisible = ref(false)
    const detailData = ref({})

    const fetchData = async () => {
        try {
            const res = await requestUtil.get('/api/factories/', { params: queryParams })
            tableData.value = res.data.results
            total.value = res.data.count
        } catch (error) {
            ElMessage.error('获取数据失败')
        }
    }

    const handleAdd = () => {
        formData.value = {
            id: null,
            factory_code: '',
            factory_name: '',
            address: '',
            contact_person: '',
            contact_phone: '',
            status: 'active'
        }
        dialogTitle.value = '新增工厂'
        dialogVisible.value = true
    }

    const handleEdit = async (id) => {
        const res = await requestUtil.get(`/api/factories/${id}/`)
        formData.value = res.data
        dialogTitle.value = '编辑工厂'
        dialogVisible.value = true
    }

    const handleView = async (id) => {
        const res = await requestUtil.get(`/api/factories/${id}/`)
        detailData.value = res.data
        detailVisible.value = true
    }

    const submitForm = async () => {
        await formRef.value.validate()
        try {
            if (formData.value.id) {
                await requestUtil.put(`/api/factories/${formData.value.id}/`, formData.value)
                ElMessage.success('更新成功')
            } else {
                await requestUtil.post('/api/factories/', formData.value)
                ElMessage.success('新增成功')
            }
            dialogVisible.value = false
            fetchData()
        } catch (error) {
            ElMessage.error('保存失败')
        }
    }

    const handleDelete = async (id) => {
        await requestUtil.del(`/api/factories/${id}/`)
        ElMessage.success('删除成功')
        fetchData()
    }

    const handleBatchDelete = async () => {
        const ids = selectedRows.value.map(row => row.id)
        try {
            await Promise.all(ids.map(id => requestUtil.del(`/api/factories/${id}/`)))
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