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

`GET /api/v1/notifications/alert-types` returns the database catalog as `{id,name}`
entries for the administrator and superadministrator filter controls.
