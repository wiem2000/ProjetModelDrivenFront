using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetModelDrivenFront.Migrations
{
    public partial class addjsontoapp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JsonSchema",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JsonSchema",
                table: "Applications");
        }
    }
}
