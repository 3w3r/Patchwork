# Excluding Specific Columns

In much the same way the `include` parameter is used to specify which columns to return, the `exclude` parameter can be used to specify which columns to exclude from the response. The client can provide a comma separated list of column names to exclude from the response. If the client does not provide the `exclude` parameter, then all columns will be returned.

```http
GET https://localhost/dbo/products?exclude=Price
```

This would cause Patchwork to create and execute a query similar to:

```sql
SELECT ID, Name 
FROM dbo.products
ORDER BY ID
OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY
```

By including all the table's columns except `Price`, the results from this query would be:

```json
HTTP/1.1 200 OK
Content-Type: application/json
Content-Range: items 0-3/4
X-Sort-Order: ID:asc

[
  { "ID":"42", "Name": "Widget A" },
  { "ID":"45", "Name": "Widget D" },
  { "ID":"44", "Name": "Widget C" },
  { "ID":"43", "Name": "Widget B" }
]
```
