using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CartServicePOC.Migrations
{
    /// <inheritdoc />
    public partial class migrationv6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StatusId",
                table: "Cart",
                newName: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Cart",
                newName: "StatusId");
        }
    }
}
