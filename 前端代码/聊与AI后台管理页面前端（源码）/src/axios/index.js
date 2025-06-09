import axios from "axios";
  const Url = localStorage.getItem('Url');

// http://192.168.0.100:3001
const instance = axios.create({
    baseURL: Url,
    timeout: 5000
})

//拦截器
// axios请求拦截器
instance.interceptors.request.use(config => {
    return config
}, e => Promise.reject(e))

// axios响应式拦截器
instance.interceptors.response.use(res => res.data, e => {
    return Promise.reject(e)
})

export default instance