<template>
  <div
      class="screen-container"
      @dblclick="toggleFullscreen"
      :class="{ 'fullscreen-mode': isFullscreen }"
  >
    <!-- 顶部标题栏 -->
    <div class="screen-header">
      <h1>智能车间生产管控大屏</h1>
      <el-button
          class="fullscreen-btn"
          :icon="isFullscreen ? Close : FullScreen"
          circle
          @click.stop="toggleFullscreen"
      />
    </div>

    <!-- 主要内容区：flex 列布局，三行等分 -->
    <div class="dashboard-rows">
      <!-- 第一行 -->
      <div class="row row-1">
        <div class="card defect-table-card">
          <div class="card-title">日期</div>
          <el-table
              :data="defectRecords"
              size="small"
              stripe
              border
              style="width: 100%"
              height="100%"
              class="auto-height-table"
          >
            <el-table-column prop="date" label="日期" min-width="130" show-overflow-tooltip />
            <el-table-column prop="product" label="产品" min-width="100" show-overflow-tooltip />
            <el-table-column prop="reportQty" label="报工数量" min-width="80" />
            <el-table-column prop="goodQty" label="良品数" min-width="80" />
            <el-table-column prop="badQty" label="不良品数" min-width="80" />
          </el-table>
        </div>

        <div class="card performance-card">
          <div class="card-title">员工绩效Top5</div>
          <div class="bar-chart" ref="performanceChartRef"></div>
        </div>

        <div class="card sales-card">
          <div class="card-title">销售订单占比</div>
          <div class="pie-chart" ref="salesChartRef"></div>
        </div>
      </div>

      <!-- 第二行 -->
      <div class="row row-2">
        <div class="card defect-card">
          <div class="card-title">7天内不良品占比</div>
          <div class="defect-summary">
            <div>总数量：<span class="highlight">{{ defectTotal }}</span></div>
            <div>最多不良品项：<span class="highlight">{{ topDefectItem }}</span></div>
            <div>最多不良品项占比：<span class="highlight">{{ topDefectPercent }}%</span></div>
          </div>
          <div class="pie-chart" ref="pieChartRef"></div>
        </div>

        <div class="card process-card">
          <div class="card-title">工序计划数Top5</div>
          <div class="bar-chart" ref="processChartRef"></div>
        </div>
      </div>

      <!-- 第三行 -->
      <div class="row row-3">
        <div class="card work-order-card">
          <div class="card-title">工单编号</div>
          <el-table
              :data="workOrders"
              size="small"
              stripe
              border
              style="width: 100%"
              height="100%"
              class="auto-height-table"
          >
            <el-table-column prop="orderNo" label="工单编号" min-width="200" show-overflow-tooltip />
            <el-table-column prop="status" label="状态" min-width="80" />
            <el-table-column prop="productCode" label="产品编号" min-width="150" show-overflow-tooltip />
            <el-table-column prop="productName" label="产品名称" min-width="120" show-overflow-tooltip />
            <el-table-column prop="spec" label="产品规格" min-width="80" />
            <el-table-column prop="planQty" label="计划数量" min-width="80" />
            <el-table-column prop="actualQty" label="实际数量" min-width="80" />
          </el-table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { FullScreen, Close } from '@element-plus/icons-vue'
import * as echarts from 'echarts'
import requestUtil from '@/util/request'
import { ElMessage } from 'element-plus'

// ---------- 数据状态 ----------
const defectRecords = ref([])
const workOrders = ref([])
const defectPieData = ref([])
const performanceData = ref({ names: [], values: [] })
const salesData = ref([])
const processData = ref({ names: [], values: [] })

// 计算属性
const defectTotal = computed(() => {
  return defectPieData.value.reduce((sum, item) => sum + item.value, 0)
})
const topDefectItem = computed(() => {
  if (!defectPieData.value.length) return ''
  const max = defectPieData.value.reduce((a, b) => a.value > b.value ? a : b)
  return max.name
})
const topDefectPercent = computed(() => {
  if (!defectTotal.value) return '0'
  const max = defectPieData.value.reduce((a, b) => a.value > b.value ? a : b)
  return ((max.value / defectTotal.value) * 100).toFixed(2)
})

// ---------- 图表引用 ----------
const pieChartRef = ref(null)
const performanceChartRef = ref(null)
const salesChartRef = ref(null)
const processChartRef = ref(null)
let pieChart = null
let performanceChart = null
let salesChart = null
let processChart = null

