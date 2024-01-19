using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CartServicePOC.Migrations
{
    /// <inheritdoc />
    public partial class migrationv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "CartItem",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Price",
                table: "CartItem",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "CartItem");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "CartItem");
        }
    }
}
