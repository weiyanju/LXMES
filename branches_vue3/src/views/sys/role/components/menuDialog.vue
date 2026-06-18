<template>
  <el-dialog
      :model-value="menuDialogVisible"
      title="分配权限"
      width="30%"
      @close="handleClose"
  >
    <el-form ref="formRef" :model="form" label-width="100px">
      <el-tree
          ref="treeRef"
          :data="treeData"
          :props="defaultProps"
          show-checkbox
          :default-expand-all="true"
          node-key="id"
          :check-strictly="true"
      />
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
import { defineEmits, defineProps, ref, watch } from "vue";
import requestUtil from "@/util/request";
import { ElMessage } from 'element-plus'

const defaultProps = {
  children: 'children',
  label: 'name'
}

const props = defineProps({
  id: {
    type: Number,
    default: null,
    required: true
  },
  menuDialogVisible: {
    type: Boolean,
    default: false,
    required: true
  }
})

const form = ref({
  id: null
})

const treeData = ref([])
const treeRef = ref(null)

// 获取菜单树（临时使用旧接口，后续 menu 模块改造后统一）
const fetchMenuTree = async () => {
  try {
    const res = await requestUtil.get('/api/menu/treeList')
    // 兼容可能的响应格式
    if (res.data.treeList) {
      treeData.value = res.data.treeList
    } else if (Array.isArray(res.data)) {
      treeData.value = res.data
    } else {
      treeData.value = res.data.results || []
    }
  } catch (error) {
    ElMessage.error('获取菜单树失败')
    console.error(error)
  }
}

// 获取角色已有菜单权限（新接口）
const fetchRoleMenus = async (roleId) => {
  try {
    const res = await requestUtil.get(`/api/role/roles/${roleId}/menus/`)
    // 响应格式为 { menuIds: [1,2,3] } 或 { menuIdList: [...] }，兼容处理
    const menuIds = res.data.menuIds || res.data.menuIdList || []
    treeRef.value?.setCheckedKeys(menuIds)
  } catch (error) {
    ElMessage.error('获取角色权限失败')
    console.error(error)
  }
}

const initFormData = async (roleId) => {
  form.value.id = roleId
  await fetchMenuTree()
  // 等待树渲染完成后设置选中状态
  await new Promise(resolve => setTimeout(resolve, 100))
  await fetchRoleMenus(roleId)
}

watch(
    () => props.menuDialogVisible,
    async (newVal) => {
      if (newVal && props.id) {
        await initFormData(props.id)
      }
    }
)

const emits = defineEmits(['update:modelValue', 'initRoleList'])

const handleClose = () => {
  emits('update:modelValue', false)
}

const handleConfirm = async () => {
  const menuIds = treeRef.value?.getCheckedKeys() || []
  if (!props.id) {
    ElMessage.error('角色ID无效')
    return
  }

  try {
    await requestUtil.post(`/api/role/roles/${props.id}/grant-menus/`, {
      menuIds
    })
    ElMessage.success('权限分配成功')
    emits('initRoleList')
    handleClose()
  } catch (error) {
    const msg = error.response?.data?.detail || '权限分配失败'
    ElMessage.error(msg)
  }
}
</script>