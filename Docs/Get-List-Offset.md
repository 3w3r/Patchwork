# Paging Through Results

When querying data, the calling client can request that the records be paged through by providing a number of records to offset by. To do this, just use the querystring parameter for `offset` and provide the number of records to skip. If the `offset` parameter is not included in the querystring, the Patchwork will assume no offset. The `offset` parameter is often combined with the `limit` parameter to create a data paging system.

When the response does not include all records from the table, the client should examine the `Content-Range` header to determine how many records were returned and how many records are in the table. This will help the client to determine if there are more records to query.

```http
GET https://localhost/dbo/products?offset=2,limit=2
```

This would cause Patchwork to create and execute a query similar to:

```sql
SELECT ID, Name, Price
FROM dbo.products
ORDER BY ID
OFFSET 2 ROWS FETCH NEXT 2 ROWS ONLY
```

The results from this query would be:

```json
HTTP/1.1 200 OK
Content-Type: application/json
Content-Range: items 2-3/4
X-Sort-Order: ID:asc

[
  { "ID":"44", "Name": "Widget C", "Price":"13.99" },
  { "ID":"45", "Name": "Widget D", "Price":"35.00" }
]
```
