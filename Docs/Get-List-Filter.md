# Filtering the Results

It is rare that a calling client will need to query all records from a table. In most cases, the client will want to filter the records to only return a subset of the records. Patchwork supports this through the `filter` query string parameter. The client can provide a filter string that will be used to filter the records returned.

## Complex Filter

The querystring parameter called `filter` is used to specify a subset of records to return. The value of the `filter` parameter is a string that contains the filter criteria. The filter criteria is a string that contains one or more filter expressions. Each filter expression is a comparison of a column value to a value provided by the client. The filter expressions can be combined using the logical operators `AND` and `OR`. The filter expressions can also be grouped using parentheses to control the order of evaluation.

| Example                                                      | Description                                                                                                                          |
| ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------ |
| `filter=price gt 10 AND price lt 60`                         | Would filter where price is between 10 and 60 (non-inclusive)                                                                        |
| `filter=price gt 60 OR price lt 10`                          | Would filter where price is greater than 60 or less than 10 (non-inclusive)                                                          |
| `filter=(price gt 60 OR price lt 10) AND status eq 'Active'` | Would filter where price is greater than 60 or less than 10 (non-inclusive) and also only include results where the status is active |

### Operators

| Operator | Description                                                                | Example Usage                   |
| -------- | -------------------------------------------------------------------------- | ------------------------------- |
| eq       | Equal                                                                      | `givenName eq 'Mark'`           |
| ne       | Not equal                                                                  | `givenName ne 'Mark'`           |
| gt       | Greater than                                                               | `price gt 25`                   |
| ge       | Greater than or equal                                                      | `price ge 25`                   |
| lt       | Less than                                                                  | `price lt 25`                   |
| le       | Less than or equal                                                         | `price le 50`                   |
| in       | Equals any of the values provided                                          | `givenName in ('Mark', 'Seth')` |
| ct       | Contains the values provided (use carefully and watch performance)         | `title ct 'Engineer'`           |
| sw       | Starts with the value provided (use carefully and watch performance)       | `salutation sw 'Honour'`        |
| cv       | Array attribute contains an element whose value equals the value provided. | `aliases cv 'OJ'`               |

> [!NOTE]
> The operator type `cv` will not be implemented in the MVP version of Patchwork and it reserved for a future release.

### Example Complex Filter

Let's consider and example against the `products` table. Here is a definition of the table:

```sql
  CREATE TABLE products (
    ID SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Price NUMERIC(10, 2) NOT NULL
  );
```

And, let's assume that these records already exists in the database.

```json
({ "ID": "44", "Name": "Widget C", "Price": "13.99" },
{ "ID": "43", "Name": "Widget B", "Price": "2.24" },
{ "ID": "42", "Name": "Widget A", "Price": "42.42" },
{ "ID": "45", "Name": "Widget D", "Price": "35.00" })
```

When we send this request to the server:

```http
GET https://localhost/dbo/products?filter=Price gt 10 AND Price lt 40
```

This would cause Patchwork to create and execute the following SQL query:

```sql
SELECT *
FROM dbo.products
WHERE Price > 10 AND Price < 40
OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY
```

We would expect the response to be:

```json
HTTP/1.1 200 OK
Content-Type: application/json
Content-Range: items 0-1/2
X-Sort-Order: ID:asc

[
  { "ID":"44", "Name": "Widget C", "Price":"13.99" },
  { "ID":"45", "Name": "Widget D", "Price":"35.00" }
]
```

## Example Complex Filter with Parentheses

Here is another example where the filter includes parentheses to group the expressions. In this case, the filter by price includes an `OR` operator to filter by price greater than 40 or less than 10 and also filters by the name of the product.

When we send this request to the server:

```http
GET https://localhost/dbo/products?filter=(Price gt 40 OR Price lt 10) AND Name eq 'Widget C'
```

This would cause Patchwork to create and execute the following SQL query:

```sql
SELECT *
FROM dbo.products
WHERE (Price > 40 OR Price < 10) AND Name = 'Widget B'
OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY
```

We would expect the response to be:

```json
HTTP/1.1 200 OK
Content-Type: application/json
Content-Range: items 0-0/1
X-Sort-Order: ID:asc

[
  { "ID":"43", "Name": "Widget B", "Price":"2.24" }
]
```

## Simple Filter

Simple filters are used to filter records based on a foreign key relationship. The filter is a string that contains the name of the column to filter by and the value to filter on. This is very useful when the client application needs to find the record related to a specific record in another table. In contrast to the complex filter, the simple filter only supports the `=` operator and it only works on columns that are foreign keys. If multiple simple filters are provided such as in the case of a many-to-many relation table, they are combined using the `AND` operator.

### Example Simple Filter

For this example, let's assume we have a second table in our sample database defined like this:

2. `Properties` table:

```sql
CREATE TABLE Properties (
    ID SERIAL PRIMARY KEY,
    ProductId INTEGER REFERENCES Products(ID),
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    FOREIGN KEY (ProductId) REFERENCES Products(ID)
);
```

And the `Properties` table has these records:

```json
({ "ID": "1", "ProductId": "42", "Name": "Property A", "Description": "This is property A" },
{ "ID": "2", "ProductId": "42", "Name": "Property B", "Description": "This is property B" },
{ "ID": "3", "ProductId": "43", "Name": "Property C", "Description": "This is property C" },
{ "ID": "4", "ProductId": "44", "Name": "Property D", "Description": "This is property D" },
{ "ID": "5", "ProductId": "44", "Name": "Property E", "Description": "This is property E" },
{ "ID": "6", "ProductId": "45", "Name": "Property F", "Description": "This is property F" })
```

When we send this request to the server:

```http
GET https://localhost/dbo/Properties?ProductId=42
```

This would cause Patchework to create and execute the following SQL query:

```sql
SELECT *
FROM Properties
WHERE ProductId = 42
OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY
```

This is requesting all the records from the `Properties` table where the `ProductId` is `42`. The response would be:

```json
HTTP/1.1 200 OK
Content-Type: application/json
Content-Range: items 0-1/2
X-Sort-Order: ID:asc

[
  { "ID":"1", "ProductId":"42", "Name": "Property A", "Description":"This is property A" },
  { "ID":"2", "ProductId":"42", "Name": "Property B", "Description":"This is property B" }
]
```
