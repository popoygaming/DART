using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DART.API.Net.Migrations
{
    /// <inheritdoc />
    public partial class AddCivilRegistryRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RegistryNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Records", x => x.Id);
                    table.CheckConstraint("CK_Records_RecordType", "[RecordType] IN ('Birth','Marriage','Death')");
                    table.ForeignKey(
                        name: "FK_Records_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Records_CreatedBy",
                table: "Records",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Records_Name",
                table: "Records",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Records_RecordType",
                table: "Records",
                column: "RecordType");

            migrationBuilder.CreateIndex(
                name: "IX_Records_RegistryNumber",
                table: "Records",
                column: "RegistryNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Records");
        }
    }
}
