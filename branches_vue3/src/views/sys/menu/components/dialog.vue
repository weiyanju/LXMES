<template>
  <el-dialog
      :model-value="dialogVisible"
      :title="dialogTitle"
      width="30%"
      @close="handleClose"
  >
    <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="100px"
    >
      <el-form-item label="上级菜单" prop="parent_id">
        <el-select v-model="form.parent_id" placeholder="请选择上级菜单" clearable>
          <el-option label="根目录" :value="null" />
          <template v-for="item in flatMenuOptions" :key="item.id">
            <el-option :label="item.label" :value="item.id" />
          </template>
        </el-select>
      </el-form-item>

      <el-form-item label="菜单类型" prop="menu_type">
        <el-radio-group v-model="form.menu_type">
          <el-radio v-if="availableMenuTypes.includes('M')" label="M">目录</el-radio>
          <el-radio v-if="availableMenuTypes.includes('C')" label="C">菜单</el-radio>
        </el-radio-group>
        <div v-if="availableMenuTypes.length === 0" style="color: #999; font-size: 12px;">
          菜单下不能添加子节点
        </div>
      </el-form-item>

      <el-form-item label="菜单图标" prop="icon">
        <el-input v-model="form.icon" placeholder="例如：user、setting" />
      </el-form-item>

      <el-form-item label="菜单名称" prop="name">
        <el-input v-model="form.name" />
      </el-form-item>

      <el-form-item label="权限标识" prop="perms">
        <el-input v-model="form.perms" placeholder="例如：system:user:list" />
      </el-form-item>

      <el-form-item label="路由路径" prop="path">
        <el-input v-model="form.path" placeholder="例如：/sys/user" />
      </el-form-item>

      <el-form-item label="组件路径" prop="component">
        <el-input v-model="form.component" placeholder="例如：sys/user/index" />
      </el-form-item>

      <el-form-item label="显示顺序" prop="order_num">
        <el-input-number v-model="form.order_num" :min="1" />
      </el-form-item>

      <el-form-item label="备注" prop="remark">
        <el-input v-model="form.remark" type="textarea" :rows="3" />
      </el-form-item>
    </el-form>

    <template #footer>
      <span class="dialog-footer">
        <el-button type="primary" @click="handleConfirm">确认</el-button>
        <el-button @click="handleClose">取消</el-button>
      </span>
    </template>
  </el-dialog>
</template>

<script setup>
import { defineEmits, defineProps, ref, watch, computed } from "vue";
import requestUtil from "@/util/request";
import { ElMessage } from 'element-plus'

const props = defineProps({
  id: {
    type: Number,
    default: null,
    required: true
  },
  dialogTitle: {
    type: String,
    default: '',
    required: true
  },
  dialogVisible: {
    type: Boolean,
    default: false,
    required: true
  },
  tableData: {
    type: Array,
    default: () => [],
    required: true
  }
})

// 扁平化树形菜单用于下拉选择（带缩进）
const flatMenuOptions = computed(() => {
  const result = []
  const flatten = (nodes, level = 0) => {
    nodes.forEach(node => {
      result.push({
        id: node.id,
        label: '　'.repeat(level) + node.name
      })
      if (node.children && node.children.length) {
        flatten(node.children, level + 1)
      }
    })
  }
  flatten(props.tableData)
  return result
})

// 构建菜单ID映射表
const menuMap = computed(() => {
  const map = {}
  const flatten = (nodes) => {
    nodes.forEach(node => {
      map[node.id] = node
      if (node.children) flatten(node.children)
    })
  }
  flatten(props.tableData)
  return map
})

const form = ref({
  id: null,
  parent_id: null,
  menu_type: 'M',
  icon: '',
  name: '',
  perms: '',
  path: '',
  component: '',
  order_num: 1,
  remark: ''
})

// 获取当前选中的上级菜单类型
const parentMenuType = computed(() => {
  if (!form.value.parent_id) return null
  return menuMap.value[form.value.parent_id]?.menu_type || null
})

// 根据上级菜单类型决定可选的菜单类型
const availableMenuTypes = computed(() => {
  if (!form.value.parent_id) {
    // 根目录 → 只能目录
    return ['M']
  }
  if (parentMenuType.value === 'M') {
    // 普通目录下 → 目录或菜单
    return ['M', 'C']
  }
  // 菜单下不能添加子节点
  return []
})

// 监听 parent_id 变化，自动调整 menu_type 为第一个合法值
watch(
    () => form.value.parent_id,
    () => {
      const allowed = availableMenuTypes.value
      if (!allowed.includes(form.value.menu_type)) {
        form.value.menu_type = allowed.length ? allowed[0] : ''
      }
    }
)

const rules = ref({
  name: [{ required: true, message: '菜单名称不能为空', trigger: 'blur' }],
  menu_type: [{ required: true, message: '请选择菜单类型', trigger: 'change' }]
})

const formRef = ref(null)

// 获取菜单详情
const initFormData = async (id) => {
  try {
    const res = await requestUtil.get(`/api/menu/menus/${id}/`)
    const data = res.data
    form.value = {
      id: data.id,
      parent_id: data.parent_id || null,
      menu_type: data.menu_type,
      icon: data.icon || '',
      name: data.name,
      perms: data.perms || '',
      path: data.path || '',
      component: data.component || '',
      order_num: data.order_num || 1,
      remark: data.remark || ''
    }
  } catch (error) {
    ElMessage.error('获取菜单详情失败')
    console.error(error)
  }
}

watch(
    () => props.dialogVisible,
    (newVal) => {
      if (newVal) {
        if (props.id) {
          initFormData(props.id)
        } else {
          form.value = {
            id: null,
            parent_id: null,
            menu_type: 'M',
            icon: '',
            name: '',
            perms: '',
            path: '',
            component: '',
            order_num: 1,
            remark: ''
          }
        }
      }
    }
)

const emits = defineEmits(['update:modelValue', 'initMenuList'])

const handleClose = () => {
  emits('update:modelValue', false)
}

const handleConfirm = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return

    // 如果无可用菜单类型（菜单下添加子节点），阻止提交
    if (availableMenuTypes.value.length === 0) {
      ElMessage.error('菜单下不能添加子节点')
      return
    }

    const payload = {
      name: form.value.name,
      icon: form.value.icon || '',
      parent_id: form.value.parent_id,
      order_num: form.value.order_num,
      path: form.value.path || '',
      component: form.value.component || '',
      menu_type: form.value.menu_type,
      perms: form.value.perms || '',
      remark: form.value.remark?.trim() || null
    }

    try {
      if (form.value.id) {
        await requestUtil.patch(`/api/menu/menus/${form.value.id}/`, payload)
        ElMessage.success('修改成功')
      } else {
        await requestUtil.post('/api/menu/menus/', payload)
        ElMessage.success('新增成功')
      }
      formRef.value.resetFields()
      emits('initMenuList')
      handleClose()
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
  })
}
</script>

<style scoped>
/* 可自行添加样式 */
</style>