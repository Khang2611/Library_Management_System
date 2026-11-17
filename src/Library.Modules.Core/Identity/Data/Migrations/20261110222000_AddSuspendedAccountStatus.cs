using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library.Modules.Core.Identity.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSuspendedAccountStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Accounts_Status",
                schema: "identity",
                table: "Accounts");

            migrationBuilder.CreateCheckConstraint(
                name: "CK_Accounts_Status",
                schema: "identity",
                table: "Accounts",
                sql: "[Status] IN (N'Active', N'Locked', N'Inactive', N'Suspended')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Accounts_Status",
                schema: "identity",
                table: "Accounts");

            migrationBuilder.CreateCheckConstraint(
                name: "CK_Accounts_Status",
                schema: "identity",
                table: "Accounts",
                sql: "[Status] IN (N'Active', N'Locked', N'Inactive')");
        }
    }
}

