<template>
  <el-dialog
      :model-value="roleDialogVisible"
      title="分配角色"
      width="30%"
      @close="handleClose"
  >
    <el-form ref="formRef" :model="form" label-width="100px">
      <el-checkbox-group v-model="form.checkedRoles">
        <el-checkbox
          v-for="role in roleOptions"
          :key="role.id"
          :label="role.id"
        >
          {{ role.name }}
        </el-checkbox>
      </el-checkbox-group>
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

const props = defineProps({
  id: {
    type: Number,
    default: null,
    required: true
  },
  roleDialogVisible: {
    type: Boolean,
    default: false,
    required: true
  },
  sysRoleList: {
    type: Array,
    default: () => [],
    required: true
  }
})

const form = ref({
  checkedRoles: []
})

const roleOptions = ref([])  // 所有可选角色
const formRef = ref(null)

// 获取所有角色列表
const fetchAllRoles = async () => {
  try {
    const res = await requestUtil.get('/api/role/roles/', { page_size: 100 })
    roleOptions.value = res.data.results
  } catch (error) {
    // 如果 /api/role/roles/ 暂不可用，可回退到旧接口
    console.warn('DRF角色接口不可用，尝试旧接口')
    const res = await requestUtil.get('role/listAll')
    roleOptions.value = res.data.roleList || []
  }
}

// 初始化已选角色
const initCheckedRoles = () => {
  if (props.sysRoleList && props.sysRoleList.length) {
    form.value.checkedRoles = props.sysRoleList.map(role => role.id)
  } else {
    form.value.checkedRoles = []
  }
}

watch(
  () => props.roleDialogVisible,
  async (newVal) => {
    if (newVal && props.id) {
      await fetchAllRoles()
      initCheckedRoles()
    }
  }
)

const emits = defineEmits(['update:modelValue', 'initUserList'])

const handleClose = () => {
  emits('update:modelValue', false)
}

const handleConfirm = async () => {
  if (!props.id) {
    ElMessage.error('用户ID无效')
    return
  }

  try {
    await requestUtil.post(`/api/user/users/${props.id}/grant-roles/`, {
      roleIds: form.value.checkedRoles
    })
    ElMessage.success('角色分配成功')
    emits('initUserList')
    handleClose()
  } catch (error) {
    const msg = error.response?.data?.detail || '角色分配失败'
    ElMessage.error(msg)
  }
}
</script>