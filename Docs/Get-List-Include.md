# Including Only Specific Columns

Often, when a client needs to get a listing of records from a table they only need a few columns from the table. Patchwork allows the client to specify which columns to return by using the `include` querystring parameter. The client can provide a comma separated list of column names to include in the response. If the client does not provide the `include` parameter, then all columns will be returned.

```http
GET https://localhost/dbo/products?include=Name,ID
```

This would cause Patchwork to create and execute a query similar to:

```sql
SELECT ID, Name 
FROM dbo.products 
ORDER BY ID
OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY
```

By including only `ID` and `Name`, the results from this query would be:

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
