import { createApp } from 'vue'
import SvgIcon from '@/icons'
import App from './App.vue'
import router from './router'
import store from './store'
import ElementPlus from 'element-plus'
// 国际化中文
import zhCn from 'element-plus/es/locale/lang/zh-cn'
import 'element-plus/dist/index.css'
import '@/assets/styles/border.css'
import '@/assets/styles/reset.css'

// 忽略 ResizeObserver 循环警告
const debounce = (fn, delay) => {
    let timer = null
    return function(...args) {
        if (timer) clearTimeout(timer)
        timer = setTimeout(() => fn.apply(this, args), delay)
    }
}
const _ResizeObserver = window.ResizeObserver
window.ResizeObserver = class ResizeObserver extends _ResizeObserver {
    constructor(callback) {
        callback = debounce(callback, 16)
        super(callback)
    }
}


const app=createApp(App)
SvgIcon(app);

const originalRemoveItem = sessionStorage.removeItem;
sessionStorage.removeItem = function(key) {
    if (key === 'access_token') {
        console.trace('access_token 被移除');
        debugger; // 自动断点
    }
    return originalRemoveItem.call(this, key);
};

app.use(store)

app.use(router)

app.use(ElementPlus, {
    locale: zhCn,
})
app.mount('#app')
