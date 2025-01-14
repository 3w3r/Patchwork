# MySQL Sample Database Schema

The MySQL sample database schema consists of the following tables:

- `customers`: stores customer’s data.
- `products`: stores a list of scale model cars.
- `productlines`: stores a list of product lines.
- `orders`: stores sales orders placed by customers.
- `orderdetails`: stores sales order line items for every sales order.
- `payments`: stores payments made by customers based on their accounts.
- `employees`: stores employee information and the organization structure such as who reports to whom.
- `offices`: stores sales office data.
  
## ER Diagram

The following picture illustrates the ER diagram of the sample database. This ER diagram is in mermaid format so we can change it if we need to make any changes to the sample database schema we are using for testing.

```mermaid
erDiagram
    PRODUCTLINES {
        varchar(50) productLine PK
        varchar(4000) textDescription
        mediumtext htmlDescription
        mediumblob image
    }

    PRODUCTS {
        varchar(15) productCode PK
        varchar(70) productName
        varchar(50) productLine FK
        varchar(10) productScale
        varchar(50) productVendor
        text productDescription
        smallint(6) quantityInStock
        decimal(10-2) buyPrice
        decimal(10-2) MSRP
    }

    OFFICES {
        varchar(10) officeCode PK
        varchar(50) city
        varchar(50) phone
        varchar(50) addressLine1
        varchar(50) addressLine2
        varchar(50) state
        varchar(50) country
        varchar(15) postalCode
        varchar(10) territory
    }

    EMPLOYEES {
        int employeeNumber PK
        varchar(50) lastName
        varchar(50) firstName
        varchar(10) extension
        varchar(100) email
        varchar(10) officeCode FK
        int reportsTo FK
        varchar(50) jobTitle
    }

    CUSTOMERS {
        int customerNumber PK
        varchar(50) customerName
        varchar(50) contactLastName
        varchar(50) contactFirstName
        varchar(50) phone
        varchar(50) addressLine1
        varchar(50) addressLine2
        varchar(50) city
        varchar(50) state
        varchar(15) postalCode
        varchar(50) country
        int salesRepEmployeeNumber FK
        decimal(10-2) creditLimit
    }

    PAYMENTS {
        int customerNumber PK, FK
        varchar(50) checkNumber PK
        date paymentDate
        decimal(10-2) amount
    }

    ORDERS {
        int orderNumber PK
        date orderDate
        date requiredDate
        date shippedDate
        varchar(15) status
        text comments
        int customerNumber FK
    }

    ORDERDETAILS {
        int orderNumber PK, FK
        varchar(15) productCode PK, FK
        int quantityOrdered
        decimal(10-2) priceEach
        smallint(6) orderLineNumber
    }

    PRODUCTS ||--o{ ORDERDETAILS: "contains"
    PRODUCTS }o--|| PRODUCTLINES: "belongs to"
    OFFICES ||--o{ EMPLOYEES: "has"
    EMPLOYEES }o--|| EMPLOYEES: "reports to"
    CUSTOMERS ||--o{ PAYMENTS: "makes"
    CUSTOMERS ||--o{ ORDERS: "places"
    ORDERS ||--o{ ORDERDETAILS: "includes"
    EMPLOYEES }o--|| CUSTOMERS: "sales rep for"

```

In addition to the ER Diagram above, the following picture illustrates the ER diagram of the same sample database as it was when we copied it from the [MySql Tutorial documentation](https://www.mysqltutorial.org/getting-started-with-mysql/mysql-sample-database/). This first image was taken from the MySQL documentation.

![Classic Models Sample Database](./sample-database-ERD.png)
