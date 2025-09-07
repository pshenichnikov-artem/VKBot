ALTER TABLE "Messages" 
ALTER COLUMN "LastReminderSent" TYPE timestamp without time zone USING "LastReminderSent"::timestamp without time zone;

ALTER TABLE "MessageDeliveries"
ALTER COLUMN "DispatchTime" TYPE timestamp without time zone USING "DispatchTime"::timestamp without time zone;