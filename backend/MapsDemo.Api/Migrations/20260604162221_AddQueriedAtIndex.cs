using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapsDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQueriedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_geocode_queries_queried_at",
                table: "geocode_queries",
                column: "queried_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_geocode_queries_queried_at",
                table: "geocode_queries");
        }
    }
}
