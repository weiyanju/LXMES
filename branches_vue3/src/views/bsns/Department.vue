<template>
    <div class="app-container">
        <!-- 搜索栏 -->
        <el-row :gutter="20" class="header">
            <el-col :span="7">
                <el-input placeholder="请输入部门名称..." v-model="queryForm.query" clearable @keyup.enter="handleSearch"></el-input>
            </el-col>
            <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
            <el-button type="success" :icon="DocumentAdd" @click="handleDialogValue()">新增</el-button>
            <el-popconfirm title="您确定批量删除这些记录吗？" @confirm="handleBatchDelete">
                <template #reference>
                    <el-button type="danger" :disabled="delBtnStatus" :icon="Delete">批量删除</el-button>
                </template>
            </el-popconfirm>
        </el-row>

        <!-- 表格 -->
        <el-table :data="tableData" stripe style="width: 100%" @selection-change="handleSelectionChange">
            <el-table-column type="selection" width="55"/>
            <el-table-column prop="id" label="编号" width="100" align="center"/>
            <el-table-column prop="name" label="部门名称" width="200" align="center"/>
            <el-table-column prop="remark" label="备注"/>
            <el-table-column label="操作" width="200" fixed="right" align="center">
                <template v-slot="scope">
                    <el-button type="primary" :icon="Edit" @click="handleDialogValue(scope.row.id)"></el-button>
                    <el-popconfirm title="您确定要删除这条记录吗？" @confirm="handleDeleteSingle(scope.row.id)">
                        <template #reference>
                            <el-button type="danger" :icon="Delete"/>
                        </template>
                    </el-popconfirm>
                </template>
            </el-table-column>
        </el-table>

        <!-- 分页 -->
        <el-pagination
                v-model:current-page="queryForm.page"
                v-model:page-size="queryForm.page_size"
                :page-sizes="[10, 20, 30, 40]"
                layout="total, sizes, prev, pager, next, jumper"
                :total="total"
                @size-change="handleSizeChange"
                @current-change="handleCurrentChange"
        />

        <!-- 新增/编辑对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="30%">
            <el-form :model="formData" label-width="80px">
                <el-form-item label="部门名称">
                    <el-input v-model="formData.name" autocomplete="off"></el-input>
                </el-form-item>
                <el-form-item label="备注">
                    <el-input v-model="formData.remark" type="textarea" :rows="3"></el-input>
                </el-form-item>
            </el-form>
            <template #footer>
                <span class="dialog-footer">
                    <el-button @click="dialogVisible = false">取消</el-button>
                    <el-button type="primary" @click="handleSave">确定</el-button>
                </span>
            </template>
        </el-dialog>
    </div>
</template>

<script setup>
    import { ref, reactive } from 'vue'
    import { Search, Delete, DocumentAdd, Edit } from '@element-plus/icons-vue'
    import requestUtil from '@/util/request'
    import { ElMessage } from 'element-plus'

    const tableData = ref([])
    const total = ref(0)
    const queryForm = reactive({
        query: '',
        page: 1,
        page_size: 10
    })

    const dialogVisible = ref(false)
    const dialogTitle = ref('')
    const formData = ref({
        id: null,
        name: '',
        remark: ''
    })

    const delBtnStatus = ref(true)
    const multipleSelection = ref([])

    const fetchList = async () => {
        try {
            const res = await requestUtil.get('/api/department/departments/', queryForm )
            tableData.value = res.data.results
            total.value = res.data.count
        } catch (error) {
            ElMessage.error('获取部门列表失败')
            console.error(error)
        }
    }

    const handleSearch = () => {
        queryForm.page = 1
        fetchList()
    }

    const handleSizeChange = (pageSize) => {
        queryForm.page_size = pageSize
        queryForm.page = 1
        fetchList()
    }

    const handleCurrentChange = (page) => {
        queryForm.page = page
        fetchList()
    }

    const handleSelectionChange = (selection) => {
        multipleSelection.value = selection
        delBtnStatus.value = selection.length === 0
    }

    const handleDialogValue = async (id) => {
        if (id) {
            try {
                const res = await requestUtil.get(`/api/department/departments/${id}/`)
                formData.value = { ...res.data }
                dialogTitle.value = '编辑部门'
                dialogVisible.value = true
            } catch (error) {
                ElMessage.error('获取部门详情失败')
            }
        } else {
            formData.value = { id: null, name: '', remark: '' }
            dialogTitle.value = '新增部门'
            dialogVisible.value = true
        }
    }

    const handleSave = async () => {
        try {
            const data = { name: formData.value.name, remark: formData.value.remark }
            if (formData.value.id) {
                await requestUtil.patch(`/api/department/departments/${formData.value.id}/`, data)
                ElMessage.success('更新成功')
            } else {
                await requestUtil.post('/api/department/departments/', data)
                ElMessage.success('新增成功')
            }
            dialogVisible.value = false
            fetchList()
        } catch (error) {
            const errorData = error.response?.data
            let errorMsg = '操作失败'
            if (errorData) {
                const messages = []
                Object.keys(errorData).forEach(key => {
                    const val = errorData[key]
                    if (Array.isArray(val)) messages.push(...val)
                    else messages.push(val)
                })
                errorMsg = messages.join(' ') || errorMsg
            }
            ElMessage.error(errorMsg)
        }
    }

    // 单个删除（带日志）
    const handleDeleteSingle = async (id) => {
        console.log('触发单个删除，id:', id)
        try {
            console.log('准备发送 DELETE 请求:', `/api/department/departments/${id}/`)
            const res = await requestUtil.del(`/api/department/departments/${id}/`)
            console.log('DELETE 响应:', res)
            ElMessage.success('删除成功')
            fetchList()
        } catch (error) {
            console.error('DELETE 错误:', error)
            ElMessage.error('删除失败')
        }
    }

    // 批量删除（带日志）
    const handleBatchDelete = async () => {
        const ids = multipleSelection.value.map(item => item.id)
        console.log('触发批量删除，ids:', ids)
        if (ids.length === 0) return
        try {
            console.log('准备发送批量 DELETE:', '/api/department/departments/batch-delete/', { ids })
            const res = await requestUtil.del('/api/department/departments/batch-delete/', { ids })
            console.log('批量删除响应:', res)
            ElMessage.success('批量删除成功')
            multipleSelection.value = []
            fetchList()
        } catch (error) {
            console.error('批量删除错误:', error)
            const msg = error.response?.data?.detail || '批量删除失败'
            ElMessage.error(msg)
        }
    }

    fetchList()
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
</style>