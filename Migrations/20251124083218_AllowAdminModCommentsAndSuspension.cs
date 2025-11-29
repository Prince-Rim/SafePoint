using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafePoint_IRS.Migrations
{
    /// <inheritdoc />
    public partial class AllowAdminModCommentsAndSuspension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SuspensionEndTime",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspensionEndTime",
                table: "Moderator",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Userid",
                table: "Comment",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "AdminId",
                table: "Comment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModId",
                table: "Comment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspensionEndTime",
                table: "Admin",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comment_AdminId",
                table: "Comment",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_ModId",
                table: "Comment",
                column: "ModId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Admin_AdminId",
                table: "Comment",
                column: "AdminId",
                principalTable: "Admin",
                principalColumn: "Adminid",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Moderator_ModId",
                table: "Comment",
                column: "ModId",
                principalTable: "Moderator",
                principalColumn: "Modid",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Admin_AdminId",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Moderator_ModId",
                table: "Comment");

            migrationBuilder.DropIndex(
                name: "IX_Comment_AdminId",
                table: "Comment");

            migrationBuilder.DropIndex(
                name: "IX_Comment_ModId",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "SuspensionEndTime",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SuspensionEndTime",
                table: "Moderator");

            migrationBuilder.DropColumn(
                name: "AdminId",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "ModId",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "SuspensionEndTime",
                table: "Admin");

            migrationBuilder.AlterColumn<Guid>(
                name: "Userid",
                table: "Comment",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
