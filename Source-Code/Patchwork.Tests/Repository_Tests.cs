using Patchwork.Authorization;
using Patchwork.Repository;
using Patchwork.SqlDialects.Sqlite;
using Patchwork.SqlDialects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using System.Globalization;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.ComponentModel.DataAnnotations;
using Npgsql.PostgresTypes;
using Json.Patch;
using Xunit.Sdk;

namespace Patchwork.Tests;

public class Repository_Tests
{
    private SqliteDialectBuilder sql;
    private DefaultPatchworkAuthorization auth;
    private PatchworkRepository repo;

    public Repository_Tests()
    {
        sql = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());
        auth = new DefaultPatchworkAuthorization();
        repo = new PatchworkRepository(auth, sql);
    }
    [Fact]
    void GetSingleRecordReturnsSingleRecord()
    {
        var output = repo.GetResource("dbo", "employees", "1625");
        Assert.Equal(1625, output.Resource.employeeNumber);
        Assert.Equal("ykato@classicmodelcars.com", output.Resource.email);
        Assert.Equal("Yoshimi", output.Resource.firstName);
        Assert.Equal("Kato", output.Resource.lastName);
    }

    [Fact]
    void GetIncludeReturnsIncludedRecord()
    {
        Assert.Fail();
    }

    [Fact]
    void GetFieldsReturnsOnlySpecifiedFields()
    {
        var output = repo.GetResource("dbo", "employees", "1625", "employeeNumber,email");
        Assert.Equal(1625, output.Resource.employeeNumber);
        Assert.Equal("ykato@classicmodelcars.com", output.Resource.email);
        Assert.Null(output.Resource.firstName);
        Assert.Null(output.Resource.lastName);
    }

    [Fact]
    void GetIncludeAndFieldsReturnsSpecifiedIncludedFields()
    {
        Assert.Fail();
    }

    [Fact]
    void GetListReturnsListOfRecords()
    {
        var output = repo.GetList("dbo", "employees");
        Assert.Equal(23, output.Count);
        output.Resources.ForEach(
            item =>
            {
                Assert.NotNull(item.email);
                Assert.NotNull(item.firstName);
                Assert.NotNull(item.lastName);
            }
            );
    }

    [Fact]
    void GetListFieldsReturnsOnlySpecifiedFields()
    {
        var output = repo.GetList("dbo", "employees", "employeeNumber,email");
        Assert.Equal(23, output.Count);
        output.Resources.ForEach(
            item =>
            {
                Assert.NotNull(item.email);
                Assert.Null(item.firstName);
                Assert.Null(item.lastName);
            }
            );

    }

    [Fact]
    void GetListFilterReturnsOnlyMatchingRecords()
    {
        var unfiltered = repo.GetList("dbo", "employees");
        var filteredEqual = repo.GetList("dbo", "employees", "", "reportsTo eq '1102'");
        var filteredGreater = repo.GetList("dbo", "employees", "", "employeeNumber gt '1500'");
        var filteredBoth = repo.GetList("dbo", "employees", "", "reportsTo eq '1102' AND employeeNumber gt '1500'");
        
        Assert.Equal(6, filteredEqual.Count);
        Assert.Equal(8, filteredGreater.Count);
        Assert.Equal(3, filteredBoth.Count);

        Assert.Equal(unfiltered.Resources.Where(item => (item.reportsTo == 1102)), filteredEqual.Resources);
        Assert.Equal(unfiltered.Resources.Where(item => (item.employeeNumber > 1500)), filteredGreater.Resources);
        Assert.Equal(unfiltered.Resources.Where(item => (item.reportsTo == 1102 && item.employeeNumber > 1500)), filteredBoth.Resources);
    }

    [Fact]
    void GetListSortReturnsSortedRecords()
    {
        var sorted = repo.GetList("dbo", "employees", "", "", "lastName:asc");
        var unsorted = repo.GetList("dbo", "employees");

        Assert.NotEqual(unsorted.Resources, sorted.Resources);
        for (int i = 1; i < sorted.Resources.Count; i++)
        {
            Assert.True(String.Compare(sorted.Resources[i].lastName, sorted.Resources[i-1].lastName) != -1);
        }
    }

    [Fact]
    void GetListPagingReturnsPagedRecords()
    {
        var pageOne = repo.GetList("dbo", "employees", "", "", "", 4, 0);
        var pageTwo = repo.GetList("dbo", "employees", "", "", "", 4, 3);

        Assert.Equal(4, pageOne.Count);
        Assert.Equal(4, pageTwo.Count);

        Assert.Equal(pageOne.Resources[3], pageTwo.Resources[0]);
        Assert.NotEqual(pageOne.Resources[1], pageTwo.Resources[1]);
    }

    [Fact]
    void GetListAllOptionsWorkTogether()
    {
        var output = repo.GetList("dbo", "employees", "employeeNumber,email,lastName", "reportsTo ne 1102", "lastName:asc", 10, 3);
        var outputPageTwo = repo.GetList("dbo", "employees", "employeeNumber,email,lastName", "reportsTo ne 1102", "lastName:asc", 10, 13);

        Assert.Equal(10, output.Count);

        Assert.Null(output.Resources.FirstOrDefault()?.firstName);
        Assert.NotNull(output.Resources.FirstOrDefault()?.lastName);

        Assert.Empty(output.Resources.Where(x => { return x.reportsTo == 1102; }));
        Assert.Equal(output.Resources, output.Resources.Where(x => { return x.reportsTo != 1102; }));

        Assert.Equal(3, outputPageTwo.Count);
        for (int i = 1; i < output.Resources.Count; i++)
        {
            Assert.True(String.Compare(output.Resources[i].lastName, output.Resources[i - 1].lastName) != -1);
        }
        Assert.True(String.Compare(outputPageTwo.Resources.FirstOrDefault()?.lastName, output.Resources.LastOrDefault()?.lastName) != -1);
    }

    [Fact]
    void PatchWorksWithValidPatchAndFailsWithInvalidPatch()
    {
        var before = repo.GetResource("dbo", "customers", "452");
        JsonPatch updatePatch = JsonSerializer.Deserialize<JsonPatch>(
            $"[{{\"op\": \"replace\",\"path\": \"/phone\",\"value\": \"7675-3262\"}}," +
            $"{{\"op\": \"remove\",\"path\": \"/salesRepEmployeeNumber\"}}," +
            $"{{\"op\": \"add\",\"path\": \"/state\",\"value\": \"placename\"}}]"
            ) ?? new JsonPatch();

        var result = repo.PatchResource("dbo", "customers", "452", updatePatch);
        var after = repo.GetResource("dbo", "customers", "452");

        Assert.NotEqual(before.Resource, after.Resource);

        Assert.Equal("7675-3262", after.Resource.phone);
        Assert.Equal("placename", after.Resource.state);
        Assert.Null(after.Resource.salesRepEmployeeNumber);


        before = repo.GetResource("dbo", "customers", "450");

        var PatchNonexistentField = JsonSerializer.Deserialize<JsonPatch>(
            $"[{{\"op\": \"replace\",\"path\": \"/bone\",\"value\": \"7675-3262\"}}]"
            ) ?? new JsonPatch();

        result = repo.PatchResource("dbo", "customers", "450", PatchNonexistentField);
        Assert.Empty(result.Changes.Operations);
        after = repo.GetResource("dbo", "customers", "450");
        Assert.Equal(before.Resource, after.Resource);

        var PatchWrongDataType = JsonSerializer.Deserialize<JsonPatch>(
            $"[{{\"op\": \"replace\",\"path\": \"/state\",\"value\": 11}}]"
            ) ?? new JsonPatch();

        //What is intended behavior here? crash / cast number to string / ignore attempted change
        //Currently assuming ignore attempted change
        result = repo.PatchResource("dbo", "customers", "450", PatchWrongDataType);
        Assert.Empty(result.Changes.Operations);
        after = repo.GetResource("dbo", "customers", "450");
        Assert.Equal(before.Resource, after.Resource);
    }

    [Fact]
    void PatchListWorksWithValidPatchAndFailsWithInvalidPatch()
    {
        JsonPatch newPatch = JsonSerializer.Deserialize<JsonPatch>(
            "[{ \"op\": \"add\", \"path\": \"/-\", \"value\" : {" +
            "\"officeCode\" : \"testentry\"," +
            "\"phone\" : \"1111111111\"," +
            "\"addressLine1\" : \"123 Office That Is Not Real\"," +
            "\"city\" : \"placeton\"," +
            "\"state\" : \"Florida 2\"," +
            "\"postalCode\" : \"23562\"," +
            "\"country\" : \"USB\"," +
            "\"territory\" : \"Ontario\"}}]"
            ) ?? new JsonPatch();

        var result = repo.PatchList("dbo", "offices", newPatch);
        var record = repo.GetResource("dbo", "offices", "testentry");
        Assert.NotNull(record);
        Assert.Equal(result.Created.FirstOrDefault()?.Resource, record.Resource);

        JsonPatch multiUpdatePatch = JsonSerializer.Deserialize<JsonPatch>(
            "[{ \"op\": \"replace\", \"path\": \"/1/phone\", \"value\" :\"+200\"}," +
            "{\"op\": \"replace\", \"path\": \"/2/phone\", \"value\" :\"+300\"}," +
            "{\"op\": \"replace\", \"path\": \"/2/phone\", \"value\" :\"+400\"}," +
            $"{{\"op\": \"remove\", \"path\": \"/{record.Resource.officeCode}\"}}]"
            ) ?? new JsonPatch();

        result = repo.PatchList("dbo", "offices", multiUpdatePatch);
        var records = repo.GetList("dbo", "offices");
        Assert.Contains(records.Resources, x => x.phone == "+200" && x.officeCode == "1");
        Assert.Contains(records.Resources, x => x.phone == "+400" && x.officeCode == "2");
        Assert.DoesNotContain(records.Resources, x => x.officeCode == "testentry");


        JsonPatch missingRequiredFieldPatch = JsonSerializer.Deserialize<JsonPatch>(
            "[{ \"op\": \"add\", \"path\": \"/-\", \"value\" : {" +
            "\"officeCode\" : \"testentry\"," +
            "\"phone\" : \"1111111111\"," +
            "\"city\" : \"placeton\"," +
            "\"state\" : \"Florida 2\"," +
            "\"postalCode\" : \"23562\"," +
            "\"country\" : \"USB\"," +
            "\"territory\" : \"Ontario\"}}]"
            ) ?? new JsonPatch();
        Assert.ThrowsAny<Exception>(() => repo.PatchList("dbo", "offices", missingRequiredFieldPatch));

        JsonPatch updateFakeRecordPatch = JsonSerializer.Deserialize<JsonPatch>(
            "[{ \"op\": \"replace\", \"path\": \"/burger/burger\", \"value\" : \"burger\"}]"
            ) ?? new JsonPatch();
        Assert.ThrowsAny<Exception>(() =>repo.PatchList("dbo", "offices", updateFakeRecordPatch));
    }

    [Fact]
    void DeleteRecordDeletesRecord()
    {
        var before = repo.GetResource("dbo", "customers", "125");
        var result = repo.DeleteResource("dbo", "customers", "125");
        var after = repo.GetResource("dbo", "customers", "125");

        Assert.True(result.Success);
        Assert.Equal(125, before.Resource.customerNumber);
        Assert.Empty(after.Resource);
    }

    [Fact]
    void DeleteRecordFailsOnNoRecord()
    {
        var result = repo.DeleteResource("dbo", "customers", "102");
        Assert.False(result.Success);
    }

    [Fact]
    void DeleteRecordFailsOnReferencedRecord()
    {
        var before = repo.GetResource("dbo", "customers", "124");
        var result = repo.DeleteResource("dbo", "customers", "124");
        var after = repo.GetResource("dbo", "customers", "124");

        Assert.False(result.Success);
        Assert.Equal(before.Resource, after.Resource);
    }

    [Fact]
    void PostRecordPostsRecord()
    {
        JsonDocument newRecord = JsonDocument.Parse(
            "{\"customerName\" : \"JoeSchmoe\","+
            "\"contactLastName\" : \"Schmoe\","+
            "\"contactFirstName\" : \"Joe\"," +
            "\"phone\" : \"9999999999\"," +
            "\"addressLine1\" : \"111 Shump Rd\"," +
            "\"addressLine2\" : \"third door on the right\"," +
            "\"city\" : \"Placeville\"," +
            "\"state\" : \"Wiscansin\"," +
            "\"postalCode\" : \"11111\"," +
            "\"country\" : \"USB\"," +
            "\"creditLimit\" : \"20000000\"}"
        );

        var result = repo.PostResource("dbo", "customers", newRecord);
        var after = repo.GetResource("dbo", "customers", result.Id);
        Assert.Equal("JoeSchmoe", after.Resource.customerName);
        Assert.Equal("Placeville", after.Resource.city);
        Assert.Equal("USB", after.Resource.country);
    }

    [Fact]
    void PostRecordFailsOnInvalidRecord()
    {
        //Missing required field, customerName
        JsonDocument newRecord = JsonDocument.Parse(
            "{\"contactLastName\" : \"Schmoe\"," +
            "\"contactFirstName\" : \"Joe\"," +
            "\"phone\" : \"9999999999\"," +
            "\"addressLine1\" : \"111 Shump Rd\"," +
            "\"addressLine2\" : \"third door on the right\"," +
            "\"city\" : \"Placeville\"," +
            "\"state\" : \"Wiscansin\"," +
            "\"postalCode\" : \"11111\"," +
            "\"country\" : \"USB\"," +
            "\"creditLimit\" : \"20000000\"}"
        );
        Assert.ThrowsAny<Exception>(() => repo.PostResource("dbo", "customers", newRecord));

    }

    [Fact]
    void PutRecordPutsRecord()
    {
        //Updates the phone number
        var before = repo.GetResource("dbo", "customers", "475");
        JsonDocument recordUpdate = JsonDocument.Parse(
            $"{{\"customerName\" : \"{before.Resource.customerName}\"," +
            $"\"contactLastName\" : \"{before.Resource.contactLastName}\"," +
            $"\"contactFirstName\" : \"{before.Resource.contactFirstName}\"," +
            $"\"phone\" : \"3109459305\"," +
            $"\"addressLine1\" : \"{before.Resource.addressLine1}\"," +
            $"\"city\" : \"{before.Resource.city}\"," +
            $"\"state\" : \"{before.Resource.state}\"," +
            $"\"postalCode\" : \"{before.Resource.postalCode}\"," +
            $"\"country\" : \"{before.Resource.country}\"," +
            $"\"creditLimit\" : \"{before.Resource.creditLimit}\"}}"
        );
        var result = repo.PutResource("dbo", "customers", "475", recordUpdate);
        var after = repo.GetResource("dbo", "customers", "475");

        Assert.NotEqual(before.Resource, after.Resource);
        Assert.Equal(result.Resource, after.Resource);
        Assert.Equal("3109459305", after.Resource.phone);
        Assert.Null(after.Resource.salesRepEmployeeNumber);
    }

    [Fact]
    void PutRecordFailsOnInvalidRecord()
    {
        //Missing required field customerName
        var before = repo.GetResource("dbo", "customers", "477");
        JsonDocument recordUpdate = JsonDocument.Parse(
            $"{{\"contactLastName\" : \"{before.Resource.contactLastName}\"," +
            $"\"contactFirstName\" : \"{before.Resource.contactFirstName}\"," +
            $"\"phone\" : \"0621-5644\"," +
            $"\"addressLine1\" : \"{before.Resource.addressLine1}\"," +
            $"\"city\" : \"{before.Resource.city}\"," +
            $"\"postalCode\" : \"{before.Resource.postalCode}\"," +
            $"\"country\" : \"{before.Resource.country}\"," +
            $"\"creditLimit\" : \"{before.Resource.creditLimit}\"}}"
        );
        Assert.ThrowsAny<Exception>(() => repo.PutResource("dbo", "customers", "477", recordUpdate));
    }

}
