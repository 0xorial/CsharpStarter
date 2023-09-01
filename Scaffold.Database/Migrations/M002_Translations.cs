using FluentMigrator;
using FluentMigrator.SqlServer;

namespace Scaffold.Database.Migrations;

[Migration(2)]
public class M002_Translations : Migration
{
    public override void Up()
    {
        Create.Table("Translations")
            .WithColumn("Id")
            .AsInt32()
            .Identity()
            .WithColumn("ItemId")
            .AsInt32()
            .ForeignKey("Items", "Id")
            .WithColumn("TranslatedText")
            .AsString();
    }

    public override void Down()
    {
        Delete.Table("Items");
    }
}