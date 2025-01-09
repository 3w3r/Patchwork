# What is JSON Patch?

JSON Patch is a format for describing changes to a [JSON](https://www.json.org/) document. It can be used to avoid sending a whole document when only a part has changed. When used in combination with the [HTTP PATCH method](https://datatracker.ietf.org/doc/html/rfc5789/), it allows partial updates for HTTP APIs in a standards compliant way.

The patch documents are themselves JSON documents.

JSON Patch is specified in [RFC 6902](https://datatracker.ietf.org/doc/html/rfc6902/) from the IETF.

## Simple example

### The original document

    {
      "baz": "qux",
      "foo": "bar"
    }

### The patch

    [
      { "op": "replace", "path": "/baz", "value": "boo" },
      { "op": "add", "path": "/hello", "value": ["world"] },
      { "op": "remove", "path": "/foo" }
    ]

### The result

    {
      "baz": "boo",
      "hello": ["world"]
    }

# How it works

A JSON Patch document is just a JSON file containing an array of patch operations. The patch operations supported by JSON Patch are "add", "remove", "replace", "move", "copy" and "test". The operations are applied in order: if any of them fail then the whole patch operation should abort.

## JSON Pointer

JSON Pointer ([IETF RFC 6901](https://datatracker.ietf.org/doc/html/rfc6901/)) defines a string format for identifying a specific value within a JSON document. It is used by all operations in JSON Patch to specify the part of the document to operate on.

A JSON Pointer is a string of tokens separated by `/` characters, these tokens either specify keys in objects or indexes into arrays. For example, given the JSON

    {
      "biscuits": [
        { "name": "Digestive" },
        { "name": "Choco Leibniz" }
      ]
    }

`/biscuits` would point to the array of biscuits and `/biscuits/1/name` would point to `"Choco Leibniz"`.

To point to the root of the document use an empty string for the pointer. The pointer `/` doesn't point to the root, it points to a key of `""` on the root (which is totally valid in JSON).

If you need to refer to a key with `~` or `/` in its name, you must escape the characters with `~0` and `~1` respectively. For example, to get `"baz"` from `{ "foo/bar~": "baz" }` you'd use the pointer `/foo~1bar~0`.

Finally, if you need to refer to the end of an array you can use `-` instead of an index. For example, to refer to the end of the array of biscuits above you would use `/biscuits/-`. This is useful when you need to insert a value at the end of an array.

## Operations

### Add

    { "op": "add", "path": "/biscuits/1", "value": { "name": "Ginger Nut" } }

Adds a value to an object or inserts it into an array. In the case of an array, the value is inserted before the given index. The `-` character can be used instead of an index to insert at the end of an array.

### Remove

    { "op": "remove", "path": "/biscuits" }

Removes a value from an object or array.

{ "op": "remove", "path": "/biscuits/0" }

Removes the first element of the array at `biscuits` (or just removes the "0" key if `biscuits` is an object)

### Replace

    { "op": "replace", "path": "/biscuits/0/name", "value": "Chocolate Digestive" }

Replaces a value. Equivalent to a "remove" followed by an "add".

### Copy

    { "op": "copy", "from": "/biscuits/0", "path": "/best_biscuit" }

Copies a value from one location to another within the JSON document. Both `from` and `path` are JSON Pointers.

### Move

    { "op": "move", "from": "/biscuits", "path": "/cookies" }

Moves a value from one location to the other. Both `from` and `path` are JSON Pointers.

### Test

    { "op": "test", "path": "/best_biscuit/name", "value": "Choco Leibniz" }

Tests that the specified value is set in the document. If the test fails, then the patch as a whole should not apply.

# Links

- [Refernce Page - jsonpatch.com/](https://jsonpatch.com/) (origin of this page)
- [C# Library - JsonPatch.Net](http://github.com/gregsdennis/json-everything) (JSON Patch support for System.Text.Json)
