# GET to Query a Single Record

Patchwork uses the HTTP GET operation to query a single record from the database by that record's primary key. This endpoint is useful when the calling client needs to get the details of a record and expand it to include the record from related tables by following the primary to foreign key relationships defined in the database. Patchwork supports two `HTTP GET` endpoints, one for querying a list of records and one for querying a single record. This page describes the endpoint for querying a single record and, optionally, it's related records from other tables.

## URL Segments

The URL for accessing a record with an `HTTP GET` operation is as follows:

```
         ┏━ The name of the server hosting the REST API.
         ┃        ┏━ The name of the schema containing the table.
         ┃        ┃             ┏━ The name of the table.
         ┃        ┃             ┃            ┏━ The value of the primary key 
         ┃        ┃             ┃            ┃  column from this table.
         ▼        ▼             ▼            ▼
https://{server}/{schema name}/{table name}/{primary key value}
```

Given this URL, the Patchwork server can quickly build a query against the database to find the record by the primary key value. Here is an example:

Given this table:

```sql
  CREATE TABLE products (
    ID SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Price NUMERIC(10, 2) NOT NULL
  );
```

Given the URL:

```
  https://localhost/dbo/products/42
```

Patchwork will build this query:

```sql
  SELECT * FROM dbo.products WHERE ID = 42
```

Which would result in this response from the API:

```json
  { "ID":"42", "Name": "Widget", "Price":"42.42" }
```

## Column Selection

By default, all columns are selected when the GET operation is used. If you wish to change which columns are returned from the GET operation, you have two options.

First, you can adjust which columns are authorized to each use with the `IPatchWorkSecurity` provider. In the provider, you can override the default behavior and remove columns that the current user should not see.

Second, the calling client can specify which columns to return using the `fields` query string parameters If the caller adds the `fields` parameter then they must give it a value of a comma separated list of columns to return. If a column is not listed, it will not be returned. There is also a special case value that can be added to the list. If the caller adds the `*` (asterisk) character then Patchwork should return all default columns. This is useful when the security provider makes a column hidden by default because the caller can ask for `fields=*,privateField` and they will get back all the default fields plus the extra one they have asked for.

Given either of these URLS:

```
    URL ->  http://localhost/dbo/products/42?fields=Name,Price
RETURNS ->{ "Name": "Widget", "Price":"42.42"  }

    URL -> http://localhost/dbo/products/42?fields=ID,Price
RETURNS -> { "ID": "42", "Price"= "42.42" }

    URL -> http://localhost/dbo/products/42?fields=*
RETURNS -> { "ID": "42", "Name": "Widget", "Price"= "42.42" }

```

## Joining Related Tables

It is often important to query a record and all of its related child records as a single operation. Patchwork supports this through the `include` query string parameter. Let's consider the case that we have these table definitions for the four related tables:

1. `Products` table:

```sql
CREATE TABLE Products (
    ID SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Description TEXT
);
```

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

3. `Verified_By` table:

```sql
CREATE TABLE Verified_By (
    ID SERIAL PRIMARY KEY,
    PropertyId INTEGER REFERENCES Properties(ID),
    Username VARCHAR(255) NOT NULL,
    FOREIGN KEY (PropertyId) REFERENCES Properties(ID)
);
```

4. `Tags` table:

```sql
CREATE TABLE Tags (
    ID SERIAL PRIMARY KEY,
    ProductId INTEGER REFERENCES Products(ID),
    TagName VARCHAR(255) NOT NULL,
    FOREIGN KEY (ProductId) REFERENCES Products(ID)
);
```

Now, let's discuss how to join these tables using the `include` query string parameter in Patchwork. To join related tables, you can use the `include` query string parameter and specify the related table names separated by commas. For example, if you want to retrieve a `product` along with its `properties`, you can use the following URL:

```
https://localhost/dbo/products/42?include=Properties
```

This will return the product with its properties as a nested object in the response. Here's an example of the response:

