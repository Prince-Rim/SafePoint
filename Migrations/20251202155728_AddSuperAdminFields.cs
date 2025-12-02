using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafePoint_IRS.Migrations
{
    /// <inheritdoc />
    public partial class AddSuperAdminFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuperAdmin",
                table: "Admin",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Permissions",
                table: "Admin",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSuperAdmin",
                table: "Admin");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "Admin");
        }
    }
}