// 全屏相关
const isFullscreen = ref(false)
const toggleFullscreen = async () => {
  const element = document.querySelector('.screen-container')
  if (!document.fullscreenElement) {
    try {
      await element.requestFullscreen()
      isFullscreen.value = true
    } catch (err) {
      ElMessage.error('全屏失败：' + err.message)
    }
  } else {
    await document.exitFullscreen()
    isFullscreen.value = false
  }
}
const handleFullscreenChange = () => {
  isFullscreen.value = !!document.fullscreenElement
}

// ---------- 数据获取函数（适配 DRF）----------
const fetchDefectRecords = async () => {
  try {
    const res = await requestUtil.get('/api/screen/defect-records')
    defectRecords.value = res.data   // 直接就是数组
  } catch (error) {
    console.error('获取不良记录失败', error)
    ElMessage.error('获取不良记录失败')
  }
}

const fetchWorkOrders = async () => {
  try {
    const res = await requestUtil.get('/api/screen/work-orders')
    workOrders.value = res.data
  } catch (error) {
    console.error('获取工单失败', error)
    ElMessage.error('获取工单失败')
  }
}

const fetchDefectPie = async () => {
  try {
    const res = await requestUtil.get('/api/screen/defect-pie')
    defectPieData.value = res.data
    if (pieChart) pieChart.setOption({ series: [{ data: defectPieData.value }] })
  } catch (error) {
    console.error('获取饼图数据失败', error)
  }
}

const fetchTop5Performance = async () => {
  try {
    const res = await requestUtil.get('/api/screen/top5-performance')
    const data = res.data
    const names = data.map(item => item.name)
    const values = data.map(item => item.value)
    performanceData.value = { names, values }
    if (performanceChart) {
      performanceChart.setOption({
        yAxis: { data: names },
        series: [{ data: values }]
      })
    }
  } catch (error) {
    console.error('获取绩效数据失败', error)
  }
}

const fetchSalesRatio = async () => {
  try {
    const res = await requestUtil.get('/api/screen/sales-order-ratio')
    salesData.value = res.data
    if (salesChart) salesChart.setOption({ series: [{ data: salesData.value }] })
  } catch (error) {
    console.error('获取销售占比失败', error)
  }
}

const fetchProcessTop5 = async () => {
  try {
    const res = await requestUtil.get('/api/screen/process-top5')
    const data = res.data
    const names = data.map(item => item.name)
    const values = data.map(item => item.value)
    processData.value = { names, values }
    if (processChart) {
      processChart.setOption({
        yAxis: { data: names },
        series: [{ data: values }]
      })
    }
  } catch (error) {
    console.error('获取工序计划失败', error)
  }
}

const loadAllData = async () => {
  await Promise.all([
    fetchDefectRecords(),
    fetchWorkOrders(),
    fetchDefectPie(),
    fetchTop5Performance(),
    fetchSalesRatio(),
    fetchProcessTop5()
  ])
}

// ---------- 图表初始化 ----------
const initCharts = () => {
  if (pieChartRef.value) {
    pieChart = echarts.init(pieChartRef.value)
    pieChart.setOption({
      tooltip: { trigger: 'item' },
      legend: { orient: 'vertical', left: 'left', textStyle: { color: '#fff' } },
      series: [{ type: 'pie', radius: '50%', data: [], label: { show: false } }]
    })
  }
  if (performanceChartRef.value) {
    performanceChart = echarts.init(performanceChartRef.value)
    performanceChart.setOption({
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { containLabel: true, left: '10%' },
      xAxis: { type: 'value', axisLabel: { color: '#fff' } },
      yAxis: { type: 'category', data: [], axisLabel: { color: '#fff' } },
      series: [{ type: 'bar', data: [], itemStyle: { color: '#409EFF' } }]
    })
  }
  if (salesChartRef.value) {
    salesChart = echarts.init(salesChartRef.value)
    salesChart.setOption({
      tooltip: { trigger: 'item' },
      legend: { orient: 'vertical', left: 'left', textStyle: { color: '#fff' } },
      series: [{ type: 'pie', radius: ['40%', '70%'], data: [], label: { show: false } }]
    })
  }
  if (processChartRef.value) {
    processChart = echarts.init(processChartRef.value)
    processChart.setOption({
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { containLabel: true, left: '12%' },
      xAxis: { type: 'value', axisLabel: { color: '#fff' } },
      yAxis: { type: 'category', data: [], axisLabel: { color: '#fff' } },
      series: [{ type: 'bar', data: [], itemStyle: { color: '#67C23A' } }]
    })
  }
}

