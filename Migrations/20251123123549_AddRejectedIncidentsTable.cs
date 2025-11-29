using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafePoint_IRS.Migrations
{
    /// <inheritdoc />
    public partial class AddRejectedIncidentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RejectedIncidents",
                columns: table => new
                {
                    RejectedIncidentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalIncidentID = table.Column<int>(type: "int", nullable: false),
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
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    LocationAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RejectionDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RejectedIncidents", x => x.RejectedIncidentID);
                    table.ForeignKey(
                        name: "FK_RejectedIncidents_Area_Area_Code",
                        column: x => x.Area_Code,
                        principalTable: "Area",
                        principalColumn: "Area_Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RejectedIncidents_IType_Incident_Code",
                        column: x => x.Incident_Code,
                        principalTable: "IType",
                        principalColumn: "Incident_Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RejectedIncidents_Users_Userid",
                        column: x => x.Userid,
                        principalTable: "Users",
                        principalColumn: "Userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RejectedIncidents_Area_Code",
                table: "RejectedIncidents",
                column: "Area_Code");

            migrationBuilder.CreateIndex(
                name: "IX_RejectedIncidents_Incident_Code",
                table: "RejectedIncidents",
                column: "Incident_Code");

            migrationBuilder.CreateIndex(
                name: "IX_RejectedIncidents_Userid",
                table: "RejectedIncidents",
                column: "Userid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RejectedIncidents");
        }
    }
}
