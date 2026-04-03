import axios from 'axios'

const axiosInstance = axios.create({
  baseURL: 'http://localhost:8002',
})

axiosInstance.interceptors.request.use(
  (config) => {
    const token = sessionStorage.getItem('token')

    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }

    return config
  },
  (error) => Promise.reject(error),
)

axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error?.response?.status === 401 && window.location.pathname !== '/login') {
      window.location.href = '/login'
    }

    return Promise.reject(error)
  },
)

export default axiosInstance
