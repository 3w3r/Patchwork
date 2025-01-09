# POST Record

The Patchwork `HTTP POST` endpoint is used to insert a new record into a table. To create a new record, the calling client should generate a JSON document with all the required fields and invoke the `POST` endpoint for the table (without a primary key value). `POST` is only supported on the table URL and will not work on a schema or record URL. Any fields omitted from the JSON document will get the default value assigned by the database. Patchwork will then return back to the caller a JSON document representing the created record. This is great because it will show the values of any default fields that were assigned by the database such as identity columns often used as primary keys.

## URL Segments

The URL for accessing a record with an `HTTP POST` operation is as follows. Note that this is the endpoint for the table name even though we are inserting a single record. The calling client cannot target a single record because that record does not yet exist; it won't exist until _after_ the `POST` is complete.

```
              ┏━ The name of the server hosting the REST API.
              ┃        ┏━ The name of the schema containing the table.
              ┃        ┃             ┏━ The name of the table.
              ▼        ▼             ▼
POST https://{server}/{schema name}/{table name}
Content-Type: application/json

{ 
  // complete object in JSON format
}
```

Let's consider an example of inserting a product into the `dbo.products` table. Here is the table definition:

```sql
  CREATE TABLE products (
    ID SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Price NUMERIC(10, 2) NOT NULL
  );
```

Given this table, the calling client can make an `HTTP POST` request like this. Note that both the name and price columns **must** be supplied as they do not permit `NULL` values and they do not have a default value. Also note that the `ID` column **must not** be provided as it will be automatically generated.

```http
POST https://localhost/dbo/products
Content-Type: application/json

{ "Name": "Widget", "Price":"42.42" }
```

Patchwork will build this query:

```sql
  INSERT INTO dbo.products (Name, Price)
  VALUES ('Widget Name Here', 42.42)
  RETURNING *;
```

This would cause Patchwork to insert an entry in the `Patchwork_Event_Log` that looks like this:

```json
{
  "op": "add", 
  "path": "/dbo/products/42", 
  "value": { "ID":"42", "Name": "Widget", "Price":"42.42" } 
}
```

Patchwork will then create a response using the values from the newly inserted record that looks like this:

```json
  { "ID":"42", "Name": "Widget", "Price":"42.42" }
```

