using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafePoint_IRS.Migrations
{
    /// <inheritdoc />
    public partial class InitialSetupV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Area",
                columns: table => new
                {
                    Area_Code = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ALocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Area", x => x.Area_Code);
                });

            migrationBuilder.CreateTable(
                name: "IType",
                columns: table => new
                {
                    Incident_Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Incident_Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IType", x => x.Incident_Code);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Userid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Contact = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Userpassword = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Userid);
                });

            migrationBuilder.CreateTable(
                name: "Moderator",
                columns: table => new
                {
                    Modid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Area_Code = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MPassword = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Moderator", x => x.Modid);
                    table.ForeignKey(
                        name: "FK_Moderator_Area_Area_Code",
                        column: x => x.Area_Code,
                        principalTable: "Area",
                        principalColumn: "Area_Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Incident",
                columns: table => new
                {
                    IncidentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Userid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Incident_Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OtherHazard = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IncidentDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Area_Code = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Descr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Img = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incident", x => x.IncidentID);
                    table.ForeignKey(
                        name: "FK_Incident_Area_Area_Code",
                        column: x => x.Area_Code,
                        principalTable: "Area",
                        principalColumn: "Area_Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incident_IType_Incident_Code",
                        column: x => x.Incident_Code,
                        principalTable: "IType",
                        principalColumn: "Incident_Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Incident_Users_Userid",
                        column: x => x.Userid,
                        principalTable: "Users",
                        principalColumn: "Userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comment",
                columns: table => new
                {
                    Comment_ID = table.Column<decimal>(type: "numeric(18,0)", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncidentID = table.Column<int>(type: "int", nullable: false),
                    Userid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    dttm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comment", x => x.Comment_ID);
                    table.ForeignKey(
                        name: "FK_Comment_Incident_IncidentID",
                        column: x => x.IncidentID,
                        principalTable: "Incident",
                        principalColumn: "IncidentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comment_Users_Userid",
                        column: x => x.Userid,
                        principalTable: "Users",
                        principalColumn: "Userid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Valid",
                columns: table => new
                {
                    Validation_ID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IncidentID = table.Column<int>(type: "int", nullable: false),
                    Validation_Status = table.Column<bool>(type: "bit", nullable: false),
                    Validation_Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Valid", x => x.Validation_ID);
                    table.ForeignKey(
                        name: "FK_Valid_Incident_IncidentID",
                        column: x => x.IncidentID,
                        principalTable: "Incident",
                        principalColumn: "IncidentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comment_IncidentID",
                table: "Comment",
                column: "IncidentID");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_Userid",
                table: "Comment",
                column: "Userid");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_Area_Code",
                table: "Incident",
                column: "Area_Code");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_Incident_Code",
                table: "Incident",
                column: "Incident_Code");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_Userid",
                table: "Incident",
                column: "Userid");

            migrationBuilder.CreateIndex(
                name: "IX_Moderator_Area_Code",
                table: "Moderator",
                column: "Area_Code");

            migrationBuilder.CreateIndex(
                name: "IX_Valid_IncidentID",
                table: "Valid",
                column: "IncidentID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comment");

            migrationBuilder.DropTable(
                name: "Moderator");

            migrationBuilder.DropTable(
                name: "Valid");

            migrationBuilder.DropTable(
                name: "Incident");

            migrationBuilder.DropTable(
                name: "Area");

            migrationBuilder.DropTable(
                name: "IType");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
