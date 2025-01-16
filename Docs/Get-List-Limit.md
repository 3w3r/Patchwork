# Limiting the Number of Records Returned

When querying data, the calling client can request that the records be limited to a specific number of records. To do this, just use the querystring parameter for `limit` and provide the number of records to return. This will override the default paging size set for the overall server and return a specific number of records to the caller. The `limit` parameter can be used in conjunction with the `offset` parameter to limit the number of records returned and to page through the results. While the default `limit` can be configured at application startup, if the application omits this configuration, Patchwork will use a default `limit` of 25 records.

When the response does not include all records from the table, the client should examine the `Content-Range` header to determine how many records were returned and how many records are in the table. This will help the client to determine if there are more records to query.

```http
GET https://localhost/dbo/products?limit=2
```

This would cause Patchwork to create and execute a query similar to:

```sql
SELECT ID, Name, Price
FROM dbo.products
ORDER BY ID
OFFSET 0 ROWS FETCH NEXT 2 ROWS ONLY
```

The results from this query would be:

```json
HTTP/1.1 200 OK
Content-Type: application/json
Content-Range: items 0-1/4
X-Sort-Order: ID:asc

[
  { "ID":"42", "Name": "Widget A", "Price":"42.42" },
  { "ID":"43", "Name": "Widget B", "Price":"2.24" }
]
```
