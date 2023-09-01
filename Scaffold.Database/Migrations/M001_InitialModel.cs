using FluentMigrator;
using FluentMigrator.SqlServer;

namespace Scaffold.Database.Migrations;

[Migration(1)]
public class M001_InitialModel : Migration
{
    public override void Up()
    {
        Create.Table("Items")
            .WithColumn("Id")
            .AsInt32()
            .Identity()
            .WithColumn("Text")
            .AsString();
    }

    public override void Down()
    {
        Delete.Table("Items");
    }
}