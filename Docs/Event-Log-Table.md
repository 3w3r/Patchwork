# Understanding the `PATCHWORK_EVENT_LOG` Table

Patchwork automatically builds an event log that maintains a complete audit history of all entities in the system. This is a core feature of the Patchwork toolkit and the audit log is based entirely on the [JSON-PATCH](https://jsonpatch.com/) specification; so much so that it inspired the name of the framework.

The basic idea is that any time a data modification is made to the entities in the system, Patchwork will calculate a `JSON-PATCH` document that captures the data change and add it to the event log. This means that if you were to query all the event log records for a given entity identifier and apply the `JSON-PATCH` operations to an empty `JSON` object in order, the result will be the current state of the entity as it appears in the database.

## Log Table Schema

The event log database table has the these columns:

| Column Name | Data Type   | Description                                                                        | Example                                                         |
| ----------- | ----------- | ---------------------------------------------------------------------------------- | --------------------------------------------------------------- |
| pk          | BigInt      | A serial integer that is the primary key and keeps record sequence automatically.  | `1123`                                                          |
| event_time  | TimeStamp   | A timestamp for when this event log was created.                                   | `2023-11-12T02:06:34.1258302Z`                                  |
| collection  | varchar(64) | The URL path to find the collection containing the modified entity.                | `/surveys/template`                                             |
| entity_pk   | varchar(64) | The resource Id that, when appended to the collection URL, retrieves the resource. | `11634`                                                         |
| json_patch  | JSONB       | A JSON-PATCH document describing a data change.                                    | `[{ "op": "replace", "path": "title", "value": "Template B" }]` |

## How Events get Added to the Log

Patchwork automatically creates and appends log entries any time an entity in the system is modified. This includes add, update, and delete operations. In fact, the act of appending an event to the log is wrapped in the same transaction as the actual database modification so it is not possible to change an entity without successfully appending that change to the log.

### Entity POST Operations

The `HTTP POST` operation in Patchwork is used to append a new record to the database. The payload for the `POST` should be a JSON representation of the entity including all fields that the client needs to give values. But, some database fields are given default values at the time of insertion such as `SERIAL` or `TIMESTAMP` columns, so those may be omitted from the payload.

Patchwork will insert the new record, then query back the newly inserted entity so that it has all the generated default values. Then, Patchwork will insert a record into the even log where the JsonPath document contains a single `add` operation that adds the entity to the collection.

#### Example POST

Let's consider an example by starting with a schema for our entity. Here is the database table we are working with:
```sql
  CREATE TABLE products (
    ID SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Price NUMERIC(10, 2) NOT NULL
  );  
```

Now, let's assume that the client performs this `POST` operation to insert a new product:
```http
POST https://localhost/dbo/products
Content-Type: application/json

{ "Name": "Widget", "Price":"42.42" }
```

Patchwork will build this query to insert the new entity into the products table. Note that it includes the `RETURNING` clause so the newly inserted record is returned back to Patchwork for further work:
```sql
  INSERT INTO dbo.products (Name, Price)
  VALUES ('Widget Name Here', 42.42)
  RETURNING *;
```

Now, Patchwork will take the newly inserted record and form it into a `JSON` document such as this:
```json
  { "ID": "42", "Name": "Widget", "Price":"42.42" }
```

And this `JSON` document will be packaged into a `JSON-Patch` that indicates the record was just added. That would look like this:
This would cause Patchwork to insert an entry in the `Patchwork_Event_Log` that looks like this:
```json
[{ "op": "add", "path": "/dbo/products/42", "value": { "ID":"42", "Name": "Widget", "Price":"42.42" } }]
```

This would be the insert statement:
```sql
  INSERT INTO patchwork_event_log (collection, entity_pk, json_patch) VALUES 
  ('/dbo/products','42','[{ "op": "add", "path": "/dbo/products/42", "value": { "ID":"42", "Name": "Widget", "Price":"42.42" } }]')
```

Note that the `path` property shows the URL path to address this entity directly. This is the same URL path a client could use to `HTTP GET` the entity directly. Since the `op` (operation) is an `add` and the `value` is the `JSON` version of the entity, this `JSON-Patch` indicates that a new record was inserted.

### Entity PUT Operations

When a client uses the `HTTP PUT` operation Patchwork will replace the existing entity with the updated version from the payload of the `PUT` operation. Consider this `PUT` operation:
```http
PUT https://localhost/dbo/products/42
Content-Type: application/json

{ "ID": "42", "Name": "Widget (Discounted)", "Price":"39.99" }
```

In this case, Patchwork will create an event log entry to indicate the update occurred. The event log will have this `JSON-Patch` value:

```json
[
  { "op": "replace", "path": "/Name", "value": "Widget (Discounted)" },
  { "op": "replace", "path": "/Price", "value": "39.99" }
]
```

Note that this `JSON-Patch` does not include the full path needed to identify the entity's `HTTP GET` URL. Instead, the `path` only indicates the fields inside the entity. This is because the actual identifier for the entity is saved into the `entity_pk` column of the event log table like this:
```sql
  INSERT INTO patchwork_event_log (collection, entity_pk, json_patch) VALUES 
  ('/dbo/products','42','[{ "op": "replace", "path": "/Name", "value": "Widget (Discounted)" }, { "op": "replace", "path": "/Price", "value": "39.99" }]')
```

> **OPTIONAL FOR CONSIDERATION**
>
> We could create a separate log entry for each operation in the JSON Patch document like this:
> ```sql
>   INSERT INTO patchwork_event_log (collection, entity_pk, json_patch) VALUES 
>   ('/dbo/products','42','{ "op": "replace", "path": "/Name", "value": "Widget (Discounted)" }'),
>   ('/dbo/products','42','{ "op": "replace", "path": "/Price", "value": "39.99" }');
> ```

### Entity DELETE Operations

Continuing our example, let's consider a `DELETE` operation. When a client invokes the `HTTP DELETE` endpoint they are requesting the removal of an entity. Patchwork will append an entry to the event log with a `remove` operation.

Here is an example `HTTP DELETE` request:
```http
DELETE https://localhost/dbo/products/42
```

This `HTTP DELETE` will trigger Patchwork to create a `Json-Patch` and insert it into the log like this and wrap it in a transaction so the entity only gets removed _after_ the event log entry is created.
```sql
  BEGIN;
  DELETE FROM dbo.products WHERE ID = 42;
  INSERT INTO patchwork_event_log (collection, entity_pk, json_patch) VALUES 
    ('/dbo/products','42','{ "op": "remove", "path": "/dbo/products/42" }]');
  COMMIT;
```

### Entity PATCH Operations

The `HTTP PATCH` operation lines up with the normal behavior of Patchwork more closely than the other `REST` operations. When Patchwork gets a `PATCH` request, it simply saves the `JSON-Patch` document into the event log then applies the patch to the entity. Consider this `HTTP PATCH` request that is equivalent to the `HTTP PUT` operation shown above:
```http
PUT https://localhost/dbo/products/42
Content-Type: application/json-patch+json

[
  { "op": "replace", "path": "/Name", "value": "Widget (Discounted)" }, 
  { "op": "replace", "path": "/Price", "value": "39.99" }
]
```

This would trigger Patchwork to make issue this SQL command against the database:
```sql
BEGIN;
UPDATE dbo.products SET Name = 'Widget (Discounted)', Price = 39.99 WHERE ID = 42;
INSERT INTO patchwork_event_log (collection, entity_pk, json_patch) VALUES 
  ('/dbo/products','42','[{ "op": "replace", "path": "/Name", "value": "Widget (Discounted)" }, { "op": "replace", "path": "/Price", "value": "39.99" }]');
COMMIT;
```

Notice that the insertion to the Patchwork event log uses the `HTTP PATCH` body verbatim and it uses the URL of the request to set the `collection` and `entity_pk` columns. Then, the `UPDATE` statement applied to the `dbo.products` table updates only the fields listed in the `JSON-Patch` per the operations. Patchwork does the heavy lifting of converting the `JSON-Patch` into an SQL `UPDATE` statement for you.

### Bulk PATCH Operations

The last, and most interesting, HTTP endpoint to consider is the `HTTP PATCH` against the collection endpoint. In this case, the patch targets the entire collection endpoint so it can, potentially, modify any entity in the collection. This means that the calling client can batch together multiple `add`, `remove`, and/or `replace` operations against one or more entities. Patchwork figures out which entity to modify based on the `path` property. The `path` property of each operation **MUST** provide an indicator of the URL to access the entity the client wants to modify.

```http
PUT https://localhost/dbo/products
Content-Type: application/json-patch+json

[
  { "op": "add", "path": "/dbo/products/-", "value": { "ID":"-", "Name": "Widget", "Price":"42.42" } },
  { "op": "remove", "path": "/dbo/products/38" },
  { "op": "replace", "path": "/dbo/products/42/Price", "value": "39.99" }
]
```

In this example we see that the client has requested to insert a new record where the `ID` will be supplied by the database. Per [RFC6901], using the `-` character indicates that this record should be inserted at the end of the collection so it will be given a new `ID` value. This is functionally equivalent to performing an `HTTP POST` with the `value` as the body of the request.

The second operation requests Patchwork to `remove` the product with the `ID` of 38. This is functionally equivalent to performing and `HTTP DELETE https://localhost/dbo/products/38` request.

And the third operation instructs Patchwork to update the product with `ID` of 42. This operation should trigger Patchwork to issue an SQL command like `UPDATE dbo.products SET Price = 39.99 WHERE ID = 42;`

Because this `JSON-Patch` makes changes to three different entities from the `dbo.products` table, it means that Patchwork will need to insert three different records into the event log; one for each of the modified entities.

The `HTTP PATCH` above would result in this SQL command against the database:
```sql
BEGIN;
INSERT INTO dbo.products (Name, Price) VALUES ('Widget', 42.42) RETURNING *; -- Need to find that record 43 was inserted
DELETE FROM dbo.products WHERE ID = 38;
UPDATE dbo.products SET Price = 39.00 WHERE ID = 42;

INSERT INTO patchwork_event_log (collection, entity_pk, json_patch) VALUES 
   ('/dbo/products','43','{ "op": "add", "path": "/dbo/products/43", "value": "{ "ID":"-", "Name": "Widget", "Price":"42.42" }" }'),
   ('/dbo/products','38','{ "op": "remove", "path": "/dbo/products/38" }'),
   ('/dbo/products','42','{ "op": "replace", "path": "/Price", "value": "39.99" }');

COMMIT;
```

