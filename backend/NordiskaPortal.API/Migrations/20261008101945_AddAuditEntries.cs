using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NordiskaPortal.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    PreviousHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            // Append-only: the database refuses to change or remove audit entries.
            migrationBuilder.Sql("""
                CREATE FUNCTION audit_entries_reject_change() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'AuditEntries is append-only (% is not allowed)', TG_OP;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER audit_entries_no_update_delete
                BEFORE UPDATE OR DELETE ON "AuditEntries"
                FOR EACH ROW EXECUTE FUNCTION audit_entries_reject_change();

                CREATE TRIGGER audit_entries_no_truncate
                BEFORE TRUNCATE ON "AuditEntries"
                FOR EACH STATEMENT EXECUTE FUNCTION audit_entries_reject_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION audit_entries_reject_change() CASCADE;");

            migrationBuilder.DropTable(
                name: "AuditEntries");
        }
    }
}
