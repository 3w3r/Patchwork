# GET to Query a List of Records

## Table of Contents for List Feature

- Get List
  - [List Introduction](#intro)
  - [URL Segments](#url-segments)
  - [Basic Example](#basic-example)
- [Fields Querystring Parameter](./Get-List-Fields.md)
- [Limit Querystring Parameter](./Get-List-Limit.md)
- [Offset Querystring Parameter](./Get-List-Offset.md)
- [Sort Querystring Parameter](./Get-List-Sort.md)
- [Filter Querystring Parameter](./Get-List-Filter.md)

## List Introduction
<a id="intro"></a>The `HTTP GET` operation is used to query a list of records from a table in the database. This endpoint is useful when the calling client needs to retrieve a set of records that match a specific criteria because it support filtering, sorting, and pagination. Patchwork supports two `HTTP GET` endpoints, one for querying a list of records and one for querying a single record. This page describes the endpoint for querying a list of records.

## URL Segments <a id="url-segments"></a>

The URL for replacing a record with an `HTTP GET` operation is as follows:

```
             ┏━ The name of the server hosting the REST API.
             ┃        ┏━ The name of the schema containing the table.
             ┃        ┃             ┏━  The name of the table.
             ┃        ┃             ┃            ┏━ Querystring Parameters.
             ▼        ▼             ▼            ▼
GET https://{server}/{schema name}/{table name}?{querystring}
```


## Basic Example <a id="basic-example"></a>

Let's consider an example of what the Patchwork toolkit would do when this API endpoint is called. First, assume we have this table in the database.

```sql
  CREATE TABLE products (
    ID SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Price NUMERIC(10, 2) NOT NULL
  );  
```

And, let's assume that these records already exists in the database.

```json
  { "ID":"44", "Name": "Widget C", "Price":"13.99" },
  { "ID":"43", "Name": "Widget B", "Price":"2.24" },
  { "ID":"42", "Name": "Widget A", "Price":"42.42" },
  { "ID":"45", "Name": "Widget D", "Price":"35.00" }
```

In the simplest example, the client will call this endpoint with no querystring parameters at all. In this case, the
Patchwork toolkit will assume that it should return to the caller a list of all records from the table. Patchwork will
automatically apply a default ordering to order the records by the primary key and limit the result size to the default
`limit` set for the overall server. While the default `limit` can be configured at application startup, if the application omits this configuration, Patchwork will use a default `limit` of 25 records.

When a calling the GET endpoint with like this operation:

```http
GET https://localhost/dbo/products
```

Patchwork will create and execute a query similar to:

```sql
SELECT ID, Name, Price 
FROM dbo.products 
ORDER BY ID
OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY
```

The response would be an `HTTP 200` with this payload:

```json
HTTP/1.1 200 OK
Content-Type: application/json
Content-Range: items 0-3/4
X-Sort-Order: ID:asc

[
  { "ID":"42", "Name": "Widget A", "Price":"42.42" },
  { "ID":"43", "Name": "Widget B", "Price":"2.24" },
  { "ID":"44", "Name": "Widget C", "Price":"13.99" },
  { "ID":"45", "Name": "Widget D", "Price":"35.00" }
]
```
