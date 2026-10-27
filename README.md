# sg-ms-notification
## Administrator dashboard alerts

`GET /api/v1/notifications/alerts` reads persisted alerts. Administrators (role 1)
are restricted using their authenticated `sub` and active `School.SchoolAdmin`
assignments. Alerts belong to the school of their profile's campus; this scope
is applied before filtering, counting and pagination. A missing administrator
profile is rejected. Superadministrators retain global access.

Optional query parameters: `schoolId` (only narrows the authorized scope),
`alertTypeId` (the actual catalog ID), `search` (description), `page` (zero-based),
`size` (1-100), `sort` (`timestamp,asc` or `timestamp,desc`), `acknowledged`,
`dateFrom` and `dateTo`. Legacy `alertType=ON_BOARD|OFF_BOARD` remains supported.
List entries include `alertTypeId` and `alertTypeName`.

`GET /api/v1/notifications/alert-types` returns distinct `{id,name}` catalog
entries only for types used by persisted alerts in the authorized school scope.
Unused catalog types and types used only by another school are excluded.
Administrators use the same active school assignments as the alert list; the
optional `schoolId` parameter can only narrow that scope. Superadministrators
see types used across schools unless `schoolId` is supplied. Options are
independent of text filters and pagination; no alerts means an empty type list.

Docker Compose defaults the required gRPC directories to `ms-route:5001` and
`ms-user-management:5001` on `sg-services-network`. Override
`Grpc__Route__Address` / `Grpc__Family__Address` in the environment when deploying
those services elsewhere. SQL Server and the directories must be running before
testing the populated alert list.