const handleResize = () => {
  pieChart?.resize()
  performanceChart?.resize()
  salesChart?.resize()
  processChart?.resize()
}

let timer = null
const startAutoRefresh = () => {
  timer = setInterval(loadAllData, 30000)
}

onMounted(() => {
  initCharts()
  loadAllData()
  startAutoRefresh()
  window.addEventListener('resize', handleResize)
  document.addEventListener('fullscreenchange', handleFullscreenChange)
})

onUnmounted(() => {
  clearInterval(timer)
  window.removeEventListener('resize', handleResize)
  document.removeEventListener('fullscreenchange', handleFullscreenChange)
  pieChart?.dispose()
  performanceChart?.dispose()
  salesChart?.dispose()
  processChart?.dispose()
})
</script>
<style scoped>
.screen-container {
  background: #0a2b3c;
  color: #fff;
  height: 100vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  box-sizing: border-box;
}

.screen-header {
  position: relative;
  text-align: center;
  padding: 10px 20px;
  background: rgba(0,0,0,0.3);
  flex-shrink: 0;
}
.screen-header h1 {
  margin: 0;
  font-size: 28px;
  color: #fff;
}
.fullscreen-btn {
  position: absolute;
  right: 20px;
  top: 50%;
  transform: translateY(-50%);
  background: rgba(255,255,255,0.2);
  border: none;
  color: #fff;
}
.fullscreen-btn:hover {
  background: rgba(255,255,255,0.3);
}

/* 三行等分区域 */
.dashboard-rows {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-height: 0;           /* 关键：允许子元素收缩 */
}
.row {
  flex: 1;
  display: flex;
  gap: 20px;
  padding: 10px;
  min-height: 0;           /* 关键：允许卡片内容滚动 */
}
.row-1 .card,
.row-2 .card,
.row-3 .card {
  flex: 1;
  min-width: 0;
  overflow: auto;
  display: flex;
  flex-direction: column;
}

/* 卡片样式 */
.card {
  background: rgba(255,255,255,0.1);
  border-radius: 8px;
  padding: 12px;
  backdrop-filter: blur(5px);
  display: flex;
  flex-direction: column;
  overflow: hidden;        /* 改为 hidden，由内部元素控制滚动 */
}
.card-title {
  font-size: 18px;
  font-weight: bold;
  margin-bottom: 12px;
  border-left: 4px solid #409EFF;
  padding-left: 10px;
  flex-shrink: 0;
}

/* 表格容器：自适应高度，内部滚动 */
.auto-height-table {
  flex: 1;
  min-height: 0;
  overflow: auto;
}
/* 让 el-table 本身占满容器，内部 tbody 滚动 */
::v-deep .el-table {
  height: 100% !important;
  display: flex;
  flex-direction: column;
}
::v-deep .el-table__inner-wrapper {
  height: 100% !important;
  display: flex;
  flex-direction: column;
}
::v-deep .el-table__body-wrapper {
  flex: 1 !important;
  overflow-y: auto !important;
}

/* 图表容器自适应 */
.pie-chart, .bar-chart {
  flex: 1;
  min-height: 0;
  width: 100%;
}

/* 不良品摘要 */
.defect-card .defect-summary {
  display: flex;
  justify-content: space-between;
  margin-bottom: 12px;
  background: rgba(0,0,0,0.3);
  padding: 8px;
  border-radius: 6px;
  flex-shrink: 0;
}
.highlight {
  font-size: 20px;
  font-weight: bold;
  color: #f56c6c;
  margin-left: 5px;
}

/* 表格样式覆盖 */
::v-deep .el-table {
  background-color: transparent;
  color: #fff;
}
::v-deep .el-table th {
  background-color: rgba(0,0,0,0.3);
  color: #fff;
}
::v-deep .el-table tr {
  background-color: transparent;
}
::v-deep .el-table td {
  border-color: rgba(255,255,255,0.2);
  color: #fff;
}
::v-deep .el-table--striped .el-table__body tr.el-table__row--striped td {
  background-color: rgba(0,0,0,0.2);
}
::v-deep .el-table__body tr:hover > td {
  background-color: rgba(0,0,0,0.4);
}
</style>