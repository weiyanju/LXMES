<template>
    <div class="app-container">
        <el-table :data="tableData" stripe border style="width: 100%">
            <el-table-column prop="code" label="条码内容" min-width="200" />
            <el-table-column prop="scan_time" label="扫描时间" width="180" />
            <el-table-column prop="remark" label="备注" />
        </el-table>

        <el-pagination
                v-model:current-page="page"
                v-model:page-size="pageSize"
                :page-sizes="[10, 20, 50, 100]"
                layout="total, sizes, prev, pager, next, jumper"
                :total="total"
                @size-change="handleSizeChange"
                @current-change="handleCurrentChange"
        />
    </div>
</template>

<script setup>
    import { ref, onMounted } from 'vue'
    import requestUtil from '@/util/request'

    const tableData = ref([])
    const page = ref(1)
    const pageSize = ref(10)
    const total = ref(0)

    const fetchList = async () => {
        try {
            // 1. 请求正确的后端接口路径
            const res = await requestUtil.get('/api/barcode/records/',  {
                params: {
                    page: page.value,
                    page_size: pageSize.value   // DRF 默认使用 page_size 参数
                }
            })

            // 2. 根据 DRF 分页响应结构解析数据
            if (res.status === 200) {
                tableData.value = res.data.results   // 实际数据在 results 中
                total.value = res.data.count         // 总条数在 count 中
            }
        } catch (error) {
            console.error('获取数据失败', error)
        }
    }

    const handleSizeChange = (val) => {
        pageSize.value = val
        page.value = 1
        fetchList()
    }

    const handleCurrentChange = (val) => {
        page.value = val
        fetchList()
    }

    onMounted(() => {
        fetchList()
        // 可选：每5秒自动刷新一次
        // setInterval(fetchList, 5000)
    })
</script>