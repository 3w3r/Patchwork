# DELETE to Remove a Record

The `HTTP DELETE` operation is used to remove a record from the database. This endpoint is fairly simple as it has no parameters, no payload, and can only work on a single record at a time.

The calling client will need to include a payload that has all the fields of the current record even if they have not changed. This is because on the back end, the Patchwork toolkit will assume that it should change the existing record to look just like the payload. So, if one of the fields on the payload is omitted, it will be set to `NULL` on the server.

## URL Segments

The URL for replacing a record with an `HTTP DELETE` operation is as follows:

```
                ┏━ The name of the server hosting the REST API.
                ┃        ┏━ The name of the schema containing the table.
                ┃        ┃             ┏━ The name of the table.
                ┃        ┃             ┃            ┏━ The value of the primary key 
                ┃        ┃             ┃            ┃  column from this table.
                ▼        ▼             ▼            ▼
DELETE https://{server}/{schema name}/{table name}/{primary key value}
```

## Generating a JSON-PATCH for this change

When a DELETE operation happens, there are a few things that the Patchwork toolkit does to ensure data integrity. First, the DELETE operation will fail if the record named in the URL does not exist. Second, the delete request will fail if there are related tables with this record as a foreign key. 

- [ ] If a DELETE request targets a primary key value that does not exist, the Patchwork toolkit **must** return an `HTTP 404: Not Found` error.
- [ ] If a DELETE request targets a primary key value that does exist and has related records in another table with a foreign key then the Patchwork toolkit **must** return an `HTTP 405: Method Not Allowed`.
- [ ] The API **must not** permit any record to be removed unless a successful logging of the DELETE operation in the Patchwork Event Log was already been completed.

## Example

As am example, let's consider that a product record exists with this JSON representation:

```json
  { "ID":"42", "Name": "Widget", "Price":"42.42" }
```

When a client calls this endpoint:

```http
DELETE https://localhost/dbo/products/42
```

In this case, the Patchwork toolkit will check to make sure that a product with `ID = 42` exists in the database. It will also check any related tables that have `ProductId` as a foreign key value. If there are any related tables, then the API will return an `HTTP 405` error.

Assuming there are no errors, the Patchwork toolkit will create a JSON-PATCH document to record the fact that the record was deleted. That record would look like this:

```json
{ "op": "remove", "path": "/dbo/products/42" }
```

This JSON-PATCH document will be saved into the event log so we know exactly when the record was removed.
