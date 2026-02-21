using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeatureFlagEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeatureFlags",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "GroupOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FeatureFlagName = table.Column<string>(type: "TEXT", nullable: false),
                    GroupId = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupOverrides_FeatureFlags_FeatureFlagName",
                        column: x => x.FeatureFlagName,
                        principalTable: "FeatureFlags",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FeatureFlagName = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOverrides_FeatureFlags_FeatureFlagName",
                        column: x => x.FeatureFlagName,
                        principalTable: "FeatureFlags",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupOverrides_FeatureFlagName_GroupId",
                table: "GroupOverrides",
                columns: new[] { "FeatureFlagName", "GroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOverrides_FeatureFlagName_UserId",
                table: "UserOverrides",
                columns: new[] { "FeatureFlagName", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupOverrides");

            migrationBuilder.DropTable(
                name: "UserOverrides");

            migrationBuilder.DropTable(
                name: "FeatureFlags");
        }
    }
}
