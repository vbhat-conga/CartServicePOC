using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CartServicePOC.Migrations
{
    /// <inheritdoc />
    public partial class migrationv4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Price",
                table: "Cart",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Cart");
        }
    }
}
