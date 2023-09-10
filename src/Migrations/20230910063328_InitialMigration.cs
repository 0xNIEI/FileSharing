using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileSharing.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entry",
                columns: table => new
                {
                    guid = table.Column<string>(type: "TEXT", nullable: false),
                    iv = table.Column<byte[]>(type: "BLOB", nullable: false),
                    aesKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    customId = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    expiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    maxNumOfDownloads = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entry", x => x.guid);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entry_customId",
                table: "Entry",
                column: "customId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entry");
        }
    }
}
