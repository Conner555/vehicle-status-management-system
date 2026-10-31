-- 检查演示车辆
SELECT Id, PlateNumber, Model, Status
FROM dbo.Vehicles
WHERE PlateNumber LIKE N'辽B100%'
ORDER BY Id;

-- 检查每辆演示车的历史记录数
SELECT
    v.PlateNumber,
    v.Model,
    COUNT(r.Id) AS RecordCount
FROM dbo.Vehicles v
LEFT JOIN dbo.VehicleStatusRecords r
    ON v.Id = r.VehicleId
WHERE v.PlateNumber LIKE N'辽B100%'
GROUP BY v.Id, v.PlateNumber, v.Model
ORDER BY v.PlateNumber;