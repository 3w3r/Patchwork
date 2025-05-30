# Options Request

In the context of the Patchwork project, the `HTTP OPTIONS` endpoint is used to retrieve the client connection permissions for a specific resource or API. This endpoint is essential for clients to understand the available HTTP methods (GET, POST, PUT, PATCH, DELETE) and any additional headers that may be required for subsequent requests.

The `HTTP OPTIONS` endpoints will always respond with an `HTTP 200: OK` response, but that response will include an HTTP header called `Allow` that tells the calling client which methods are available and the payload will give the reasons that a client may be blocked for any method they are not able to perform.

This method is available on both the single-record endpoint and the listing endpoint.

## Record Listing URL Segments

The URL for accessing a record listing with an `HTTP OPTIONS` operation is as follows:

```
         ┏━ The name of the server hosting the REST API.
         ┃        ┏━ The name of the schema containing the table.
         ┃        ┃             ┏━ The name of the table.
         ▼        ▼             ▼
https://{server}/{schema name}/{table name}
```

When making this call, the client can expect to get an `HTTP 200: OK` response with headers telling them what HTTP Methods are available on that endpoint. For example, if the client had full access to the `products` table, then the request/response would look like this:

```http
OPTIONS https://localhost/dbo/products
---
HTTP/1.1 200 OK
Allow: POST, GET, PATCH, OPTIONS
Access-Control-Max-Age: 86400

{
  "post":{ "action": "post_dbo_products" },
  "get":{ "action": "get_dbo_products" },
  "patch":{ "action": "patch_dbo_products" }
  "options":{ "action": "options_dbo_products" }
}
```

Note that this response shows the HTTP Header `Allow` indicating that the client can `POST` to create new records, `GET` to query a listing of records, `PATCH` to perform an update to multiple records, and `OPTIONS` to find these permissions. The `Access-Control-Max-Age` indicates the maximum amount of time in seconds that the client can assume these permissions have not changed.

And, here is an example of a client connection where the content is read only.

```http
OPTIONS https://localhost/dbo/products
---
HTTP/1.1 200 OK
Allow: GET, OPTIONS
Access-Control-Max-Age: 86400

{
  "post":{ "action": "post_dbo_products", "restrictions": [{"code":"readonly", "message":"POST permission not granted"}] },
  "get":{ "action": "get_dbo_products" },
  "patch":{ "action": "patch_dbo_products", "restrictions": [{"code":"readonly", "message":"PATCH permission not granted"}] }, }
  "options":{ "action": "options_dbo_products" }
}
```

Note that in this response the body payload lists all HTTP methods that _could_ be granted and indicates the reason that some are missing from the `Allow` header.

## Single Record URL Segments

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

Like the record listing endpoint, the client can use the `OPTIONS` endpoint to find the access to a specific record. Here is an example of what this would look like assuming the client has full access to the record.

```http
OPTIONS https://localhost/dbo/products/42
---
HTTP/1.1 200 OK
Allow: GET, PUT, PATCH, DELETE, OPTIONS
Access-Control-Max-Age: 86400

{
  "get":{ "action": "get_dbo_products_42", "restrictions": [] },
  "put":{ "action": "put_dbo_products_42", "restrictions": [] },
  "patch":{ "action": "patch_dbo_products_42", "restrictions": [] },
  "delete":{ "action": "delete_dbo_products_42", "restrictions": [] },
  "options":{ "action": "options_dbo_products_42", "restrictions": [] }
}
```

As above, the response here shows the `Allow` HTTP Header indicating that the current client connection can `GET` the record details, `PUT` a new version of the record, `PATCH` a selective update, `DELETE` the record, and use the `OPTIONS` endpoint to find the current permissions.

And, here is an example of a client connection where the content is read only.

```http
OPTIONS https://localhost/dbo/products
---
HTTP/1.1 200 OK
Allow: GET, OPTIONS
Access-Control-Max-Age: 86400

{
  "get":{ "action": "get_dbo_products_42", "restrictions": [] },
  "post":{ "action": "post_dbo_products_42", "restrictions": [] },
  "put":{ "action": "put_dbo_products_42", "restrictions": [{"code":"readonly", "message":"PUT permission not granted"}]  },
  "patch":{ "action": "patch_dbo_products_42", "restrictions": [{"code":"readonly", "message":"PATCH permission not granted"}]  },
  "delete":{ "action": "delete_dbo_products_42", "restrictions": [{"code":"readonly", "message":"DELETE permission not granted"}]  },
  "options":{ "action": "options_dbo_products_42", "restrictions": [] }
}
```

Note that in this response the body payload lists all HTTP methods that _could_ be granted and indicates the reason that some are missing from the `Allow` header.

## OPTIONS Response Body Payload

The response body payload is a JSON object that follows a defined schema. The response is a single object that has an optional property for each of the HTTP verbs. At a high level, it would look like this:

```json
{
  "get":{},
  "post":{},
  "put":{},
  "patch":{},
  "delete":{},
  "options":{}
}
```

The object for each of the HTTP verbs is the same schema. It has a required string property called `action` that gives a unique name to the endpoint's action. If the client can use the endpoint there will be no other properties on this object.

If the client cannot access this endpoint then there will be a second parameter. The second parameter is an array that lists the reasons the calling client cannot access the endpoint. The objects in this array have two properties. The first property called `code` identifies the security rule that prevents access and the second property called `message` is a text message that can be shown to a user to explain why the endpoint is not available.

```json
{
  "get":{ "action": "get_dbo_products_42" }, // endpoint is allowed
  "put":{ 
    "action": "put_dbo_products_42", 
    "restrictions": [
      {"code":"readonly", "message":"PUT permission not granted"} // endpoint blocked because the user is readonly
      ] 
    },
}
```

### Payload Schema

Here is a JSON Schema for the response payload.

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "get": { "$ref": "#/definitions/httpMethod" },
    "post": { "$ref": "#/definitions/httpMethod" },
    "put": { "$ref": "#/definitions/httpMethod" },
    "patch": { "$ref": "#/definitions/httpMethod" },
    "delete": { "$ref": "#/definitions/httpMethod" },
    "options": { "$ref": "#/definitions/httpMethod" }
  },
  "required": ["get", "post", "put", "patch", "delete", "options"],
  "definitions": {
    "httpMethod": {
      "type": "object",
      "properties": {
        "action": { "type": "string" },
        "restrictions": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/restriction"
          }
        }
      },
      "required": ["action", "restrictions"]
    },
    "restriction": {
      "type": "object",
      "properties": {
        "code": { "type": "string" },
        "message": { "type": "string" }
      },
      "required": ["code", "message"]
    }
  }
}
```
