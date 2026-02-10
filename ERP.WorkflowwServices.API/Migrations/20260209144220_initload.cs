using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.WorkflowwServices.API.Migrations
{
    /// <inheritdoc />
    public partial class initload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblWFEvent",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PageId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    ShipRecordId = table.Column<int>(type: "int", nullable: true),
                    OfficeRecordId = table.Column<int>(type: "int", nullable: true),
                    MultiUse = table.Column<bool>(type: "bit", nullable: false),
                    CheckShow = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblWFEvent", x => x.EventId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblWFEvent");
        }
    }
}
