--INSERT INTO [BranchSTAverageServiceTime] ([ServiceTypeName], [AverageServiceTime], [Branch], [Region], [Company]) 
SELECT x.[ServiceTypeName], AVG(DATEDIFF(SECOND, '19000101', y.[EventQueueTimeInsert] - x.[EventQueueTimeInsert])), x.[BranchName], x.[RegionName], x.[CompanyName] From 
(SELECT  [ServiceTypeName], [QueueId], [EventQueueTimeInsert], [BranchName], [RegionName], [CompanyName], [SessionId] From [Queue_Event_Shaddow] 
left Join [ServiceType] on [Queue_Event_Shaddow].QueueServiceType = [ServiceType].ServiceTypeId 
left Join [Branch] on [ServiceType].[ServiceTypeBranch] = [Branch].[BranchId]
left join [Region] on [Branch].[BranchRegion] = [Region].[RegionId] 
left join [Company] on [Region].[RegionCompany] = [Company].[CompanyId]
WHERE [EventId] = 7 
and [ServiceTypeName] in (SELECT [ServiceTypeName] from [ServiceType])
) x, 
(SELECT  [ServiceTypeName], [QueueId], [EventQueueTimeInsert], [BranchName], [RegionName], [CompanyName], [SessionId] From [Queue_Event_Shaddow] 
left Join [ServiceType] on[Queue_Event_Shaddow].QueueServiceType = [ServiceType].ServiceTypeId 
left Join [Branch] on [ServiceType].[ServiceTypeBranch] = [Branch].[BranchId]
left join [Region] on [Branch].[BranchRegion] = [Region].[RegionId] 
left join [Company] on [Region].[RegionCompany] = [Company].[CompanyId]
WHERE ([EventId] = 8 or [EventId] = 18) 
and [ServiceTypeName] in (SELECT [ServiceTypeName] from [ServiceType])
) y 
WHERE x.[QueueId] = y.[QueueId] and x.EventQueueTimeInsert < y.EventQueueTimeInsert and x.SessionId = y.SessionId
GROUP BY x.[ServiceTypeName], x.[BranchName], x.[RegionName], x.[CompanyName]
order by x.[BranchName]

