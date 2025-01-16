# PATCH to Update a List of Records






-----

> Here are some notes on how the HTTP Patch operation should apply to a resource listing endpoint

To update multiple resources in a single HTTP PATCH operation against a listing endpoint in a REST API, you can use the JSON Patch format as specified in RFC 6902. Here's how you can structure your JSON-PATCH document:

### JSON Patch for Multiple Resources

- **HTTP Method**: Use `PATCH` for the HTTP request.
- **Endpoint**: The endpoint should represent the collection of resources, e.g., `/resources`.

**Request Body**:

```json
[
  {
    "op": "replace",
    "path": "/123/organisation",
    "value": "A better name for my Org. Ltd"
  },
  {
    "op": "replace",
    "path": "/456/organisation",
    "value": "Acme Ltd"
  }
]
```

- **Content-Type**: Set the `Content-Type` header to `application/json-patch+json` to indicate that the request body contains a JSON Patch document.

### Explanation:

- **Operations**: Each operation in the JSON Patch document specifies what action to take (`op`), where to apply it (`path`), and what value to use (`value`).
  - `op`: The operation to perform. Common operations include `add`, `remove`, `replace`, `move`, `copy`, and `test`.
  - `path`: The JSON Pointer to the target location within the resource. For multiple resources, you would use the resource ID as part of the path.
  - `value`: The new value to set at the specified path.

- **Multiple Resources**: By including multiple operations in the array, you can update multiple resources in one request. Each operation targets a specific resource by its ID.

### Considerations:

- **Atomicity**: The PATCH method should be atomic; if one operation fails, the entire patch should fail, ensuring consistency across the resources.
- **Error Handling**: If an error occurs during the application of the patch, the server should handle it appropriately, potentially rolling back changes or reporting errors for each operation.
- **Consistency**: Ensure that the server can handle the patch in a way that maintains data consistency, especially if the resources are part of the same consistency boundary (e.g., within the same database transaction).

### Example:

If you have a `/contacts` endpoint and you want to update the `organisation` attribute for two contacts with IDs `123` and `456`, your PATCH request would look like the example provided above.

This approach allows for efficient updates of multiple resources in a single HTTP request, reducing the number of network calls and improving performance, especially in scenarios where bulk updates are common. However, it's crucial to ensure that your server can handle such requests correctly, maintaining data integrity and providing appropriate error responses if necessary.

Citations:
1. https://www.reddit.com/r/rails/comments/2j6pn7/how_you_update_multiple_records_json_api_with_one/
2. https://apihandyman.io/api-design-tips-and-tricks-getting-creating-updating-or-deleting-multiple-resources-in-one-api-call/
3. https://stackoverflow.com/questions/49754484/bulk-update-using-the-json-merge-patch-specification-rfc-7396
4. https://learn.microsoft.com/en-us/aspnet/core/web-api/jsonpatch?view=aspnetcore-9.0&viewFallbackFrom=aspnetcore-2.2
5. https://help.forcepoint.com/flexedge/sd-wan/en-us/7.1.0/smc-api-ug/GUID-BCE99050-6AAB-4AA4-B1AC-1AE9F66AC647.html
6. https://zuplo.com/blog/2024/10/10/unlocking-the-power-of-json-patch
7. https://softwareengineering.stackexchange.com/questions/363646/how-to-define-a-http-patch-to-edit-multiple-entities-in-one-request
8. https://stackoverflow.com/questions/32098423/rest-updating-multiple-resources-with-one-request-is-it-standard-or-to-be-avo
9. https://discuss.jsonapi.org/t/patch-complex-data-structures/1148
10. https://jsonpatch.com
11. https://discuss.jsonapi.org/t/updating-multiple-objects-in-single-request/183
