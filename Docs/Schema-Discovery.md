# Schema Discovery

One of the most powerful feature of the Patchwork toolkit is the ability to discover the schema of a given dataset. This is done by analyzing the metadata in the database management system (DBMS) and the data itself. Patchwork will look at the structure of the database and automatically generate all the REST API endpoints for accessing the data. This is done by examining the `schemas`, `tables`, and `columns` in the database.

First, Patchwork will find all the `schemas` defined in the database and use the name of each schema to create the first segment of the URL for accessing the data.

Then, Patchwork will find all of the `tables` in the database and use name of each table to create the second segment of the URL for accessing the data.

Next, Patchwork will find all of the `columns` in each table. Patchwork requires that each table have a primary key column. Patchwork will use the primary key column to create the third segment of the URL for accessing the data. Patchwork also looks for foreign key columns and uses them to establish join relationships between tables.

Lastly, Patchwork will examine all the `columns` in each table to create a JSON Schema from each table. When Patchwork reads or writes data to or from the table, all the data objects must be valid according to the JSON Schema.

## Startup Procedure

At startup, Patchwork will connect to the database and discover the schema of the database. Patchwork will then generate all the REST API endpoints for accessing the data. Patchwork will also generate the JSON Schema for each table in the database. Here is the sequence of events that Patchwork will follow:

### Connect to the Database

When Patchwork is first registered with Dependency Injection, it must be configured with the connection string for the database. Patchwork will use this connection string to connect to the database and discover the schema of the database. 

```csharp
// show example of initializing Patchwork with the connection string
// in the ConfigureServices method of the Startup class
```

### Discover the Schema

Next, Patchwork will connect to the database and discover the schema of the database. Patchwork will find all the `schemas`, `tables`, and `columns` in the database. Patchwork will use this information to generate the REST API endpoints for accessing the data. Since this happens automatically as application startup, all of the discovered schema data will need to be cached in memory.

Patchwork assumes that all `schemas`, `tables`, and `columns` in the database that are accessible using the current connection string should be made available through the REST API.

Patchwork includes the `IDatabaseSchemaService` interface that defines the service that makes all the schema information available to the application.  Patchwork uses this interface internally, but it is available for use by the application as well.

```csharp
// show an example of building in instance of the `IDatabaseSchemaService` object from
// the Patchwork toolkit and how it can be used to access the schema information including
// the `schemas`, `tables`, and the JSON Schema for each table
```

### Generate the REST API Endpoints

Patchwork will generate all the REST API endpoints for accessing the data. Patchwork will use the schema information to create the URL for each endpoint. Patchwork will also use the schema information to create the JSON Schema for each table.

```csharp
// show example of how Patchwork generates the REST API endpoints for accessing the data
```