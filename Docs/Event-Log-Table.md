# Understanding the `PATCHWORK_EVENT_LOG` Table

Patchwork automatically builds an event log that maintains a complete audit history of all entities in the system. This is a core feature of the Patchwork toolkit and the audit log is based entirely on the [JSON-PATCH](https://jsonpatch.com/) specification; so much so that it inspired the name of the framework.

The basic idea is that any time a data modification is made to the entities in the system, Patchwork will calculate a `JSON-PATCH` document that captures the data change and add it to the event log. This means that if you were to query all the event log records for a given entity identifier and apply the `JSON-PATCH` operations to an empty `JSON` object in order, the result will be the current state of the entity as it appears in the database.

## Log Table Schema

The event log database table has the these columns:

| Column Name | Data Type   | Description                                                                       | Example                      |
| ----------- | ----------- | --------------------------------------------------------------------------------- | ---------------------------- |
| pk          | BigInt      | A serial integer that is the primary key and keeps record sequence automatically. | 1123                         |
| event_time  | TimeStamp   | A timestamp for when this event log was created.                                  | 2023-11-12T02:06:34.1258302Z |
| entity_name | varchar(64) | The table name that holds the entity.                                             | surveys.template             |
| entity_pk   | varchar(64) | An identifier that indicates when entity this log entry applies to.               | CS6000_t11634                |
| json_patch  | JSONB       | A JSON-PATCH document describing a data change.
