using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafePoint_IRS.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgesAndResolvedStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrustScore",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsResolved",
                table: "Incident",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserBadge",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AwardedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBadge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBadge_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBadge_UserId",
                table: "UserBadge",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBadge");

            migrationBuilder.DropColumn(
                name: "TrustScore",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "Incident");
        }
    }
}