```json
{
  "ID": "42",
  "Name": "Widget",
  "Description": "A sample product",
  "Properties": [
    {
      "ID": "1",
      "ProductId": "42",
      "Name": "Property 1",
      "Description": "Description of Property 1"
    },
    {
      "ID": "2",
      "ProductId": "42",
      "Name": "Property 2",
      "Description": "Description of Property 2"
    }
  ]
}
```

You can also join multiple tables in a hierarchy by specifying them in the `include` parameter separated by commas. Listing multiple tables separated by commas indicates to Patchwork that those tables are a dependency chain where the first is related to this table and the second is related to the first, etc. For example, to retrieve a product along with its properties and the users who verified those properties, you can use the following URL:

```
https://localhost/dbo/products/42?include=Properties,Verified_By
```

This will return the product with its properties and the verified users as nested objects in the response. Here's an example of the response:

```json
{
  "ID": "42",
  "Name": "Widget",
  "Description": "A sample product",
  "Properties": [
    {
      "ID": "1",
      "ProductId": "42",
      "Name": "Property 1",
      "Description": "Description of Property 1",
      "Verified_By": [
        {
          "ID": "1",
          "PropertyId": "1",
          "Username": "user1"
        },
        {
          "ID": "2",
          "PropertyId": "1",
          "Username": "user2"
        }
      ]
    },
    {
      "ID": "2",
      "ProductId": "42",
      "Name": "Property 2",
      "Description": "Description of Property 2",
      "Verified_By": [
        {
          "ID": "3",
          "PropertyId": "2",
          "Username": "user3"
        }
      ]
    }
  ]
}
```

Patchwork also supports joining on multiple tables that are not in a dependency chain. In this case, you should use the `include` parameter multiple times; once for each table related directly to main table.

Consider this url:

```
https://localhost/dbo/products/42?include=Properties&include=Tags
```

```json
{
  "ID": "42",
  "Name": "Widget",
  "Description": "A sample product",
  "Properties": [
    {
      "ID": "1",
      "ProductId": "42",
      "Name": "Property 1",
      "Description": "Description of Property 1"
    },
    {
      "ID": "2",
      "ProductId": "42",
      "Name": "Property 2",
      "Description": "Description of Property 2"
    }
  ],
  "Tags": [
    {
      "ID": "1",
      "ProductId": "42",
      "TagName": "Tag 1"
    },
    {
      "ID": "2",
      "ProductId": "42",
      "TagName": "Tag 2"
    }
  ]
}
```

In this response, the product with ID 42 is returned along with its properties and tags. The properties are nested within the product object, and the tags are returned as a separate array. This demonstrates how Patchwork supports joining multiple tables using the `include` query string parameter.

## Specifications

- [ ] The first segment of the GET URL **must** specify the schema name that contains the table to query.
- [ ] The second segment of the GET URL **must** be the name of the table.
- [ ] The third segment of the GET URL **must** contain a string serialized version of the primary key value for the record being queried.
- [ ] The URL **may** contain a query string parameter called `fields`.
- [ ] If the `fields` parameter present, the response **must** include only the columns listed.
- [ ] If the `fields` parameter **may** contain an asterisk (`*`) character. If present, the Patchwork **MUST** return all columns.
- [ ] The URL **may** contain the query string parameter called `include`.
- [ ] If the parameter `include` is present then Patchwork **must** query the tables named and append the JSON values from that table as an array property in the response.
- [ ] If the `include` parameter includes multiple tables separated by a comma then Patchwork **must** join from each table to the next in sequence and return the list of joined records as nested JSON under each record in turn to build a "tree" of response data.
- [ ] If the `include` parameter appears more than once, then Patchwork **must** include one array of related tables on the main JSON response object for each `include` parameter.
- [ ] if a `include` parameter is included but the named table does not have a foreign key from the main table then Patchwork **must** return an `HTTP 403: Bad Request` response.
