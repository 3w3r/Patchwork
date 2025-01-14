# Paging Through Results

When querying data, the calling client can request that the records be paged through by providing a page number and a page size. To do this, just use the querystring parameter for `page` and provide the page number to return. Patchwork will assume that the number of records in one page is the default maximum page size set for the overall server unless the client specifies a different size. If the `page` parameter and the `size` parameter are both provided, then the `size` parameter will override the default page size.

When the response does not include all records from the table, the client should examine the `Content-Range` header to determine how many records were returned and how many records are in the table. This will help the client to determine if there are more records to query.

```http
GET https://localhost/dbo/products?page=2,size=2
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
X-Sort-Order: ID,asc

[
  { "ID":"44", "Name": "Widget C", "Price":"13.99" },
  { "ID":"45", "Name": "Widget D", "Price":"35.00" }
]
```

