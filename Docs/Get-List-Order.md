# Ordering the Results

When querying data, the calling client can request that the records be sorted in order by any column they want. To do this, just use the querystring parameter for `sort` and provide the column name and the direction to sort. The direction is immediately after the column name and separated by a colon `:` character. The direction can be either `asc` for ascending or `desc` for descending. If the client needs to sort by more than one column, additional columns can be listed with each column separated by a comma `,` character. The default sorting is ascending if the sort order is omitted from the column name. Again, the default paging will be applied because the client did not specify a `limit` or `offset` parameter.

Here is an example of how to sort the records by the `Price` column in descending order then by the `Name` column in ascending order:

```http
GET https://localhost/dbo/products?sort=Price:desc,Name
```

This would cause Patchwork to create and execute a query similar to:

```sql
SELECT ID, Name, Price 
FROM dbo.products 
ORDER BY Price DESC
OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY
```

The results from this query would be:

```json
HTTP/1.1 200 OK
Content-Type: application/json
Content-Range: items 0-3/4
X-Sort-Order: Price:desc,Name:asc

[
  { "ID":"42", "Name": "Widget A", "Price":"42.42" },
  { "ID":"45", "Name": "Widget D", "Price":"35.00" },
  { "ID":"44", "Name": "Widget C", "Price":"13.99" },
  { "ID":"43", "Name": "Widget B", "Price":"2.24" }
]
```
