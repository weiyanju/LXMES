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
      <el-form-item label="角色名称" prop="name">
        <el-input v-model="form.name"/>
      </el-form-item>

      <el-form-item label="权限字符" prop="code">
        <el-input v-model="form.code"/>
      </el-form-item>

      <el-form-item label="备注" prop="remark">
        <el-input v-model="form.remark" type="textarea" :rows="4"/>
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
import { defineEmits, defineProps, ref, watch } from "vue";
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
  }
})

const form = ref({
  id: null,
  name: "",
  code: "",
  remark: ""
})

const rules = ref({
  name: [{ required: true, message: '请输入角色名称', trigger: 'blur' }],
  code: [{ required: true, message: '请输入权限字符', trigger: 'blur' }]
})

const formRef = ref(null)

// 获取角色详情（GET /api/role/roles/{id}/）
const initFormData = async (id) => {
  try {
    const res = await requestUtil.get(`/api/role/roles/${id}/`)
    form.value = { ...res.data }
  } catch (error) {
    ElMessage.error('获取角色详情失败')
    console.error(error)
  }
}

watch(
    () => props.dialogVisible,
    () => {
      if (props.id) {
        initFormData(props.id)
      } else {
        form.value = {
          id: null,
          name: "",
          code: "",
          remark: ""
        }
      }
    }
)

const emits = defineEmits(['update:modelValue', 'initRoleList'])

const handleClose = () => {
  emits('update:modelValue', false)
}

const handleConfirm = () => {
  formRef.value.validate(async (valid) => {
    if (!valid) return

    const payload = {
      name: form.value.name,
      code: form.value.code,
      remark: form.value.remark?.trim() || null   // 空字符串转 null，避免后端校验错误
    }

    try {
      if (form.value.id) {
        // 修改：PATCH /api/role/roles/{id}/
        await requestUtil.patch(`/api/role/roles/${form.value.id}/`, payload)
        ElMessage.success('修改成功')
      } else {
        // 新增：POST /api/role/roles/
        await requestUtil.post('/api/role/roles/', payload)
        ElMessage.success('新增成功')
      }
      formRef.value.resetFields()
      emits('initRoleList')
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