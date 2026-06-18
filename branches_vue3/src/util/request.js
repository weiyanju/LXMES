// 引入axios
import axios from 'axios';
import { ElMessage } from 'element-plus';

// 从环境变量获取后端 API 基础地址，开发环境可配置为 http://127.0.0.1:8000 或局域网 IP
const baseUrl = process.env.VUE_APP_API_BASE_URL || '';

// 创建axios实例
const httpService = axios.create({
    baseURL: baseUrl,
    timeout: 10000
});

// ===================== 请求拦截器 =====================
httpService.interceptors.request.use(function (config) {
    // JWT 认证：从 sessionStorage 获取 access_token，并添加到 Authorization 头
    const token = window.sessionStorage.getItem('access_token');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
}, function (error) {
    return Promise.reject(error);
});

// ===================== 响应拦截器 =====================
httpService.interceptors.response.use(
    function (response) {
        // 直接返回响应数据，业务层可通过 response.data 获取内容
        return response;
    },
    async function (error) {
        const originalRequest = error.config;
        const { response } = error;

        // 如果是 401 且未重试过，尝试刷新 token
        if (response && response.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;
            const refreshToken = window.sessionStorage.getItem('refresh_token');
            if (refreshToken) {
                try {
                    // 调用刷新接口（使用绝对路径避免 baseURL 重复）
                    const refreshRes = await axios.post(
                        (baseUrl || '') + '/api/token/refresh/',
                        { refresh: refreshToken }
                    );
                    const newAccessToken = refreshRes.data.access;
                    // 存储新 access_token
                    window.sessionStorage.setItem('access_token', newAccessToken);
                    // 更新原请求的 Authorization 头
                    originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
                    // 重试原请求
                    return httpService(originalRequest);
                } catch (refreshError) {
                    // 刷新失败，清除凭证并提示
                    window.sessionStorage.removeItem('access_token');
                    window.sessionStorage.removeItem('refresh_token');
                    ElMessage.error('登录已过期，请重新登录');
                    // 可根据项目路由跳转到登录页（如需自动跳转，可导入 router 后使用）
                    // router.push('/login');
                    return Promise.reject(refreshError);
                }
            } else {
                // 没有 refresh_token，直接清除 access_token 并提示
                window.sessionStorage.removeItem('access_token');
                ElMessage.error('登录已过期，请重新登录');
                // router.push('/login');
            }
        }

        // 原有其他错误处理
        if (response) {
            const { status, data } = response;
            switch (status) {
                case 403:
                    ElMessage.error('没有权限执行此操作');
                    break;
                case 400:
                    // 字段校验错误等，具体消息由业务组件处理，此处不重复提示
                    break;
                default:
                    // 其他错误，优先展示 DRF 返回的 detail 字段
                    ElMessage.error(data?.detail || `请求失败 (${status})`);
            }
        } else {
            // 网络错误或超时
            ElMessage.error('网络连接异常，请稍后重试');
        }
        return Promise.reject(error);
    }
);

// ===================== 封装请求方法 =====================

export function get(url, params = {}) {
    return httpService({ url, method: 'get', params });
}

export function post(url, data = {}) {
    return httpService({ url, method: 'post', data });
}

export function put(url, data = {}) {
    return httpService({ url, method: 'put', data });
}

export function patch(url, data = {}) {
    return httpService({ url, method: 'patch', data });
}

export function del(url, data = {}) {
    return httpService({ url, method: 'delete', data });
}

export function fileUpload(url, data = {}) {
    return httpService({
        url,
        method: 'post',
        data,
        headers: { 'Content-Type': 'multipart/form-data' }
    });
}

export function getServerUrl() {
    return baseUrl;
}

export const getMediaUrl = (relativePath) => {
    const base = process.env.VUE_APP_MEDIA_BASE_URL || getServerUrl() + '/media/'
    return base + relativePath
}

export default {
    get,
    post,
    put,
    patch,
    del,
    fileUpload,
    getServerUrl,
    getMediaUrl
};