import axiosInstance from './axiosInstance'

export const login = (payload) => axiosInstance.post('/auth/login', payload)
export const signup = (payload) => axiosInstance.post('/auth/signup', payload)
