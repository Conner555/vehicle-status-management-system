
import api from './api'

// 查询待领取验车任务
export function getPendingInspections(params) {
  return api.get('/inspections/pending', {
    params
  })
}

// 领取验车任务
export function claimInspection(inspectionId) {
  return api.post(
    `/inspections/${inspectionId}/claim`
  )
}

// 提交验车结果
export function completeInspection(
  inspectionId,
  data
) {
  return api.post(
    `/inspections/${inspectionId}/complete`,
    data
  )
}

// 查询验车提醒
export function getInspectionReminders(params) {
  return api.get(
    '/operator/inspection-reminders',
    { params }
  )
}

//查询验车订单
export function getMyInspections(params) {
  return api.get('/inspections/mine', { params })
}


//查询历史验车订单
export function getInspectionHistory(params) {
  return api.get('/inspections/history', { params })
}
