INSERT INTO "Groups" ("Id", "Name", "Cohort", "GroupNumber")
VALUES (1, 'ИТ/б-22-1-о', 'ИТ/б-22-о', 1)
ON CONFLICT ("Id") DO UPDATE SET 
"Name" = EXCLUDED."Name",
"Cohort" = EXCLUDED."Cohort",
"GroupNumber" = EXCLUDED."GroupNumber";

INSERT INTO "Users" ("VkUserId", "FullName", "Role", "IsConfirmed", "IsBlocked", "GroupId")
VALUES (562436407, 'Боров Максим Геннадьевич', 'admin', true, false, 1)
ON CONFLICT ("VkUserId") DO UPDATE SET 
"FullName" = EXCLUDED."FullName",
"Role" = EXCLUDED."Role",
"IsConfirmed" = EXCLUDED."IsConfirmed",
"IsBlocked" = EXCLUDED."IsBlocked",
"GroupId" = EXCLUDED."GroupId";