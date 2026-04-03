import axiosInstance from './axiosInstance'

export const getAdminDashboard = () => axiosInstance.get('/admin/dashboard')
