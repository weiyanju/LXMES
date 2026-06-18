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
      <el-form-item label="用户名" prop="username">
        <el-input v-model="form.username" :disabled="form.id ? true : false"/>
        <el-alert
                v-if="!form.id"
                title="默认初始密码：123456"
                :closable="false"
                style="line-height: 10px;"
                type="success">
        </el-alert>
      </el-form-item>

      <el-form-item label="手机号" prop="phonenumber">
        <el-input v-model="form.phonenumber"/>
      </el-form-item>

      <el-form-item label="邮箱" prop="email">
        <el-input v-model="form.email"/>
      </el-form-item>

      <!-- 所属部门多选框 -->
      <el-form-item label="所属部门" prop="departments">
        <el-select v-model="form.departments" multiple placeholder="请选择部门">
          <el-option
                  v-for="dept in departmentOptions"
                  :key="dept.id"
                  :label="dept.name"
                  :value="dept.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="状态" prop="status">
        <el-radio-group v-model="form.status">
          <el-radio :label="1">正常</el-radio>
          <el-radio :label="0">禁用</el-radio>
        </el-radio-group>
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

  // 表单数据，部门字段改为 departments（ID 数组）
  const form = ref({
    id: null,
    username: "",
    password: "123456",
    status: 1,
    phonenumber: "",
    email: "",
    remark: "",
    departments: []    // 部门 ID 数组
  })

  const departmentOptions = ref([])

  // 加载部门列表（GET 请求，分页参数足够大以获取全部）
  const fetchDepartments = async () => {
    try {
      const res = await requestUtil.get('/api/department/departments/', { page_size: 100 })
      departmentOptions.value = res.data.results
    } catch (error) {
      ElMessage.error('获取部门列表失败')
    }
  }

  // 用户名查重（DRF 自定义 action）
  const checkUsername = async (rule, value, callback) => {
    if (!form.value.id) {  // 新增时才校验
      try {
        const res = await requestUtil.post('/api/user/users/check-username/', { username: value })
        if (res.data.exists) {
          callback(new Error("用户名已存在！"))
        } else {
          callback()
        }
      } catch (error) {
        callback(new Error("校验失败，请稍后重试"))
      }
    } else {
      callback()
    }
  }

  const rules = ref({
    username: [
      { required: true, message: '请输入用户名', trigger: 'blur' },
      { validator: checkUsername, trigger: 'blur' }
    ],
    email: [
      { required: true, message: "邮箱地址不能为空", trigger: "blur" },
      { type: "email", message: "请输入正确的邮箱地址", trigger: ["blur", "change"] }
    ],
    phonenumber: [
      { required: true, message: "手机号码不能为空", trigger: "blur" },
      { pattern: /^1[3-9]\d{9}$/, message: "请输入正确的手机号码", trigger: "blur" }
    ],
  })

  const formRef = ref(null)

  // 获取用户详情（GET /api/user/users/{id}/）
  const initFormData = async (id) => {
    try {
      const res = await requestUtil.get(`/api/user/users/${id}/`)
      const userData = res.data
      form.value = {
        id: userData.id,
        username: userData.username,
        password: userData.password,
        status: userData.status,
        phonenumber: userData.phonenumber,
        email: userData.email,
        remark: userData.remark,
        departments: userData.departments || []   // 后端返回的部门 ID 数组
      }
    } catch (error) {
      ElMessage.error('获取用户详情失败')
    }
  }

  watch(
          () => props.dialogVisible,
          async (newVal) => {
            if (newVal) {
              await fetchDepartments()
              // 只有当 id 有效（存在且不为 -1）时才获取详情
              if (props.id && props.id !== -1) {
                await initFormData(props.id)
              } else {
                form.value = {
                  id: null,
                  username: "",
                  password: "123456",
                  status: 1,
                  phonenumber: "",
                  email: "",
                  remark: "",
                  departments: []
                }
              }
            }
          }
  )

  const emits = defineEmits(['update:modelValue', 'initUserList'])

  const handleClose = () => {
    emits('update:modelValue', false)
  }

  const handleConfirm = () => {
    formRef.value.validate(async (valid) => {
      if (!valid) return

      const payload = {
        username: form.value.username,
        email: form.value.email,
        phonenumber: form.value.phonenumber,
        status: form.value.status,
        remark: form.value.remark?.trim() || null,   // 空字符串转为 null
        departments: form.value.departments
      }
      if (!form.value.id) {
        payload.password = form.value.password
      }

      try {
        if (form.value.id) {
          await requestUtil.patch(`/api/user/users/${form.value.id}/`, payload)
          ElMessage.success('修改成功')
        } else {
          await requestUtil.post('/api/user/users/', payload)
          ElMessage.success('新增成功')
        }
        formRef.value.resetFields()
        emits('initUserList')
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